namespace Test.Shared.Suites
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Less3.Storage;

    /// <summary>
    /// Tests for the DiskStorageDriver class.
    /// </summary>
    public class StorageTests : TestSuite
    {
        #region Private-Members

        private string _TestDirectory;

        #endregion

        #region Public-Members

        /// <summary>
        /// The display name of this test suite.
        /// </summary>
        public override string Name => "Storage Tests";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageTests"/> class.
        /// </summary>
        public StorageTests()
        {
            _TestDirectory = Path.Combine(Path.GetTempPath(), "less3-storage-test-" + Guid.NewGuid().ToString("N"));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Runs all storage tests.
        /// </summary>
        public override async Task RunTestsAsync()
        {
            Directory.CreateDirectory(_TestDirectory);

            try
            {
                await RunTest("DiskStorageDriver_WriteAndRead_ByteArray", () =>
                {
                    DiskStorageDriver driver = new DiskStorageDriver(_TestDirectory);
                    byte[] data = Encoding.UTF8.GetBytes("Hello, Less3!");
                    byte[] md5 = driver.Write("test1.txt", data);

                    AssertNotNull(md5, "MD5 hash should not be null");
                    AssertTrue(md5.Length > 0, "MD5 hash should have content");

                    byte[] readBack = driver.Read("test1.txt");
                    AssertEqual(data.Length, readBack.Length);
                    AssertEqual("Hello, Less3!", Encoding.UTF8.GetString(readBack));
                });

                await RunTest("DiskStorageDriver_WriteAndRead_Stream", () =>
                {
                    DiskStorageDriver driver = new DiskStorageDriver(_TestDirectory);
                    byte[] data = Encoding.UTF8.GetBytes("Stream test data");

                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        byte[] md5 = driver.Write("test2.txt", data.Length, ms);
                        AssertNotNull(md5);
                    }

                    byte[] readBack = driver.Read("test2.txt");
                    AssertEqual("Stream test data", Encoding.UTF8.GetString(readBack));
                });

                await RunTest("DiskStorageDriver_WriteAndReadAsync", async () =>
                {
                    DiskStorageDriver driver = new DiskStorageDriver(_TestDirectory);
                    byte[] data = Encoding.UTF8.GetBytes("Async test data");
                    byte[] md5 = await driver.WriteAsync("test3.txt", data).ConfigureAwait(false);

                    AssertNotNull(md5);

                    byte[] readBack = await driver.ReadAsync("test3.txt").ConfigureAwait(false);
                    AssertEqual("Async test data", Encoding.UTF8.GetString(readBack));
                });

                await RunTest("DiskStorageDriver_WriteAndReadAsync_Stream", async () =>
                {
                    DiskStorageDriver driver = new DiskStorageDriver(_TestDirectory);
                    byte[] data = Encoding.UTF8.GetBytes("Async stream test");

                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        byte[] md5 = await driver.WriteAsync("test4.txt", data.Length, ms).ConfigureAwait(false);
                        AssertNotNull(md5);
                    }

                    byte[] readBack = await driver.ReadAsync("test4.txt").ConfigureAwait(false);
                    AssertEqual("Async stream test", Encoding.UTF8.GetString(readBack));
                });

                await RunTest("DiskStorageDriver_Exists", () =>
                {
                    DiskStorageDriver driver = new DiskStorageDriver(_TestDirectory);
                    byte[] data = Encoding.UTF8.GetBytes("exists test");
                    driver.Write("exists.txt", data);

                    AssertTrue(driver.Exists("exists.txt"));
                    AssertFalse(driver.Exists("nonexistent.txt"));
                });

                await RunTest("DiskStorageDriver_Delete", () =>
                {
                    DiskStorageDriver driver = new DiskStorageDriver(_TestDirectory);
                    byte[] data = Encoding.UTF8.GetBytes("delete test");
                    driver.Write("todelete.txt", data);
                    AssertTrue(driver.Exists("todelete.txt"));

                    driver.Delete("todelete.txt");
                    AssertFalse(driver.Exists("todelete.txt"));
                });

                await RunTest("DiskStorageDriver_ReadStream", () =>
                {
                    DiskStorageDriver driver = new DiskStorageDriver(_TestDirectory);
                    byte[] data = Encoding.UTF8.GetBytes("stream read test");
                    driver.Write("streamread.txt", data);

                    ObjectStream os = driver.ReadStream("streamread.txt");
                    AssertNotNull(os);
                    AssertEqual("streamread.txt", os.Key);
                    AssertEqual(data.Length, (int)os.ContentLength);
                    AssertNotNull(os.Data);

                    using (StreamReader reader = new StreamReader(os.Data))
                    {
                        string content = reader.ReadToEnd();
                        AssertEqual("stream read test", content);
                    }
                });

                await RunTest("DiskStorageDriver_ReadRange", () =>
                {
                    DiskStorageDriver driver = new DiskStorageDriver(_TestDirectory);
                    byte[] data = Encoding.UTF8.GetBytes("ABCDEFGHIJ");
                    driver.Write("range.txt", data);

                    byte[] range = driver.ReadRange("range.txt", 0, 5);
                    AssertEqual(5, range.Length);
                    AssertEqual("ABCDE", Encoding.UTF8.GetString(range));
                });

                await RunTest("DiskStorageDriver_ReadRangeAsync", async () =>
                {
                    DiskStorageDriver driver = new DiskStorageDriver(_TestDirectory);
                    byte[] data = Encoding.UTF8.GetBytes("0123456789");
                    driver.Write("rangeasync.txt", data);

                    byte[] range = await driver.ReadRangeAsync("rangeasync.txt", 0, 3).ConfigureAwait(false);
                    AssertEqual(3, range.Length);
                    AssertEqual("012", Encoding.UTF8.GetString(range));
                });

                await RunTest("DiskStorageDriver_ReadRangeStream", () =>
                {
                    DiskStorageDriver driver = new DiskStorageDriver(_TestDirectory);
                    byte[] data = Encoding.UTF8.GetBytes("ABCDEFGHIJ");
                    driver.Write("rangestream.txt", data);

                    ObjectStream os = driver.ReadRangeStream("rangestream.txt", 0, 5);
                    AssertNotNull(os);
                    AssertEqual(5L, os.ContentLength);

                    using (StreamReader reader = new StreamReader(os.Data))
                    {
                        string content = reader.ReadToEnd();
                        AssertEqual("ABCDE", content);
                    }
                });

                await RunTest("DiskStorageDriver_StreamBufferSize", () =>
                {
                    DiskStorageDriver driver = new DiskStorageDriver(_TestDirectory);
                    AssertEqual(65536, driver.StreamBufferSize);

                    driver.StreamBufferSize = 4096;
                    AssertEqual(4096, driver.StreamBufferSize);

                    AssertThrows<ArgumentException>(() =>
                    {
                        driver.StreamBufferSize = 0;
                    });
                });

                await RunTest("DiskStorageDriver_LargeFile", async () =>
                {
                    DiskStorageDriver driver = new DiskStorageDriver(_TestDirectory);
                    byte[] data = new byte[1024 * 1024]; // 1MB
                    Random random = new Random(42);
                    random.NextBytes(data);

                    byte[] md5 = await driver.WriteAsync("largefile.bin", data).ConfigureAwait(false);
                    AssertNotNull(md5);

                    byte[] readBack = await driver.ReadAsync("largefile.bin").ConfigureAwait(false);
                    AssertEqual(data.Length, readBack.Length);

                    for (int i = 0; i < data.Length; i++)
                    {
                        if (data[i] != readBack[i])
                        {
                            Assert(false, $"Data mismatch at byte {i}");
                            break;
                        }
                    }
                });
            }
            finally
            {
                try
                {
                    if (Directory.Exists(_TestDirectory))
                        Directory.Delete(_TestDirectory, true);
                }
                catch
                {
                }
            }
        }

        #endregion
    }
}
