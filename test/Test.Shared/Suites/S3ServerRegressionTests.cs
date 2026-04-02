namespace Test.Shared.Suites
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using S3ServerLibrary;
    using S3ServerLibrary.S3Objects;
    using WatsonWebserver.Core;

    /// <summary>
    /// Direct regression checks for upstream S3Server behavior.
    /// </summary>
    public class S3ServerRegressionTests : TestSuite
    {
        #region Public-Members

        /// <summary>
        /// The display name of this test suite.
        /// </summary>
        public override string Name => "S3Server Regression Tests";

        #endregion

        #region Public-Methods

        /// <summary>
        /// Runs direct S3Server regression tests without Less3 in the middle.
        /// </summary>
        public override async Task RunTestsAsync()
        {
            await RunTest("S3Server_EnableSignatures_RequiresSecretCallback", () =>
            {
                S3ServerSettings settings = CreateServerSettings(GetRandomPort());
                settings.EnableSignatures = true;

                using S3Server server = new S3Server(settings);
                InvalidOperationException exception = AssertThrows<InvalidOperationException>(() => server.Start());
                AssertContains(exception.Message, "Service.GetSecretKey");
            });

            await RunTest("S3Server_ObjectReadRange_Returns206", async () =>
            {
                int port = GetRandomPort();
                S3ServerSettings settings = CreateServerSettings(port);
                using S3Server server = new S3Server(settings);
                using HttpClient client = CreateHttpClient();

                server.Object.ReadRange = ctx =>
                {
                    S3Object obj = new S3Object(
                        ctx.Request.Key,
                        "1",
                        true,
                        DateTime.UtcNow,
                        "\"etag-123\"",
                        5,
                        new Owner("owner-1", "owner"),
                        "ABCDE",
                        "text/plain");

                    return Task.FromResult(obj);
                };

                server.Start();

                try
                {
                    await WaitForServerReadyAsync(client, port).ConfigureAwait(false);

                    HttpRequestMessage request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"http://127.0.0.1:{port}/bucket/range.txt");
                    request.Headers.Range = new RangeHeaderValue(0, 4);

                    HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.PartialContent, response.StatusCode);
                    AssertTrue(response.Headers.AcceptRanges.Contains("bytes"), "Expected Accept-Ranges header");

                    string? contentRange = response.Content.Headers.TryGetValues("Content-Range", out var values)
                        ? values.FirstOrDefault()
                        : null;
                    AssertNotNull(contentRange);
                    AssertContains(contentRange!, "bytes 0-4/");

                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    AssertEqual("ABCDE", body);
                }
                finally
                {
                    server.Stop();
                }
            });

            await RunTest("S3Server_UnwiredRecognizedRequest_ReturnsNotImplemented", async () =>
            {
                int port = GetRandomPort();
                S3ServerSettings settings = CreateServerSettings(port);
                using S3Server server = new S3Server(settings);
                using HttpClient client = CreateHttpClient();

                server.Start();

                try
                {
                    await WaitForServerReadyAsync(client, port).ConfigureAwait(false);

                    HttpResponseMessage response = await client.GetAsync($"http://127.0.0.1:{port}/bucket?website").ConfigureAwait(false);
                    AssertEqual((HttpStatusCode)501, response.StatusCode);

                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    AssertContains(body, "<Code>NotImplemented</Code>");
                }
                finally
                {
                    server.Stop();
                }
            });
        }

        #endregion

        #region Private-Methods

        private static HttpClient CreateHttpClient()
        {
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            return client;
        }

        private static S3ServerSettings CreateServerSettings(int port)
        {
            S3ServerSettings settings = new S3ServerSettings();
            settings.Webserver = new WebserverSettings("127.0.0.1", port, false);
            settings.Logger = _ => { };
            return settings;
        }

        private static int GetRandomPort()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static async Task WaitForServerReadyAsync(HttpClient client, int port)
        {
            string url = $"http://127.0.0.1:{port}/";

            for (int i = 0; i < 40; i++)
            {
                try
                {
                    using HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);
                    return;
                }
                catch (HttpRequestException)
                {
                }
                catch (TaskCanceledException)
                {
                }

                await Task.Delay(100).ConfigureAwait(false);
            }

            throw new TimeoutException("S3Server did not become ready in time.");
        }

        #endregion
    }
}
