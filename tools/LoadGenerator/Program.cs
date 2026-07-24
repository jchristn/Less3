namespace LoadGenerator
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.Runtime;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Less3.Helpers;

    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            LoadOptions options = LoadOptions.Parse(args);
            if (options.ShowHelp)
            {
                PrintHelp();
                return 0;
            }

            try
            {
                PrintHeader("Less3 Load Generator");
                PrintOptions(options);

                LoadGeneratorRunner runner = new LoadGeneratorRunner(options);
                IReadOnlyList<PhaseResult> results = await runner.RunAsync().ConfigureAwait(false);

                Console.WriteLine();
                PrintResults(results);

                return results.Any(r => r.Failed > 0) ? 1 : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Load generation failed: " + ex.Message);
                Console.ResetColor();
                return 1;
            }
        }

        private static void PrintHelp()
        {
            PrintHeader("Less3 Load Generator");
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run --project tools/LoadGenerator -- --server http://127.0.0.1:8000 [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --server <url>       Less3 server URL. Default: http://127.0.0.1:8000");
            Console.WriteLine("  --timeframe <value>  Synthetic time window such as 12h, 7d, or 00:30:00. Default: 7d");
            Console.WriteLine("  --density <value>    low, medium, high, or extreme. Default: medium");
            Console.WriteLine("  --tenant-id <id>     Tenant Id. Default: default");
            Console.WriteLine("  --admin-key <key>    REST/admin API key. Default: less3admin");
            Console.WriteLine("  --access-key <key>   S3 access key. Default: default");
            Console.WriteLine("  --secret-key <key>   S3 secret key. Default: default");
            Console.WriteLine("  --prefix <value>     Bucket/object naming prefix. Default: demo");
            Console.WriteLine("  --parallelism <n>    Override profile concurrency.");
            Console.WriteLine("  --dry-run            Print planned work without writing data.");
            Console.WriteLine("  --help               Show help.");
        }

        private static void PrintHeader(string title)
        {
            string line = new string('=', Math.Max(32, title.Length + 8));
            Console.WriteLine(line);
            Console.WriteLine("  " + title);
            Console.WriteLine(line);
        }

        private static void PrintOptions(LoadOptions options)
        {
            Console.WriteLine();
            Console.WriteLine("Configuration");
            Console.WriteLine("-------------");
            Console.WriteLine("Server             : " + options.Server);
            Console.WriteLine("Tenant             : " + options.TenantId);
            Console.WriteLine("Density            : " + options.Profile.Name);
            Console.WriteLine("Timeframe          : " + FormatDuration(options.Timeframe));
            Console.WriteLine("Buckets            : " + options.Profile.BucketCount);
            Console.WriteLine("Objects per bucket : " + options.Profile.ObjectsPerBucket);
            Console.WriteLine("Request rows       : " + options.Profile.RequestHistoryRows);
            Console.WriteLine("Live traffic ops   : " + options.Profile.LiveTrafficOps);
            Console.WriteLine("Parallelism        : " + options.Parallelism);
            Console.WriteLine("Dry run            : " + options.DryRun);
        }

        private static void PrintResults(IReadOnlyList<PhaseResult> results)
        {
            Console.WriteLine("Results");
            Console.WriteLine("-------");
            string header = FormatRow("Phase", "Target", "Created", "Failed", "Elapsed", "Rate/sec");
            Console.WriteLine(header);
            Console.WriteLine(new string('-', header.Length));

            foreach (PhaseResult result in results)
            {
                Console.WriteLine(FormatRow(
                    result.Name,
                    result.Target.ToString(),
                    result.Created.ToString(),
                    result.Failed.ToString(),
                    FormatDuration(result.Elapsed),
                    result.RatePerSecond.ToString("0.00")));
            }
        }

        private static string FormatRow(string phase, string target, string created, string failed, string elapsed, string rate)
        {
            return phase.PadRight(28)
                + target.PadLeft(10)
                + created.PadLeft(10)
                + failed.PadLeft(10)
                + elapsed.PadLeft(14)
                + rate.PadLeft(12);
        }

        private static string FormatDuration(TimeSpan value)
        {
            if (value.TotalDays >= 1) return value.TotalDays.ToString("0.##") + "d";
            if (value.TotalHours >= 1) return value.TotalHours.ToString("0.##") + "h";
            if (value.TotalMinutes >= 1) return value.TotalMinutes.ToString("0.##") + "m";
            return value.TotalSeconds.ToString("0.##") + "s";
        }
    }

    internal sealed class LoadGeneratorRunner
    {
        private readonly LoadOptions _Options;
        private readonly AmazonS3Client _S3;
        private readonly HttpClient _Http;
        private readonly List<string> _Buckets = new List<string>();
        private readonly ConcurrentBag<SyntheticObject> _Objects = new ConcurrentBag<SyntheticObject>();
        private readonly DateTime _EndUtc = DateTime.UtcNow;
        private readonly DateTime _StartUtc;

        internal LoadGeneratorRunner(LoadOptions options)
        {
            _Options = options;
            _StartUtc = _EndUtc.Subtract(options.Timeframe);

            BasicAWSCredentials credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
            AmazonS3Config config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.USWest1,
                ServiceURL = options.Server.TrimEnd('/') + "/",
                ForcePathStyle = true,
                UseHttp = options.Server.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
                MaxErrorRetry = 0,
                Timeout = TimeSpan.FromSeconds(30)
            };

            _S3 = new AmazonS3Client(credentials, config);
            _Http = new HttpClient();
            _Http.Timeout = TimeSpan.FromSeconds(30);
            _Http.DefaultRequestHeaders.Add("x-api-key", options.AdminKey);
        }

        internal async Task<IReadOnlyList<PhaseResult>> RunAsync()
        {
            List<PhaseResult> results = new List<PhaseResult>();

            results.Add(await RunPhaseAsync("Buckets", _Options.Profile.BucketCount, CreateBucketAsync).ConfigureAwait(false));
            results.Add(await RunPhaseAsync(
                "Objects",
                _Options.Profile.BucketCount * _Options.Profile.ObjectsPerBucket,
                CreateObjectAsync).ConfigureAwait(false));
            int bucketTagTarget = _Options.DryRun ? _Options.Profile.BucketCount : _Buckets.Count;
            int objectTagTarget = _Options.DryRun ? _Options.Profile.ObjectTagCount : Math.Min(_Objects.Count, _Options.Profile.ObjectTagCount);
            results.Add(await RunPhaseAsync("Bucket tags", bucketTagTarget, CreateBucketTagsAsync).ConfigureAwait(false));
            results.Add(await RunPhaseAsync("Object tags", objectTagTarget, CreateObjectTagsAsync).ConfigureAwait(false));
            results.Add(await RunPhaseAsync("Live traffic", _Options.Profile.LiveTrafficOps, RunLiveTrafficAsync).ConfigureAwait(false));
            results.Add(await RunPhaseAsync("Request history", _Options.Profile.RequestHistoryRows, CreateRequestHistoryAsync).ConfigureAwait(false));

            _S3.Dispose();
            _Http.Dispose();
            return results;
        }

        private async Task<PhaseResult> RunPhaseAsync(string name, int target, Func<int, Task> operation)
        {
            if (target < 1) return PhaseResult.Empty(name);

            if (_Options.DryRun)
            {
                return new PhaseResult(name, target, target, 0, TimeSpan.Zero);
            }

            int created = 0;
            int failed = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();

            await Parallel.ForEachAsync(
                Enumerable.Range(0, target),
                new ParallelOptions { MaxDegreeOfParallelism = _Options.Parallelism },
                async (index, cancellationToken) =>
                {
                    try
                    {
                        await operation(index).ConfigureAwait(false);
                        System.Threading.Interlocked.Increment(ref created);
                    }
                    catch
                    {
                        System.Threading.Interlocked.Increment(ref failed);
                    }
                }).ConfigureAwait(false);

            stopwatch.Stop();
            Console.WriteLine(name.PadRight(22) + " created " + created + " of " + target + " in " + stopwatch.Elapsed.TotalSeconds.ToString("0.00") + "s");
            return new PhaseResult(name, target, created, failed, stopwatch.Elapsed);
        }

        private async Task CreateBucketAsync(int index)
        {
            string name = CreateBucketName(index);
            PutBucketResponse response = await _S3.PutBucketAsync(new PutBucketRequest
            {
                BucketName = name
            }).ConfigureAwait(false);

            EnsureSuccess(response.HttpStatusCode, "PutBucket");
            lock (_Buckets) _Buckets.Add(name);
        }

        private async Task CreateObjectAsync(int index)
        {
            string bucket = _Buckets[index % _Buckets.Count];
            SyntheticObject obj = CreateSyntheticObject(bucket, index);
            byte[] payload = CreatePayload(obj.SizeBytes, index);

            using MemoryStream stream = new MemoryStream(payload, writable: false);
            PutObjectRequest request = new PutObjectRequest
            {
                BucketName = bucket,
                Key = obj.Key,
                InputStream = stream,
                ContentType = obj.ContentType
            };
            request.Metadata.Add("less3-demo-created-utc", obj.CreatedUtc.ToString("O"));
            request.Metadata.Add("less3-demo-scenario", obj.Scenario);

            PutObjectResponse response = await _S3.PutObjectAsync(request).ConfigureAwait(false);

            EnsureSuccess(response.HttpStatusCode, "PutObject");
            _Objects.Add(obj);
        }

        private async Task CreateBucketTagsAsync(int index)
        {
            string bucket = _Buckets[index % _Buckets.Count];
            PutBucketTaggingResponse response = await _S3.PutBucketTaggingAsync(new PutBucketTaggingRequest
            {
                BucketName = bucket,
                TagSet = new List<Tag>
                {
                    new Tag { Key = "demo", Value = "true" },
                    new Tag { Key = "density", Value = _Options.Profile.Name },
                    new Tag { Key = "segment", Value = SegmentFor(index) }
                }
            }).ConfigureAwait(false);

            EnsureSuccess(response.HttpStatusCode, "PutBucketTagging");
        }

        private async Task CreateObjectTagsAsync(int index)
        {
            SyntheticObject obj = _Objects.ElementAt(index % _Objects.Count);
            PutObjectTaggingResponse response = await _S3.PutObjectTaggingAsync(new PutObjectTaggingRequest
            {
                BucketName = obj.Bucket,
                Key = obj.Key,
                Tagging = new Tagging
                {
                    TagSet = new List<Tag>
                    {
                        new Tag { Key = "scenario", Value = obj.Scenario },
                        new Tag { Key = "density", Value = _Options.Profile.Name }
                    }
                }
            }).ConfigureAwait(false);

            EnsureSuccess(response.HttpStatusCode, "PutObjectTagging");
        }

        private async Task RunLiveTrafficAsync(int index)
        {
            if (_Objects.Count < 1) return;
            SyntheticObject obj = _Objects.ElementAt(index % _Objects.Count);
            int selector = index % 5;

            if (selector == 0)
            {
                ListObjectsV2Response list = await _S3.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = obj.Bucket,
                    Prefix = PrefixOf(obj.Key),
                    MaxKeys = 25
                }).ConfigureAwait(false);
                EnsureSuccess(list.HttpStatusCode, "ListObjectsV2");
                return;
            }

            if (selector == 1)
            {
                GetObjectMetadataResponse head = await _S3.GetObjectMetadataAsync(new GetObjectMetadataRequest
                {
                    BucketName = obj.Bucket,
                    Key = obj.Key
                }).ConfigureAwait(false);
                EnsureSuccess(head.HttpStatusCode, "HeadObject");
                return;
            }

            using GetObjectResponse get = await _S3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = obj.Bucket,
                Key = obj.Key
            }).ConfigureAwait(false);
            EnsureSuccess(get.HttpStatusCode, "GetObject");
            using MemoryStream sink = new MemoryStream();
            await get.ResponseStream.CopyToAsync(sink).ConfigureAwait(false);
        }

        private async Task CreateRequestHistoryAsync(int index)
        {
            SyntheticObject obj = _Objects.Count > 0
                ? _Objects.ElementAt(index % _Objects.Count)
                : new SyntheticObject("unknown", "unknown", "synthetic", "application/octet-stream", 0, _EndUtc);
            Random random = new Random(_Options.Seed + index);
            string requestType = RequestTypeFor(random);
            string method = MethodFor(requestType);
            int statusCode = StatusCodeFor(random);
            long durationMs = DurationFor(requestType, random);
            DateTime createdUtc = TimestampFor(index, _Options.Profile.RequestHistoryRows, random);

            object row = new
            {
                Id = IdGenerator.GenerateRequestHistoryId(),
                TenantId = _Options.TenantId,
                HttpMethod = method,
                RequestUrl = _Options.Server.TrimEnd('/') + "/" + obj.Bucket + "/" + obj.Key,
                SourceIp = SourceIpFor(random),
                StatusCode = statusCode,
                Success = statusCode < 400,
                DurationMs = durationMs,
                RequestType = requestType,
                UserId = "usr_default_admin",
                AccessKey = _Options.AccessKey,
                RequestContentType = method == "PUT" || method == "POST" ? obj.ContentType : null,
                RequestBodyLength = method == "PUT" || method == "POST" ? obj.SizeBytes : 0,
                ResponseContentType = obj.ContentType,
                ResponseBodyLength = method == "GET" && statusCode < 400 ? obj.SizeBytes : 0,
                RequestBody = (string?)null,
                ResponseBody = (string?)null,
                CreatedUtc = createdUtc
            };

            using HttpResponseMessage response = await PostRestAsync("requesthistory?tenantId=" + _Options.TenantId, row).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.Created)
            {
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new InvalidOperationException("Request history create failed: " + body);
            }
        }

        private async Task<HttpResponseMessage> PostRestAsync(string path, object body)
        {
            string json = JsonSerializer.Serialize(body);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _Options.Server.TrimEnd('/') + "/api/v1/" + path);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await _Http.SendAsync(request).ConfigureAwait(false);
        }

        private string CreateBucketName(int index)
        {
            string token = IdGenerator.GenerateBucketId().Replace("_", "-", StringComparison.Ordinal).ToLowerInvariant();
            if (token.Length > 12) token = token.Substring(0, 12);
            string prefix = _Options.Prefix.ToLowerInvariant().Replace("_", "-", StringComparison.Ordinal);
            return prefix + "-" + SegmentFor(index) + "-" + token + "-" + index.ToString("D2");
        }

        private SyntheticObject CreateSyntheticObject(string bucket, int index)
        {
            Random random = new Random(_Options.Seed + index);
            DateTime createdUtc = TimestampFor(index, _Options.Profile.BucketCount * _Options.Profile.ObjectsPerBucket, random);
            string scenario = ScenarioFor(random);
            string extension = ExtensionFor(scenario);
            string contentType = ContentTypeFor(extension);
            int size = SizeFor(scenario, random);
            string key = scenario + "/"
                + createdUtc.ToString("yyyy/MM/dd/HH")
                + "/asset-" + index.ToString("D7")
                + extension;

            return new SyntheticObject(bucket, key, scenario, contentType, size, createdUtc);
        }

        private DateTime TimestampFor(int index, int total, Random random)
        {
            if (total < 1) total = 1;
            double position = index / (double)total;
            double wave = (Math.Sin(position * Math.PI * 8) + 1) / 2;
            double jitter = random.NextDouble() * 0.04;
            double weighted = Math.Clamp((position * 0.85) + (wave * 0.11) + jitter, 0, 1);
            return _StartUtc.AddTicks((long)(_Options.Timeframe.Ticks * weighted));
        }

        private byte[] CreatePayload(int bytes, int index)
        {
            byte[] data = new byte[bytes];
            Random random = new Random(_Options.Seed + index);
            random.NextBytes(data);
            return data;
        }

        private int SizeFor(string scenario, Random random)
        {
            int min = scenario == "media" ? 16384 : 512;
            int max = scenario == "media" ? _Options.Profile.MaxObjectBytes : Math.Max(1024, _Options.Profile.MaxObjectBytes / 8);
            return random.Next(min, Math.Max(min + 1, max));
        }

        private static string PrefixOf(string key)
        {
            int index = key.LastIndexOf('/');
            if (index < 0) return String.Empty;
            return key.Substring(0, index + 1);
        }

        private static string SegmentFor(int index)
        {
            string[] segments = new[] { "media", "logs", "exports", "backups", "invoices", "events" };
            return segments[index % segments.Length];
        }

        private static string ScenarioFor(Random random)
        {
            string[] scenarios = new[] { "media", "media", "logs", "exports", "backups", "events", "reports" };
            return scenarios[random.Next(scenarios.Length)];
        }

        private static string ExtensionFor(string scenario)
        {
            return scenario switch
            {
                "media" => ".jpg",
                "logs" => ".jsonl",
                "exports" => ".csv",
                "backups" => ".bin",
                "events" => ".json",
                _ => ".txt"
            };
        }

        private static string ContentTypeFor(string extension)
        {
            return extension switch
            {
                ".jpg" => "image/jpeg",
                ".jsonl" => "application/jsonl",
                ".csv" => "text/csv",
                ".json" => "application/json",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        private static string RequestTypeFor(Random random)
        {
            int value = random.Next(100);
            if (value < 45) return "GetObject";
            if (value < 70) return "PutObject";
            if (value < 84) return "ListObjects";
            if (value < 94) return "HeadObject";
            return "RestEnumerate";
        }

        private static string MethodFor(string requestType)
        {
            return requestType switch
            {
                "PutObject" => "PUT",
                "RestEnumerate" => "POST",
                "HeadObject" => "HEAD",
                _ => "GET"
            };
        }

        private static int StatusCodeFor(Random random)
        {
            int value = random.Next(100);
            if (value < 83) return 200;
            if (value < 90) return 204;
            if (value < 94) return 304;
            if (value < 97) return 403;
            if (value < 99) return 404;
            return 500;
        }

        private static long DurationFor(string requestType, Random random)
        {
            int baseline = requestType switch
            {
                "PutObject" => 35,
                "ListObjects" => 22,
                "RestEnumerate" => 18,
                "HeadObject" => 8,
                _ => 14
            };

            double burst = random.NextDouble() < 0.08 ? random.Next(80, 350) : 0;
            return baseline + random.Next(0, baseline * 3) + (long)burst;
        }

        private static string SourceIpFor(Random random)
        {
            return "10." + random.Next(1, 240) + "." + random.Next(0, 255) + "." + random.Next(1, 255);
        }

        private static void EnsureSuccess(HttpStatusCode statusCode, string operation)
        {
            if ((int)statusCode < 200 || (int)statusCode > 299)
            {
                throw new InvalidOperationException(operation + " failed with HTTP " + (int)statusCode + ".");
            }
        }
    }

    internal sealed class LoadOptions
    {
        internal string Server { get; private set; } = "http://127.0.0.1:8000";
        internal string TenantId { get; private set; } = "default";
        internal string AdminKey { get; private set; } = "less3admin";
        internal string AccessKey { get; private set; } = "default";
        internal string SecretKey { get; private set; } = "default";
        internal string Prefix { get; private set; } = "demo";
        internal TimeSpan Timeframe { get; private set; } = TimeSpan.FromDays(7);
        internal DensityProfile Profile { get; private set; } = DensityProfile.Medium;
        internal int Parallelism { get; private set; } = DensityProfile.Medium.Parallelism;
        internal int Seed { get; private set; } = RandomNumberGenerator.GetInt32(1, Int32.MaxValue);
        internal bool DryRun { get; private set; }
        internal bool ShowHelp { get; private set; }

        internal static LoadOptions Parse(string[] args)
        {
            LoadOptions options = new LoadOptions();
            bool parallelismOverridden = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase))
                {
                    options.ShowHelp = true;
                    return options;
                }

                if (arg.Equals("--dry-run", StringComparison.OrdinalIgnoreCase))
                {
                    options.DryRun = true;
                    continue;
                }

                if (arg.Equals("--server", StringComparison.OrdinalIgnoreCase))
                {
                    options.Server = ReadString(args, ref i, arg).TrimEnd('/');
                    continue;
                }

                if (arg.Equals("--tenant-id", StringComparison.OrdinalIgnoreCase))
                {
                    options.TenantId = ReadString(args, ref i, arg);
                    continue;
                }

                if (arg.Equals("--admin-key", StringComparison.OrdinalIgnoreCase))
                {
                    options.AdminKey = ReadString(args, ref i, arg);
                    continue;
                }

                if (arg.Equals("--access-key", StringComparison.OrdinalIgnoreCase))
                {
                    options.AccessKey = ReadString(args, ref i, arg);
                    continue;
                }

                if (arg.Equals("--secret-key", StringComparison.OrdinalIgnoreCase))
                {
                    options.SecretKey = ReadString(args, ref i, arg);
                    continue;
                }

                if (arg.Equals("--prefix", StringComparison.OrdinalIgnoreCase))
                {
                    options.Prefix = ReadString(args, ref i, arg);
                    continue;
                }

                if (arg.Equals("--timeframe", StringComparison.OrdinalIgnoreCase))
                {
                    options.Timeframe = ParseTimeframe(ReadString(args, ref i, arg));
                    continue;
                }

                if (arg.Equals("--density", StringComparison.OrdinalIgnoreCase))
                {
                    options.Profile = DensityProfile.FromName(ReadString(args, ref i, arg));
                    if (!parallelismOverridden) options.Parallelism = options.Profile.Parallelism;
                    continue;
                }

                if (arg.Equals("--parallelism", StringComparison.OrdinalIgnoreCase))
                {
                    options.Parallelism = Math.Clamp(ReadInt(args, ref i, arg), 1, 512);
                    parallelismOverridden = true;
                    continue;
                }

                if (arg.Equals("--seed", StringComparison.OrdinalIgnoreCase))
                {
                    options.Seed = ReadInt(args, ref i, arg);
                    continue;
                }

                throw new ArgumentException("Unknown argument: " + arg);
            }

            if (!Uri.TryCreate(options.Server, UriKind.Absolute, out Uri? uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("--server must be an absolute HTTP or HTTPS URL.");
            }

            return options;
        }

        private static string ReadString(string[] args, ref int index, string name)
        {
            if (index + 1 >= args.Length) throw new ArgumentException(name + " requires a value.");
            index++;
            if (String.IsNullOrWhiteSpace(args[index])) throw new ArgumentException(name + " cannot be empty.");
            return args[index];
        }

        private static int ReadInt(string[] args, ref int index, string name)
        {
            string value = ReadString(args, ref index, name);
            if (!Int32.TryParse(value, out int parsed)) throw new ArgumentException(name + " must be an integer.");
            return parsed;
        }

        private static TimeSpan ParseTimeframe(string value)
        {
            if (TimeSpan.TryParse(value, out TimeSpan parsed)) return parsed;

            string suffix = value.Substring(value.Length - 1).ToLowerInvariant();
            string number = value.Substring(0, value.Length - 1);
            if (!Double.TryParse(number, out double amount)) throw new ArgumentException("--timeframe is invalid.");

            return suffix switch
            {
                "s" => TimeSpan.FromSeconds(amount),
                "m" => TimeSpan.FromMinutes(amount),
                "h" => TimeSpan.FromHours(amount),
                "d" => TimeSpan.FromDays(amount),
                _ => throw new ArgumentException("--timeframe suffix must be s, m, h, or d.")
            };
        }
    }

    internal sealed class DensityProfile
    {
        internal static readonly DensityProfile Low = new DensityProfile("low", 3, 20, 40, 120, 12, 65536, 2);
        internal static readonly DensityProfile Medium = new DensityProfile("medium", 6, 75, 200, 1000, 75, 262144, 6);
        internal static readonly DensityProfile High = new DensityProfile("high", 12, 250, 750, 5000, 300, 1048576, 12);
        internal static readonly DensityProfile Extreme = new DensityProfile("extreme", 24, 1000, 2500, 20000, 1200, 2097152, 24);

        internal string Name { get; }
        internal int BucketCount { get; }
        internal int ObjectsPerBucket { get; }
        internal int LiveTrafficOps { get; }
        internal int RequestHistoryRows { get; }
        internal int ObjectTagCount { get; }
        internal int MaxObjectBytes { get; }
        internal int Parallelism { get; }

        private DensityProfile(
            string name,
            int bucketCount,
            int objectsPerBucket,
            int liveTrafficOps,
            int requestHistoryRows,
            int objectTagCount,
            int maxObjectBytes,
            int parallelism)
        {
            Name = name;
            BucketCount = bucketCount;
            ObjectsPerBucket = objectsPerBucket;
            LiveTrafficOps = liveTrafficOps;
            RequestHistoryRows = requestHistoryRows;
            ObjectTagCount = objectTagCount;
            MaxObjectBytes = maxObjectBytes;
            Parallelism = parallelism;
        }

        internal static DensityProfile FromName(string name)
        {
            if (name.Equals("low", StringComparison.OrdinalIgnoreCase)) return Low;
            if (name.Equals("medium", StringComparison.OrdinalIgnoreCase)) return Medium;
            if (name.Equals("high", StringComparison.OrdinalIgnoreCase)) return High;
            if (name.Equals("extreme", StringComparison.OrdinalIgnoreCase)) return Extreme;
            throw new ArgumentException("--density must be low, medium, high, or extreme.");
        }
    }

    internal sealed record SyntheticObject(
        string Bucket,
        string Key,
        string Scenario,
        string ContentType,
        int SizeBytes,
        DateTime CreatedUtc);

    internal sealed record PhaseResult(string Name, int Target, int Created, int Failed, TimeSpan Elapsed)
    {
        internal double RatePerSecond => Elapsed.TotalSeconds <= 0 ? 0 : Created / Elapsed.TotalSeconds;

        internal static PhaseResult Empty(string name)
        {
            return new PhaseResult(name, 0, 0, 0, TimeSpan.Zero);
        }
    }
}
