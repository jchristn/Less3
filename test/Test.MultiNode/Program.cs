namespace Test.MultiNode
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.Runtime;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Less3.Locking;
    using Less3.Settings;
    using SyslogLogging;

    /// <summary>
    /// End-to-end multi-node integration harness. A single command stands up a temporary cluster in
    /// Docker on randomized ports, exercises every major API in positive and negative cases, runs
    /// failure scenarios (node crash, cross-node operations, concurrent writes, Clutch WebSocket
    /// reconnect), reports PASS/FAIL with per-test runtime, and tears the stack down.
    ///
    /// Usage:
    ///   dotnet run --project test/Test.MultiNode        (stand up, test, tear down)
    ///   dotnet run --project test/Test.MultiNode -- --keep       (leave the stack running)
    ///   dotnet run --project test/Test.MultiNode -- --no-clutch  (skip the Clutch tests)
    /// </summary>
    public static class Program
    {
        private static readonly TestReport _Report = new TestReport();
        private static readonly HttpClient _Http = new HttpClient();
        private const string _AdminKey = "less3admin";

        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>0 if every test passed; 1 otherwise.</returns>
        public static async Task<int> Main(string[] args)
        {
            bool keep = args != null && args.Contains("--keep");
            bool noClutch = args != null && args.Contains("--no-clutch");

            CancellationToken token = CancellationToken.None;

            List<int> ports = PortAllocator.GetFreePorts(5);
            string projectName = "less3mn" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string tempDir = Path.Combine(Path.GetTempPath(), projectName);

            DockerStack stack = new DockerStack(tempDir, projectName, ports[0], ports[1], ports[2], ports[3], ports[4]);
            stack.WriteConfigs();

            Console.WriteLine("Less3 Multi-Node Integration Tests");
            Console.WriteLine("Project:   " + projectName);
            Console.WriteLine("Temp dir:  " + tempDir);
            Console.WriteLine("LB :8000 -> " + stack.LbPort + "   node1 -> " + stack.Node1Port + "   node2 -> " + stack.Node2Port + "   postgres -> " + stack.PostgresPort + "   clutch -> " + stack.ClutchPort);

            if (!await DockerAvailableAsync(token).ConfigureAwait(false))
            {
                Console.WriteLine("ERROR: docker is not available on this machine. Install Docker and ensure the daemon is running.");
                return 1;
            }

            bool coreUp = false;
            try
            {
                _Report.Section("Infrastructure");
                await _Report.RunAsync("Build and start core stack (postgres, 2 nodes, nginx)", async () =>
                {
                    ProcessRunResult up = await stack.UpAsync("postgres less3-node1 less3-node2 nginx", true, token).ConfigureAwait(false);
                    if (up.ExitCode != 0) throw new InvalidOperationException("docker compose up failed (exit " + up.ExitCode + "):\n" + Tail(up.Output, 40));
                }).ConfigureAwait(false);

                coreUp = true;

                await _Report.RunAsync("Load balancer /healthz is ready", () => WaitForHealthyAsync(stack.LbPort, 180000, token)).ConfigureAwait(false);
                await _Report.RunAsync("Node 1 /healthz is ready", () => WaitForHealthyAsync(stack.Node1Port, 120000, token)).ConfigureAwait(false);
                await _Report.RunAsync("Node 2 /healthz is ready", () => WaitForHealthyAsync(stack.Node2Port, 120000, token)).ConfigureAwait(false);

                AmazonS3Client lb = MakeS3Client(stack.LbPort, "default", "default");
                AmazonS3Client node1 = MakeS3Client(stack.Node1Port, "default", "default");
                AmazonS3Client node2 = MakeS3Client(stack.Node2Port, "default", "default");
                // A nonexistent access key must be rejected regardless of signature validation
                // (the harness runs with signatures off for SDK compatibility, so a merely-wrong
                // secret would not be checked; an unknown access key still fails credential lookup).
                AmazonS3Client bad = MakeS3Client(stack.LbPort, "nonexistent-access-key", "nonexistent-secret");

                await RunClusterRestTests(stack).ConfigureAwait(false);
                await RunS3PositiveTests(lb).ConfigureAwait(false);
                await RunS3NegativeTests(lb, bad).ConfigureAwait(false);
                await RunCrossNodeTests(node1, node2).ConfigureAwait(false);
                await RunConcurrencyTests(node1, node2).ConfigureAwait(false);
                await RunObservabilityTests(stack).ConfigureAwait(false);

                if (!noClutch) await RunClutchTests(stack, token).ConfigureAwait(false);

                await RunFailoverTests(stack, token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FATAL: " + e.Message);
                Console.ResetColor();
                if (coreUp)
                {
                    Console.WriteLine("--- node1 logs ---");
                    Console.WriteLine(Tail(await SafeLogsAsync(stack, "less3-node1", token).ConfigureAwait(false), 30));
                }
            }
            finally
            {
                if (!keep)
                {
                    Console.WriteLine();
                    Console.WriteLine("Tearing down stack...");
                    try { await stack.DownAsync(token).ConfigureAwait(false); } catch (Exception e) { Console.WriteLine("teardown warning: " + e.Message); }
                    try { Directory.Delete(tempDir, true); } catch (Exception) { }
                }
                else
                {
                    Console.WriteLine("Leaving stack running (--keep). Tear down with: docker compose -p " + projectName + " -f \"" + Path.Combine(tempDir, "compose.yaml") + "\" down -v");
                }
            }

            return _Report.Summarize();
        }

        #region Test-Sections

        private static async Task RunClusterRestTests(DockerStack stack)
        {
            _Report.Section("Cluster REST APIs");

            await _Report.RunAsync("GET /healthz through the load balancer", async () =>
            {
                JsonElement j = await GetJsonAsync("http://127.0.0.1:" + stack.LbPort + "/healthz", false).ConfigureAwait(false);
                string status = j.GetProperty("status").GetString();
                if (status != "ok") throw new Exception("healthz status was '" + status + "'");
            }).ConfigureAwait(false);

            await _Report.RunAsync("GET /api/v1/cluster/health reports an enabled 2-node cluster", async () =>
            {
                JsonElement j = await GetJsonAsync("http://127.0.0.1:" + stack.LbPort + "/api/v1/cluster/health", true).ConfigureAwait(false);
                if (!j.GetProperty("ClusterEnabled").GetBoolean()) throw new Exception("ClusterEnabled was false");
                int total = j.GetProperty("TotalNodes").GetInt32();
                if (total < 2) throw new Exception("TotalNodes was " + total + " (expected >= 2). Raw: " + j.GetRawText());
            }).ConfigureAwait(false);

            await _Report.RunAsync("GET /api/v1/cluster/nodes lists both nodes", async () =>
            {
                JsonElement j = await GetJsonAsync("http://127.0.0.1:" + stack.LbPort + "/api/v1/cluster/nodes", true).ConfigureAwait(false);
                int count = j.GetArrayLength();
                if (count < 2) throw new Exception("expected >= 2 nodes, got " + count + ". Raw: " + j.GetRawText());
            }).ConfigureAwait(false);

            await _Report.RunAsync("GET /api/v1/cluster/leader responds", async () =>
            {
                await GetJsonAsync("http://127.0.0.1:" + stack.LbPort + "/api/v1/cluster/leader", true).ConfigureAwait(false);
            }).ConfigureAwait(false);

            await _Report.RunAsync("GET /api/v1/locks returns an array", async () =>
            {
                JsonElement j = await GetJsonAsync("http://127.0.0.1:" + stack.LbPort + "/api/v1/locks", true).ConfigureAwait(false);
                if (j.ValueKind != JsonValueKind.Array) throw new Exception("expected an array, got " + j.ValueKind);
            }).ConfigureAwait(false);

            await _Report.RunAsync("REST rejects a bad admin key", async () =>
            {
                using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, "http://127.0.0.1:" + stack.LbPort + "/api/v1/cluster/health");
                req.Headers.Add("x-api-key", "not-the-admin-key");
                using HttpResponseMessage resp = await _Http.SendAsync(req).ConfigureAwait(false);
                if ((int)resp.StatusCode == 200) throw new Exception("bad admin key was accepted (200)");
            }).ConfigureAwait(false);
        }

        private static async Task RunS3PositiveTests(AmazonS3Client s3)
        {
            _Report.Section("S3 API — positive");
            string bucket = "mn-pos-" + Rand();

            await _Report.RunAsync("ListBuckets", async () => { await s3.ListBucketsAsync().ConfigureAwait(false); }).ConfigureAwait(false);

            await _Report.RunAsync("CreateBucket", async () => { await s3.PutBucketAsync(bucket).ConfigureAwait(false); }).ConfigureAwait(false);

            await _Report.RunAsync("Bucket appears in ListBuckets", async () =>
            {
                ListBucketsResponse lb = await s3.ListBucketsAsync().ConfigureAwait(false);
                if (!lb.Buckets.Any(b => b.BucketName == bucket)) throw new Exception("bucket " + bucket + " not listed");
            }).ConfigureAwait(false);

            await _Report.RunAsync("GetBucketLocation", async () => { await s3.GetBucketLocationAsync(bucket).ConfigureAwait(false); }).ConfigureAwait(false);

            byte[] payload = RandomBytes(4096);
            await _Report.RunAsync("PutObject", () => PutBytesAsync(s3, bucket, "hello.bin", payload)).ConfigureAwait(false);

            await _Report.RunAsync("GetObject returns identical bytes", async () =>
            {
                byte[] got = await GetBytesAsync(s3, bucket, "hello.bin").ConfigureAwait(false);
                if (!got.SequenceEqual(payload)) throw new Exception("read back " + got.Length + " bytes, expected " + payload.Length + " and identical content");
            }).ConfigureAwait(false);

            await _Report.RunAsync("HeadObject reports content length", async () =>
            {
                GetObjectMetadataResponse md = await s3.GetObjectMetadataAsync(bucket, "hello.bin").ConfigureAwait(false);
                if (md.ContentLength != payload.Length) throw new Exception("content length " + md.ContentLength + " != " + payload.Length);
            }).ConfigureAwait(false);

            await _Report.RunAsync("GetObject range returns the requested slice", async () =>
            {
                GetObjectRequest req = new GetObjectRequest { BucketName = bucket, Key = "hello.bin", ByteRange = new ByteRange(0, 9) };
                using GetObjectResponse resp = await s3.GetObjectAsync(req).ConfigureAwait(false);
                using MemoryStream ms = new MemoryStream();
                await resp.ResponseStream.CopyToAsync(ms).ConfigureAwait(false);
                byte[] slice = ms.ToArray();
                if (!slice.SequenceEqual(payload.Take(slice.Length))) throw new Exception("range slice did not match the first bytes");
            }).ConfigureAwait(false);

            await _Report.RunAsync("ListObjectsV2 includes the object", async () =>
            {
                ListObjectsV2Response list = await s3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket }).ConfigureAwait(false);
                if (!list.S3Objects.Any(o => o.Key == "hello.bin")) throw new Exception("hello.bin not listed");
            }).ConfigureAwait(false);

            await _Report.RunAsync("PutObjectTagging and GetObjectTagging round-trip", async () =>
            {
                await s3.PutObjectTaggingAsync(new PutObjectTaggingRequest
                {
                    BucketName = bucket,
                    Key = "hello.bin",
                    Tagging = new Tagging { TagSet = new List<Tag> { new Tag { Key = "env", Value = "test" } } }
                }).ConfigureAwait(false);

                GetObjectTaggingResponse tg = await s3.GetObjectTaggingAsync(new GetObjectTaggingRequest { BucketName = bucket, Key = "hello.bin" }).ConfigureAwait(false);
                if (!tg.Tagging.Any(t => t.Key == "env" && t.Value == "test")) throw new Exception("tag env=test not returned");
            }).ConfigureAwait(false);

            await _Report.RunAsync("DeleteObjects (batch)", async () =>
            {
                await PutBytesAsync(s3, bucket, "d1", RandomBytes(16)).ConfigureAwait(false);
                await PutBytesAsync(s3, bucket, "d2", RandomBytes(16)).ConfigureAwait(false);
                await s3.DeleteObjectsAsync(new DeleteObjectsRequest
                {
                    BucketName = bucket,
                    Objects = new List<KeyVersion> { new KeyVersion { Key = "d1" }, new KeyVersion { Key = "d2" } }
                }).ConfigureAwait(false);
            }).ConfigureAwait(false);

            // Multipart
            string mpKey = "multipart.bin";
            byte[] part1 = RandomBytes(1024 * 1024);
            byte[] part2 = RandomBytes(512 * 1024);
            string uploadId = null;

            await _Report.RunAsync("InitiateMultipartUpload", async () =>
            {
                InitiateMultipartUploadResponse init = await s3.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest { BucketName = bucket, Key = mpKey }).ConfigureAwait(false);
                uploadId = init.UploadId;
                if (String.IsNullOrEmpty(uploadId)) throw new Exception("no upload id returned");
            }).ConfigureAwait(false);

            List<PartETag> etags = new List<PartETag>();
            await _Report.RunAsync("UploadPart x2", async () =>
            {
                UploadPartResponse p1 = await s3.UploadPartAsync(new UploadPartRequest { BucketName = bucket, Key = mpKey, UploadId = uploadId, PartNumber = 1, InputStream = new MemoryStream(part1), PartSize = part1.Length }).ConfigureAwait(false);
                UploadPartResponse p2 = await s3.UploadPartAsync(new UploadPartRequest { BucketName = bucket, Key = mpKey, UploadId = uploadId, PartNumber = 2, InputStream = new MemoryStream(part2), PartSize = part2.Length }).ConfigureAwait(false);
                etags.Add(new PartETag { PartNumber = 1, ETag = p1.ETag });
                etags.Add(new PartETag { PartNumber = 2, ETag = p2.ETag });
            }).ConfigureAwait(false);

            await _Report.RunAsync("ListParts shows both parts", async () =>
            {
                ListPartsResponse lp = await s3.ListPartsAsync(new ListPartsRequest { BucketName = bucket, Key = mpKey, UploadId = uploadId }).ConfigureAwait(false);
                if (lp.Parts.Count != 2) throw new Exception("expected 2 parts, got " + lp.Parts.Count);
            }).ConfigureAwait(false);

            await _Report.RunAsync("CompleteMultipartUpload assembles the object", async () =>
            {
                await s3.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest { BucketName = bucket, Key = mpKey, UploadId = uploadId, PartETags = etags }).ConfigureAwait(false);
                byte[] assembled = await GetBytesAsync(s3, bucket, mpKey).ConfigureAwait(false);
                byte[] expected = part1.Concat(part2).ToArray();
                if (!assembled.SequenceEqual(expected)) throw new Exception("assembled object (" + assembled.Length + " bytes) did not match the concatenated parts (" + expected.Length + " bytes)");
            }).ConfigureAwait(false);

            await _Report.RunAsync("AbortMultipartUpload discards an upload", async () =>
            {
                InitiateMultipartUploadResponse init = await s3.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest { BucketName = bucket, Key = "aborted.bin" }).ConfigureAwait(false);
                await s3.UploadPartAsync(new UploadPartRequest { BucketName = bucket, Key = "aborted.bin", UploadId = init.UploadId, PartNumber = 1, InputStream = new MemoryStream(RandomBytes(65536)), PartSize = 65536 }).ConfigureAwait(false);
                await s3.AbortMultipartUploadAsync(new AbortMultipartUploadRequest { BucketName = bucket, Key = "aborted.bin", UploadId = init.UploadId }).ConfigureAwait(false);
            }).ConfigureAwait(false);

            await _Report.RunAsync("Versioning enable + versioned overwrite keeps versions", async () =>
            {
                string vbucket = "mn-ver-" + Rand();
                await s3.PutBucketAsync(vbucket).ConfigureAwait(false);
                await s3.PutBucketVersioningAsync(new PutBucketVersioningRequest { BucketName = vbucket, VersioningConfig = new S3BucketVersioningConfig { Status = VersionStatus.Enabled } }).ConfigureAwait(false);
                await PutBytesAsync(s3, vbucket, "v.bin", RandomBytes(64)).ConfigureAwait(false);
                await PutBytesAsync(s3, vbucket, "v.bin", RandomBytes(64)).ConfigureAwait(false);
                ListVersionsResponse versions = await s3.ListVersionsAsync(new ListVersionsRequest { BucketName = vbucket, Prefix = "v.bin" }).ConfigureAwait(false);
                if (versions.Versions.Count < 2) throw new Exception("expected >= 2 versions, got " + versions.Versions.Count);
            }).ConfigureAwait(false);
        }

        private static async Task RunS3NegativeTests(AmazonS3Client s3, AmazonS3Client bad)
        {
            _Report.Section("S3 API — negative");
            string bucket = "mn-neg-" + Rand();
            await s3.PutBucketAsync(bucket).ConfigureAwait(false);

            await _Report.RunAsync("GetObject on a missing key returns 404 NoSuchKey", async () =>
            {
                await ExpectStatusAsync(() => s3.GetObjectAsync(bucket, "does-not-exist"), 404).ConfigureAwait(false);
            }).ConfigureAwait(false);

            await _Report.RunAsync("PutObject to a missing bucket returns 404", async () =>
            {
                await ExpectStatusAsync(() => PutBytesAsync(s3, "no-such-bucket-" + Rand(), "k", RandomBytes(8)), 404).ConfigureAwait(false);
            }).ConfigureAwait(false);

            await _Report.RunAsync("ListObjects on a missing bucket returns 404", async () =>
            {
                await ExpectStatusAsync(() => s3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = "no-such-bucket-" + Rand() }), 404).ConfigureAwait(false);
            }).ConfigureAwait(false);

            await _Report.RunAsync("Unknown access key is rejected (403)", async () =>
            {
                await ExpectStatusAsync(() => bad.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket }), 403).ConfigureAwait(false);
            }).ConfigureAwait(false);

            await _Report.RunAsync("CompleteMultipartUpload with a bogus ETag is rejected", async () =>
            {
                InitiateMultipartUploadResponse init = await s3.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest { BucketName = bucket, Key = "bad-mp" }).ConfigureAwait(false);
                await s3.UploadPartAsync(new UploadPartRequest { BucketName = bucket, Key = "bad-mp", UploadId = init.UploadId, PartNumber = 1, InputStream = new MemoryStream(RandomBytes(65536)), PartSize = 65536 }).ConfigureAwait(false);
                bool threw = false;
                try
                {
                    await s3.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest { BucketName = bucket, Key = "bad-mp", UploadId = init.UploadId, PartETags = new List<PartETag> { new PartETag { PartNumber = 1, ETag = "\"deadbeefdeadbeefdeadbeefdeadbeef\"" } } }).ConfigureAwait(false);
                }
                catch (AmazonS3Exception) { threw = true; }
                if (!threw) throw new Exception("completing with a wrong ETag was accepted");
                await s3.AbortMultipartUploadAsync(new AbortMultipartUploadRequest { BucketName = bucket, Key = "bad-mp", UploadId = init.UploadId }).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private static async Task RunCrossNodeTests(AmazonS3Client node1, AmazonS3Client node2)
        {
            _Report.Section("Cross-node operations");
            string bucket = "mn-xn-" + Rand();
            await node1.PutBucketAsync(bucket).ConfigureAwait(false);

            byte[] payload = RandomBytes(50000);
            await _Report.RunAsync("Write on node 1, read on node 2 (shared storage + control plane)", async () =>
            {
                await PutBytesAsync(node1, bucket, "xn.bin", payload).ConfigureAwait(false);
                byte[] got = await RetryValueAsync(() => GetBytesAsync(node2, bucket, "xn.bin"), 6, 1000).ConfigureAwait(false);
                if (!got.SequenceEqual(payload)) throw new Exception("node 2 read " + got.Length + " bytes, not identical to what node 1 wrote");
            }).ConfigureAwait(false);

            await _Report.RunAsync("Multipart initiated + uploaded on node 1, completed on node 2", async () =>
            {
                string key = "xn-multipart.bin";
                byte[] p1 = RandomBytes(700000);
                byte[] p2 = RandomBytes(300000);
                InitiateMultipartUploadResponse init = await node1.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest { BucketName = bucket, Key = key }).ConfigureAwait(false);
                UploadPartResponse r1 = await node1.UploadPartAsync(new UploadPartRequest { BucketName = bucket, Key = key, UploadId = init.UploadId, PartNumber = 1, InputStream = new MemoryStream(p1), PartSize = p1.Length }).ConfigureAwait(false);
                UploadPartResponse r2 = await node1.UploadPartAsync(new UploadPartRequest { BucketName = bucket, Key = key, UploadId = init.UploadId, PartNumber = 2, InputStream = new MemoryStream(p2), PartSize = p2.Length }).ConfigureAwait(false);

                await node2.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
                {
                    BucketName = bucket,
                    Key = key,
                    UploadId = init.UploadId,
                    PartETags = new List<PartETag> { new PartETag { PartNumber = 1, ETag = r1.ETag }, new PartETag { PartNumber = 2, ETag = r2.ETag } }
                }).ConfigureAwait(false);

                byte[] assembled = await GetBytesAsync(node1, bucket, key).ConfigureAwait(false);
                if (!assembled.SequenceEqual(p1.Concat(p2).ToArray())) throw new Exception("object completed on node 2 did not match parts uploaded on node 1");
            }).ConfigureAwait(false);
        }

        private static async Task RunConcurrencyTests(AmazonS3Client node1, AmazonS3Client node2)
        {
            _Report.Section("Concurrency & integrity");
            string bucket = "mn-conc-" + Rand();
            await node1.PutBucketAsync(bucket).ConfigureAwait(false);

            await _Report.RunAsync("Concurrent same-key writes across nodes leave one consistent object", async () =>
            {
                const int writers = 12;
                List<byte[]> payloads = new List<byte[]>();
                for (int i = 0; i < writers; i++) payloads.Add(RandomBytes(20000));

                List<Task> tasks = new List<Task>();
                for (int i = 0; i < writers; i++)
                {
                    int idx = i;
                    AmazonS3Client client = idx % 2 == 0 ? node1 : node2;
                    tasks.Add(PutBytesAsync(client, bucket, "hot.bin", payloads[idx]));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                byte[] final = await GetBytesAsync(node1, bucket, "hot.bin").ConfigureAwait(false);
                bool matchesOne = payloads.Any(p => p.SequenceEqual(final));
                if (!matchesOne) throw new Exception("final object is not byte-identical to any single write — indicates a torn/mixed object");
            }).ConfigureAwait(false);
        }

        private static async Task RunObservabilityTests(DockerStack stack)
        {
            _Report.Section("Observability");

            await _Report.RunAsync("Node 1 exposes a Prometheus /metrics endpoint", () => AssertMetricsAsync(stack.Node1Port)).ConfigureAwait(false);
            await _Report.RunAsync("Node 2 exposes a Prometheus /metrics endpoint", () => AssertMetricsAsync(stack.Node2Port)).ConfigureAwait(false);

            await _Report.RunAsync("Metrics include Watson HTTP-server series", async () =>
            {
                string body = await RetryValueAsync(() => _Http.GetStringAsync("http://127.0.0.1:" + stack.Node1Port + "/metrics"), 5, 1500).ConfigureAwait(false);
                bool hasSeries = body.Contains("http_server") || body.Contains("watson_");
                if (!hasSeries) throw new Exception("no recognizable Watson/HTTP series in /metrics output (" + body.Length + " bytes)");
            }).ConfigureAwait(false);
        }

        private static async Task AssertMetricsAsync(int metricsPort)
        {
            string body = await RetryValueAsync(async () =>
            {
                using HttpResponseMessage resp = await _Http.GetAsync("http://127.0.0.1:" + metricsPort + "/metrics").ConfigureAwait(false);
                string text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                if ((int)resp.StatusCode != 200) throw new Exception("/metrics returned " + (int)resp.StatusCode);
                return text;
            }, 6, 1500).ConfigureAwait(false);

            if (!body.Contains("# TYPE") && !body.Contains("# HELP"))
                throw new Exception("/metrics did not return Prometheus exposition format (" + body.Length + " bytes)");
        }

        private static async Task RunClutchTests(DockerStack stack, CancellationToken token)
        {
            _Report.Section("Clutch WebSocket lock provider");

            await _Report.RunAsync("Start Clutch server", async () =>
            {
                ProcessRunResult up = await stack.StartAsync("clutch", token).ConfigureAwait(false);
                if (up.ExitCode != 0) throw new InvalidOperationException("failed to start clutch:\n" + Tail(up.Output, 20));
                await WaitForTcpAsync(stack.ClutchPort, 90000, token).ConfigureAwait(false);
                // Clutch shares Postgres and needs time to run its own migrations before the lock
                // WebSocket accepts connections.
                await Task.Delay(12000, token).ConfigureAwait(false);
            }).ConfigureAwait(false);

            LoggingModule logging = new LoggingModule("127.0.0.1", 514, false);
            ClutchSettings settings = new ClutchSettings { Endpoint = "http://127.0.0.1:" + stack.ClutchPort, AccessKey = "clutch-default-access-key" };
            ClutchLockManager manager = new ClutchLockManager(settings, new LockSettings(), logging);

            LockHandle held = null;
            try
            {
                await _Report.RunAsync("Clutch: acquire a Write lock over WebSocket", async () =>
                {
                    try
                    {
                        held = await RetryValueAsync(() => manager.AcquireAsync("test/clutch-key", LockMode.Write, new AcquireOptions(10000)), 6, 3000).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        string logs = await SafeLogsAsync(stack, "clutch", token).ConfigureAwait(false);
                        throw new Exception(e.Message + "\nclutch logs:\n" + Tail(logs, 15));
                    }
                    if (held == null) throw new Exception("acquire returned no handle");
                    await manager.ReleaseAsync(held).ConfigureAwait(false);
                }).ConfigureAwait(false);

                await _Report.RunAsync("Clutch: WebSocket re-establishes after server restart", async () =>
                {
                    // Force the connection to drop by restarting the Clutch server, then acquire
                    // again — the manager must transparently reconnect.
                    await stack.KillAsync("clutch", token).ConfigureAwait(false);
                    await Task.Delay(2000, token).ConfigureAwait(false);
                    await stack.StartAsync("clutch", token).ConfigureAwait(false);
                    await WaitForTcpAsync(stack.ClutchPort, 90000, token).ConfigureAwait(false);
                    await Task.Delay(5000, token).ConfigureAwait(false);

                    LockHandle after = await RetryValueAsync(() => manager.AcquireAsync("test/clutch-key-2", LockMode.Write, new AcquireOptions(10000)), 5, 2000).ConfigureAwait(false);
                    if (after == null) throw new Exception("acquire after reconnect returned no handle");
                    await manager.ReleaseAsync(after).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            finally
            {
                manager.Dispose();
            }
        }

        private static async Task RunFailoverTests(DockerStack stack, CancellationToken token)
        {
            _Report.Section("Failover");
            AmazonS3Client lb = MakeS3Client(stack.LbPort, "default", "default");
            string bucket = "mn-fail-" + Rand();
            await lb.PutBucketAsync(bucket).ConfigureAwait(false);
            await PutBytesAsync(lb, bucket, "before.bin", RandomBytes(1024)).ConfigureAwait(false);

            await _Report.RunAsync("Kill node 2", async () =>
            {
                await stack.KillAsync("less3-node2", token).ConfigureAwait(false);
            }).ConfigureAwait(false);

            await _Report.RunAsync("Load balancer keeps serving reads/writes after a node dies", async () =>
            {
                byte[] payload = RandomBytes(2048);
                await RetryAsync(() => PutBytesAsync(lb, bucket, "after.bin", payload), 10, 2000).ConfigureAwait(false);
                byte[] got = await RetryValueAsync(() => GetBytesAsync(lb, bucket, "after.bin"), 10, 2000).ConfigureAwait(false);
                if (!got.SequenceEqual(payload)) throw new Exception("object written through the LB after a node died did not read back correctly");
            }).ConfigureAwait(false);

            await _Report.RunAsync("Cluster health reports the dead node unhealthy", async () =>
            {
                bool observed = false;
                DateTime deadline = DateTime.UtcNow.AddSeconds(30);
                while (DateTime.UtcNow < deadline)
                {
                    try
                    {
                        JsonElement j = await GetJsonAsync("http://127.0.0.1:" + stack.LbPort + "/api/v1/cluster/health", true).ConfigureAwait(false);
                        if (j.GetProperty("HealthyNodes").GetInt32() < j.GetProperty("TotalNodes").GetInt32()) { observed = true; break; }
                    }
                    catch (Exception) { }
                    await Task.Delay(2000, token).ConfigureAwait(false);
                }
                if (!observed) throw new Exception("cluster never reported a node unhealthy within 30s");
            }).ConfigureAwait(false);

            await _Report.RunAsync("Restarted node rejoins and becomes healthy", async () =>
            {
                await stack.StartAsync("less3-node2", token).ConfigureAwait(false);
                await WaitForHealthyAsync(stack.Node2Port, 90000, token).ConfigureAwait(false);

                bool healthy = false;
                DateTime deadline = DateTime.UtcNow.AddSeconds(30);
                while (DateTime.UtcNow < deadline)
                {
                    try
                    {
                        JsonElement j = await GetJsonAsync("http://127.0.0.1:" + stack.LbPort + "/api/v1/cluster/health", true).ConfigureAwait(false);
                        if (j.GetProperty("HealthyNodes").GetInt32() >= 2) { healthy = true; break; }
                    }
                    catch (Exception) { }
                    await Task.Delay(2000, token).ConfigureAwait(false);
                }
                if (!healthy) throw new Exception("restarted node never returned to healthy within 30s");
            }).ConfigureAwait(false);
        }

        #endregion

        #region Helpers

        private static AmazonS3Client MakeS3Client(int port, string accessKey, string secretKey)
        {
            AmazonS3Config config = new AmazonS3Config
            {
                ServiceURL = "http://127.0.0.1:" + port,
                ForcePathStyle = true,
                AuthenticationRegion = "us-west-1",
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
            };
            return new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), config);
        }

        private static async Task PutBytesAsync(AmazonS3Client s3, string bucket, string key, byte[] bytes)
        {
            using MemoryStream ms = new MemoryStream(bytes);
            await s3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = key, InputStream = ms, ContentType = "application/octet-stream" }).ConfigureAwait(false);
        }

        private static async Task<byte[]> GetBytesAsync(AmazonS3Client s3, string bucket, string key)
        {
            using GetObjectResponse resp = await s3.GetObjectAsync(bucket, key).ConfigureAwait(false);
            using MemoryStream ms = new MemoryStream();
            await resp.ResponseStream.CopyToAsync(ms).ConfigureAwait(false);
            return ms.ToArray();
        }

        private static async Task ExpectStatusAsync(Func<Task> op, int expectedStatus)
        {
            try
            {
                await op().ConfigureAwait(false);
                throw new Exception("expected HTTP " + expectedStatus + " but the operation succeeded");
            }
            catch (AmazonS3Exception e)
            {
                if ((int)e.StatusCode != expectedStatus)
                    throw new Exception("expected HTTP " + expectedStatus + " but got " + (int)e.StatusCode + " (" + e.ErrorCode + ")");
            }
        }

        private static async Task<JsonElement> GetJsonAsync(string url, bool admin)
        {
            using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, url);
            if (admin) req.Headers.Add("x-api-key", _AdminKey);
            using HttpResponseMessage resp = await _Http.SendAsync(req).ConfigureAwait(false);
            string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if ((int)resp.StatusCode != 200) throw new Exception("GET " + url + " returned " + (int)resp.StatusCode + ": " + body);
            using JsonDocument doc = JsonDocument.Parse(body);
            return doc.RootElement.Clone();
        }

        private static async Task WaitForHealthyAsync(int port, int timeoutMs, CancellationToken token)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            Exception last = null;
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    using HttpResponseMessage resp = await _Http.GetAsync("http://127.0.0.1:" + port + "/healthz").ConfigureAwait(false);
                    if ((int)resp.StatusCode == 200) return;
                    last = new Exception("status " + (int)resp.StatusCode);
                }
                catch (Exception e) { last = e; }
                await Task.Delay(2000, token).ConfigureAwait(false);
            }
            throw new TimeoutException("port " + port + " /healthz never became ready within " + timeoutMs + "ms. Last: " + (last?.Message ?? "n/a"));
        }

        private static async Task WaitForTcpAsync(int port, int timeoutMs, CancellationToken token)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    using System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient();
                    await client.ConnectAsync("127.0.0.1", port).ConfigureAwait(false);
                    if (client.Connected) return;
                }
                catch (Exception) { }
                await Task.Delay(1500, token).ConfigureAwait(false);
            }
            throw new TimeoutException("TCP port " + port + " never opened within " + timeoutMs + "ms.");
        }

        private static async Task RetryAsync(Func<Task> op, int attempts, int delayMs)
        {
            Exception last = null;
            for (int i = 0; i < attempts; i++)
            {
                try { await op().ConfigureAwait(false); return; }
                catch (Exception e) { last = e; await Task.Delay(delayMs).ConfigureAwait(false); }
            }
            throw new Exception("operation failed after " + attempts + " attempts. Last: " + (last?.Message ?? "n/a"));
        }

        private static async Task<T> RetryValueAsync<T>(Func<Task<T>> op, int attempts, int delayMs)
        {
            Exception last = null;
            for (int i = 0; i < attempts; i++)
            {
                try { return await op().ConfigureAwait(false); }
                catch (Exception e) { last = e; await Task.Delay(delayMs).ConfigureAwait(false); }
            }
            throw new Exception("operation failed after " + attempts + " attempts. Last: " + (last?.Message ?? "n/a"));
        }

        private static async Task<bool> DockerAvailableAsync(CancellationToken token)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo { FileName = "docker", Arguments = "version --format {{.Server.Version}}", RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                using System.Diagnostics.Process p = System.Diagnostics.Process.Start(psi);
                await p.WaitForExitAsync(token).ConfigureAwait(false);
                return p.ExitCode == 0;
            }
            catch (Exception) { return false; }
        }

        private static async Task<string> SafeLogsAsync(DockerStack stack, string service, CancellationToken token)
        {
            try { return await stack.LogsAsync(service, token).ConfigureAwait(false); }
            catch (Exception e) { return "(could not fetch logs: " + e.Message + ")"; }
        }

        private static string Tail(string text, int lines)
        {
            if (String.IsNullOrEmpty(text)) return "(no output)";
            string[] all = text.Replace("\r", "").Split('\n');
            int start = Math.Max(0, all.Length - lines);
            return String.Join("\n", all.Skip(start));
        }

        private static readonly Random _Rng = new Random();

        private static string Rand()
        {
            lock (_Rng) { return _Rng.Next(100000, 999999).ToString(); }
        }

        private static byte[] RandomBytes(int count)
        {
            byte[] bytes = new byte[count];
            lock (_Rng) { _Rng.NextBytes(bytes); }
            return bytes;
        }

        #endregion
    }
}
