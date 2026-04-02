namespace Test.Xunit
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using global::Xunit;
    using Less3.Storage;

    /// <summary>
    /// Xunit tests for the DiskStorageDriver class.
    /// </summary>
    public class StorageXunitTests : IDisposable
    {
        private string _TestDirectory;
        private DiskStorageDriver _Driver;

        /// <summary>
        /// Initializes a new test instance with a temporary directory.
        /// </summary>
        public StorageXunitTests()
        {
            _TestDirectory = Path.Combine(Path.GetTempPath(), "less3-xunit-storage-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_TestDirectory);
            _Driver = new DiskStorageDriver(_TestDirectory);
        }

        /// <summary>
        /// Cleans up the temporary directory.
        /// </summary>
        public void Dispose()
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

        [Fact]
        public void WriteAndRead_ByteArray_RoundTrips()
        {
            byte[] data = Encoding.UTF8.GetBytes("Hello, Less3!");
            byte[] md5 = _Driver.Write("test1.txt", data);

            Assert.NotNull(md5);
            Assert.True(md5.Length > 0);

            byte[] readBack = _Driver.Read("test1.txt");
            Assert.Equal(data.Length, readBack.Length);
            Assert.Equal("Hello, Less3!", Encoding.UTF8.GetString(readBack));
        }

        [Fact]
        public void WriteAndRead_Stream_RoundTrips()
        {
            byte[] data = Encoding.UTF8.GetBytes("Stream test data");

            using (MemoryStream ms = new MemoryStream(data))
            {
                byte[] md5 = _Driver.Write("test2.txt", data.Length, ms);
                Assert.NotNull(md5);
            }

            byte[] readBack = _Driver.Read("test2.txt");
            Assert.Equal("Stream test data", Encoding.UTF8.GetString(readBack));
        }

        [Fact]
        public async Task WriteAndReadAsync_ByteArray_RoundTrips()
        {
            byte[] data = Encoding.UTF8.GetBytes("Async test data");
            byte[] md5 = await _Driver.WriteAsync("test3.txt", data);

            Assert.NotNull(md5);

            byte[] readBack = await _Driver.ReadAsync("test3.txt");
            Assert.Equal("Async test data", Encoding.UTF8.GetString(readBack));
        }

        [Fact]
        public void Exists_ReturnsTrueForExisting()
        {
            byte[] data = Encoding.UTF8.GetBytes("exists");
            _Driver.Write("exists.txt", data);

            Assert.True(_Driver.Exists("exists.txt"));
            Assert.False(_Driver.Exists("nonexistent.txt"));
        }

        [Fact]
        public void Delete_RemovesFile()
        {
            byte[] data = Encoding.UTF8.GetBytes("delete test");
            _Driver.Write("todelete.txt", data);
            Assert.True(_Driver.Exists("todelete.txt"));

            _Driver.Delete("todelete.txt");
            Assert.False(_Driver.Exists("todelete.txt"));
        }

        [Fact]
        public void ReadStream_ReturnsObjectStream()
        {
            byte[] data = Encoding.UTF8.GetBytes("stream read test");
            _Driver.Write("streamread.txt", data);

            ObjectStream os = _Driver.ReadStream("streamread.txt");
            Assert.NotNull(os);
            Assert.Equal("streamread.txt", os.Key);
            Assert.Equal(data.Length, (int)os.ContentLength);

            using (StreamReader reader = new StreamReader(os.Data))
            {
                string content = reader.ReadToEnd();
                Assert.Equal("stream read test", content);
            }
        }

        [Fact]
        public void ReadRange_ReturnsRequestedCount()
        {
            byte[] data = Encoding.UTF8.GetBytes("ABCDEFGHIJ");
            _Driver.Write("range.txt", data);

            byte[] range = _Driver.ReadRange("range.txt", 0, 5);
            Assert.Equal(5, range.Length);
            Assert.Equal("ABCDE", Encoding.UTF8.GetString(range));
        }

        [Fact]
        public async Task ReadRangeAsync_ReturnsRequestedCount()
        {
            byte[] data = Encoding.UTF8.GetBytes("0123456789");
            _Driver.Write("rangeasync.txt", data);

            byte[] range = await _Driver.ReadRangeAsync("rangeasync.txt", 0, 3);
            Assert.Equal(3, range.Length);
            Assert.Equal("012", Encoding.UTF8.GetString(range));
        }

        [Fact]
        public void ReadRangeStream_ReturnsRequestedCount()
        {
            byte[] data = Encoding.UTF8.GetBytes("ABCDEFGHIJ");
            _Driver.Write("rangestream.txt", data);

            ObjectStream os = _Driver.ReadRangeStream("rangestream.txt", 0, 5);
            Assert.NotNull(os);
            Assert.Equal(5L, os.ContentLength);

            using (StreamReader reader = new StreamReader(os.Data))
            {
                Assert.Equal("ABCDE", reader.ReadToEnd());
            }
        }

        [Fact]
        public void StreamBufferSize_ValidatesRange()
        {
            Assert.Equal(65536, _Driver.StreamBufferSize);

            _Driver.StreamBufferSize = 4096;
            Assert.Equal(4096, _Driver.StreamBufferSize);

            Assert.Throws<ArgumentException>(() => _Driver.StreamBufferSize = 0);
        }

        [Fact]
        public async Task LargeFile_RoundTrips()
        {
            byte[] data = new byte[1024 * 1024]; // 1MB
            Random random = new Random(42);
            random.NextBytes(data);

            byte[] md5 = await _Driver.WriteAsync("largefile.bin", data);
            Assert.NotNull(md5);

            byte[] readBack = await _Driver.ReadAsync("largefile.bin");
            Assert.Equal(data, readBack);
        }
    }
}
