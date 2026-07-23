namespace Test.PerformanceBenchmark
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.S3.Model;
    using Test.Shared;

    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            BenchmarkOptions options = BenchmarkOptions.Parse(args);
            if (options.ShowHelp)
            {
                PrintHelp();
                return 0;
            }

            using Less3TestServer server = new Less3TestServer();

            try
            {
                PrintHeader("Less3 Performance Benchmark");
                PrintOptions(options);

                Console.WriteLine();
                Console.WriteLine("Starting temporary Less3 server...");
                await server.StartAsync().ConfigureAwait(false);
                Console.WriteLine("Server ready: " + server.BaseUrl);

                BenchmarkRunner runner = new BenchmarkRunner(server, options);
                IReadOnlyList<BenchmarkResult> results = await runner.RunAsync().ConfigureAwait(false);

                Console.WriteLine();
                PrintResults(results);

                return results.Any(r => r.Errors > 0) ? 1 : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Benchmark failed: " + ex.Message);
                Console.ResetColor();
                return 1;
            }
        }

        private static void PrintHelp()
        {
            PrintHeader("Less3 Performance Benchmark");
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run --project test/Test.PerformanceBenchmark -- [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --buckets <n>             Number of temporary buckets to create. Default: 2");
            Console.WriteLine("  --objects-per-bucket <n>  Objects to write per bucket. Default: 25");
            Console.WriteLine("  --object-bytes <n>        Bytes per object payload. Default: 1024");
            Console.WriteLine("  --parallelism <n>         Max concurrent operations. Default: 4");
            Console.WriteLine("  --list-page-size <n>      MaxKeys/page size for list/enumerate tests. Default: 10");
            Console.WriteLine("  --skip-rest               Skip REST enumeration benchmarks.");
            Console.WriteLine("  --skip-s3                 Skip S3 API benchmarks.");
            Console.WriteLine("  --help                    Show help.");
        }

        private static void PrintHeader(string title)
        {
            string line = new string('=', Math.Max(32, title.Length + 8));
            Console.WriteLine(line);
            Console.WriteLine("  " + title);
            Console.WriteLine(line);
        }

        private static void PrintOptions(BenchmarkOptions options)
        {
            Console.WriteLine();
            Console.WriteLine("Configuration");
            Console.WriteLine("-------------");
            Console.WriteLine("Buckets            : " + options.Buckets);
            Console.WriteLine("Objects per bucket : " + options.ObjectsPerBucket);
            Console.WriteLine("Object bytes       : " + options.ObjectBytes);
            Console.WriteLine("Parallelism        : " + options.Parallelism);
            Console.WriteLine("List page size     : " + options.ListPageSize);
            Console.WriteLine("S3 benchmarks      : " + (!options.SkipS3));
            Console.WriteLine("REST benchmarks    : " + (!options.SkipRest));
        }

        private static void PrintResults(IReadOnlyList<BenchmarkResult> results)
        {
            Console.WriteLine("Results");
            Console.WriteLine("-------");

            string header = FormatRow("Benchmark", "Ops", "Avg ms", "P50", "P95", "P99", "Ops/sec", "Errors");
            Console.WriteLine(header);
            Console.WriteLine(new string('-', header.Length));

            foreach (BenchmarkResult result in results)
            {
                Console.WriteLine(FormatRow(
                    result.Name,
                    result.Operations.ToString(),
                    result.AverageMs.ToString("0.00"),
                    result.P50Ms.ToString("0.00"),
                    result.P95Ms.ToString("0.00"),
                    result.P99Ms.ToString("0.00"),
                    result.OperationsPerSecond.ToString("0.00"),
                    result.Errors.ToString()));
            }
        }

        private static string FormatRow(
            string name,
            string ops,
            string avg,
            string p50,
            string p95,
            string p99,
            string opsPerSecond,
            string errors)
        {
            return name.PadRight(36)
                + ops.PadLeft(8)
                + avg.PadLeft(10)
                + p50.PadLeft(10)
                + p95.PadLeft(10)
                + p99.PadLeft(10)
                + opsPerSecond.PadLeft(12)
                + errors.PadLeft(9);
        }
    }

    internal sealed class BenchmarkRunner
    {
        private readonly Less3TestServer _Server;
        private readonly BenchmarkOptions _Options;
        private readonly List<string> _Buckets = new List<string>();
        private readonly Dictionary<string, string> _BucketIds = new Dictionary<string, string>();
        private readonly byte[] _Payload;

        internal BenchmarkRunner(Less3TestServer server, BenchmarkOptions options)
        {
            _Server = server;
            _Options = options;
            _Payload = Enumerable.Range(0, options.ObjectBytes).Select(i => (byte)(i % 251)).ToArray();
        }

        internal async Task<IReadOnlyList<BenchmarkResult>> RunAsync()
        {
            List<BenchmarkResult> results = new List<BenchmarkResult>();
            await SeedBenchmarkCredentialAsync().ConfigureAwait(false);

            if (!_Options.SkipS3)
            {
                results.Add(await MeasureAsync(
                    "S3 CreateBucket",
                    _Options.Buckets,
                    _Options.Parallelism,
                    CreateBucketAsync).ConfigureAwait(false));

                foreach (string bucket in _Buckets)
                {
                    _BucketIds[bucket] = await GetBucketIdAsync(bucket).ConfigureAwait(false);
                }

                int objectOps = _Options.Buckets * _Options.ObjectsPerBucket;
                results.Add(await MeasureAsync(
                    "S3 PutObject",
                    objectOps,
                    _Options.Parallelism,
                    PutObjectAsync).ConfigureAwait(false));

                results.Add(await MeasureAsync(
                    "S3 GetObject",
                    objectOps,
                    _Options.Parallelism,
                    GetObjectAsync).ConfigureAwait(false));

                results.Add(await MeasureAsync(
                    "S3 ListObjectsV2",
                    Math.Max(_Options.Buckets * 5, 1),
                    _Options.Parallelism,
                    ListObjectsAsync).ConfigureAwait(false));
            }
            else
            {
                await SeedBucketsAndObjectsAsync().ConfigureAwait(false);
            }

            if (!_Options.SkipRest)
            {
                if (_BucketIds.Count < 1)
                {
                    foreach (string bucket in _Buckets)
                    {
                        _BucketIds[bucket] = await GetBucketIdAsync(bucket).ConfigureAwait(false);
                    }
                }

                results.Add(await MeasureAsync(
                    "REST Enumerate Objects",
                    Math.Max(_Options.Buckets * 5, 1),
                    _Options.Parallelism,
                    EnumerateObjectsAsync).ConfigureAwait(false));

                results.Add(await MeasureAsync(
                    "REST Enumerate RequestHistory",
                    10,
                    _Options.Parallelism,
                    EnumerateRequestHistoryAsync).ConfigureAwait(false));
            }

            return results;
        }

        private async Task SeedBenchmarkCredentialAsync()
        {
            string userId = TestIds.User();
            string credentialId = TestIds.Credential();

            using HttpResponseMessage userResponse = await _Server.AdminPostAsync("users", JsonSerializer.Serialize(new
            {
                Id = userId,
                Name = "BenchmarkUser",
                Email = "benchmark-" + TestIds.Suffix().Substring(0, 8) + "@example.com"
            })).ConfigureAwait(false);
            await EnsureRestCreatedAsync(userResponse, "benchmark user").ConfigureAwait(false);

            using HttpResponseMessage credentialResponse = await _Server.AdminPostAsync("credentials", JsonSerializer.Serialize(new
            {
                Id = credentialId,
                UserId = userId,
                Description = "Performance benchmark credential",
                AccessKey = _Server.AccessKey,
                SecretKey = _Server.SecretKey,
                IsBase64 = false
            })).ConfigureAwait(false);
            await EnsureRestCreatedAsync(credentialResponse, "benchmark credential").ConfigureAwait(false);

            await _Server.GrantTenantAdminAsync("User", userId).ConfigureAwait(false);
            await _Server.GrantTenantAdminAsync("Credential", credentialId).ConfigureAwait(false);
        }

        private async Task SeedBucketsAndObjectsAsync()
        {
            BenchmarkResult createResult = await MeasureAsync(
                "S3 CreateBucket",
                _Options.Buckets,
                _Options.Parallelism,
                CreateBucketAsync).ConfigureAwait(false);

            if (createResult.Errors > 0)
            {
                throw new InvalidOperationException("Failed to seed benchmark buckets.");
            }

            foreach (string bucket in _Buckets)
            {
                _BucketIds[bucket] = await GetBucketIdAsync(bucket).ConfigureAwait(false);
            }

            BenchmarkResult putResult = await MeasureAsync(
                "S3 PutObject",
                _Options.Buckets * _Options.ObjectsPerBucket,
                _Options.Parallelism,
                PutObjectAsync).ConfigureAwait(false);

            if (putResult.Errors > 0)
            {
                throw new InvalidOperationException("Failed to seed benchmark objects.");
            }
        }

        private async Task CreateBucketAsync(int index)
        {
            string bucketName = "bench-" + TestIds.Suffix().Substring(0, 8) + "-" + index.ToString("D3");
            PutBucketResponse response = await _Server.S3Client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName
            }).ConfigureAwait(false);

            EnsureSuccess(response.HttpStatusCode, "CreateBucket");
            lock (_Buckets) _Buckets.Add(bucketName);
        }

        private async Task PutObjectAsync(int index)
        {
            string bucket = _Buckets[index % _Buckets.Count];
            string key = ObjectKey(index);
            using MemoryStream stream = new MemoryStream(_Payload, writable: false);
            PutObjectResponse response = await _Server.S3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = stream,
                ContentType = "application/octet-stream"
            }).ConfigureAwait(false);

            EnsureSuccess(response.HttpStatusCode, "PutObject");
        }

        private async Task GetObjectAsync(int index)
        {
            string bucket = _Buckets[index % _Buckets.Count];
            string key = ObjectKey(index);
            using GetObjectResponse response = await _Server.S3Client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = bucket,
                Key = key
            }).ConfigureAwait(false);

            EnsureSuccess(response.HttpStatusCode, "GetObject");
            using MemoryStream sink = new MemoryStream();
            await response.ResponseStream.CopyToAsync(sink).ConfigureAwait(false);
        }

        private async Task ListObjectsAsync(int index)
        {
            string bucket = _Buckets[index % _Buckets.Count];
            ListObjectsV2Response response = await _Server.S3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucket,
                Prefix = "bench/",
                MaxKeys = _Options.ListPageSize
            }).ConfigureAwait(false);

            EnsureSuccess(response.HttpStatusCode, "ListObjectsV2");
        }

        private async Task EnumerateObjectsAsync(int index)
        {
            string bucket = _Buckets[index % _Buckets.Count];
            string bucketId = _BucketIds[bucket];
            using HttpResponseMessage response = await _Server.RestPostAsync(
                "objects/enumerate?tenantId=default&bucketId=" + bucketId,
                JsonSerializer.Serialize(new
                {
                    Limit = _Options.ListPageSize,
                    Filters = new Dictionary<string, string>
                    {
                        { "prefix", "bench/" }
                    }
                })).ConfigureAwait(false);

            await EnsureRestSuccessAsync(response, "REST Enumerate Objects").ConfigureAwait(false);
        }

        private async Task EnumerateRequestHistoryAsync(int index)
        {
            using HttpResponseMessage response = await _Server.RestPostAsync(
                "requesthistory/enumerate?tenantId=default",
                JsonSerializer.Serialize(new
                {
                    Limit = _Options.ListPageSize,
                    SortField = "createdUtc",
                    SortDirection = "desc"
                })).ConfigureAwait(false);

            await EnsureRestSuccessAsync(response, "REST Enumerate RequestHistory").ConfigureAwait(false);
        }

        private async Task<string> GetBucketIdAsync(string bucketName)
        {
            using HttpResponseMessage response = await _Server.RestPostAsync(
                "buckets/enumerate?tenantId=default",
                JsonSerializer.Serialize(new
                {
                    Limit = 1,
                    Filters = new Dictionary<string, string>
                    {
                        { "name", bucketName }
                    }
                })).ConfigureAwait(false);

            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException("Failed to resolve bucket Id for " + bucketName + ": " + body);
            }

            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement items = document.RootElement.GetProperty("Items");
            if (items.GetArrayLength() != 1)
            {
                throw new InvalidOperationException("Bucket " + bucketName + " was not found in REST enumeration.");
            }

            return items[0].GetProperty("Id").GetString() ?? throw new InvalidOperationException("Bucket Id was empty.");
        }

        private string ObjectKey(int index)
        {
            return "bench/object-" + index.ToString("D6") + ".bin";
        }

        private static async Task<BenchmarkResult> MeasureAsync(
            string name,
            int operations,
            int parallelism,
            Func<int, Task> operation)
        {
            double[] samples = new double[operations];
            ConcurrentQueue<Exception> errors = new ConcurrentQueue<Exception>();

            Stopwatch total = Stopwatch.StartNew();
            await Parallel.ForEachAsync(
                Enumerable.Range(0, operations),
                new ParallelOptions { MaxDegreeOfParallelism = parallelism },
                async (index, cancellationToken) =>
                {
                    Stopwatch current = Stopwatch.StartNew();
                    try
                    {
                        await operation(index).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        errors.Enqueue(ex);
                    }
                    finally
                    {
                        current.Stop();
                        samples[index] = current.Elapsed.TotalMilliseconds;
                    }
                }).ConfigureAwait(false);
            total.Stop();

            return BenchmarkResult.FromSamples(name, operations, samples, total.Elapsed, errors.Count);
        }

        private static void EnsureSuccess(HttpStatusCode statusCode, string operation)
        {
            if ((int)statusCode < 200 || (int)statusCode > 299)
            {
                throw new InvalidOperationException(operation + " failed with HTTP " + (int)statusCode + ".");
            }
        }

        private static async Task EnsureRestSuccessAsync(HttpResponseMessage response, string operation)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new InvalidOperationException(operation + " failed with HTTP " + (int)response.StatusCode + ": " + body);
            }
        }

        private static async Task EnsureRestCreatedAsync(HttpResponseMessage response, string operation)
        {
            if (response.StatusCode != HttpStatusCode.Created)
            {
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new InvalidOperationException("Failed to create " + operation + "; HTTP " + (int)response.StatusCode + ": " + body);
            }
        }
    }

    internal sealed class BenchmarkOptions
    {
        internal int Buckets { get; private set; } = 2;
        internal int ObjectsPerBucket { get; private set; } = 25;
        internal int ObjectBytes { get; private set; } = 1024;
        internal int Parallelism { get; private set; } = 4;
        internal int ListPageSize { get; private set; } = 10;
        internal bool SkipRest { get; private set; }
        internal bool SkipS3 { get; private set; }
        internal bool ShowHelp { get; private set; }

        internal static BenchmarkOptions Parse(string[] args)
        {
            BenchmarkOptions options = new BenchmarkOptions();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase))
                {
                    options.ShowHelp = true;
                    return options;
                }

                if (arg.Equals("--skip-rest", StringComparison.OrdinalIgnoreCase))
                {
                    options.SkipRest = true;
                    continue;
                }

                if (arg.Equals("--skip-s3", StringComparison.OrdinalIgnoreCase))
                {
                    options.SkipS3 = true;
                    continue;
                }

                if (arg.Equals("--buckets", StringComparison.OrdinalIgnoreCase))
                {
                    options.Buckets = ReadInt(args, ref i, arg, 1, 1000);
                    continue;
                }

                if (arg.Equals("--objects-per-bucket", StringComparison.OrdinalIgnoreCase))
                {
                    options.ObjectsPerBucket = ReadInt(args, ref i, arg, 1, 1000000);
                    continue;
                }

                if (arg.Equals("--object-bytes", StringComparison.OrdinalIgnoreCase))
                {
                    options.ObjectBytes = ReadInt(args, ref i, arg, 1, 104857600);
                    continue;
                }

                if (arg.Equals("--parallelism", StringComparison.OrdinalIgnoreCase))
                {
                    options.Parallelism = ReadInt(args, ref i, arg, 1, 1024);
                    continue;
                }

                if (arg.Equals("--list-page-size", StringComparison.OrdinalIgnoreCase))
                {
                    options.ListPageSize = ReadInt(args, ref i, arg, 1, 1000);
                    continue;
                }

                throw new ArgumentException("Unknown argument: " + arg);
            }

            if (options.SkipS3 && options.SkipRest)
            {
                throw new ArgumentException("At least one benchmark family must be enabled.");
            }

            return options;
        }

        private static int ReadInt(string[] args, ref int index, string name, int min, int max)
        {
            if (index + 1 >= args.Length) throw new ArgumentException(name + " requires a value.");
            index++;
            if (!Int32.TryParse(args[index], out int value)) throw new ArgumentException(name + " must be an integer.");
            return Math.Clamp(value, min, max);
        }
    }

    internal sealed class BenchmarkResult
    {
        internal string Name { get; private set; } = null!;
        internal int Operations { get; private set; }
        internal double AverageMs { get; private set; }
        internal double P50Ms { get; private set; }
        internal double P95Ms { get; private set; }
        internal double P99Ms { get; private set; }
        internal double OperationsPerSecond { get; private set; }
        internal int Errors { get; private set; }

        internal static BenchmarkResult FromSamples(string name, int operations, double[] samples, TimeSpan elapsed, int errors)
        {
            double[] ordered = samples.OrderBy(s => s).ToArray();
            return new BenchmarkResult
            {
                Name = name,
                Operations = operations,
                AverageMs = ordered.Length == 0 ? 0 : ordered.Average(),
                P50Ms = Percentile(ordered, 50),
                P95Ms = Percentile(ordered, 95),
                P99Ms = Percentile(ordered, 99),
                OperationsPerSecond = elapsed.TotalSeconds <= 0 ? 0 : operations / elapsed.TotalSeconds,
                Errors = errors
            };
        }

        private static double Percentile(double[] ordered, int percentile)
        {
            if (ordered.Length == 0) return 0;
            int index = (int)Math.Ceiling(percentile / 100.0 * ordered.Length) - 1;
            index = Math.Clamp(index, 0, ordered.Length - 1);
            return ordered[index];
        }
    }
}
