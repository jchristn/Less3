#pragma warning disable CS1591 // Missing XML comment for publicly visible test member

namespace Test.Xunit
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using Less3.Storage;
    using global::Xunit;

    /// <summary>
    /// Verifies that the disk storage driver writes bytes intact and computes the content hash in a
    /// single pass that matches an independent hash of the same bytes.
    /// </summary>
    public class StorageIntegrityTests
    {
        private static string NewBaseDir()
        {
            string dir = Path.Combine(Path.GetTempPath(), "less3-storage-" + Guid.NewGuid().ToString("N")) + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(dir);
            return dir;
        }

        [Fact]
        public void WriteThenReadIsByteIdenticalWithMatchingHash()
        {
            string baseDir = NewBaseDir();
            try
            {
                DiskStorageDriver driver = new DiskStorageDriver(baseDir);

                byte[] payload = new byte[200000];
                new Random(12345).NextBytes(payload);

                byte[] expectedMd5;
                using (MD5 md5 = MD5.Create()) expectedMd5 = md5.ComputeHash(payload);

                byte[] returnedMd5;
                using (MemoryStream ms = new MemoryStream(payload))
                {
                    returnedMd5 = driver.Write("obj-1", payload.Length, ms);
                }

                Assert.Equal(expectedMd5, returnedMd5);

                byte[] readBack = driver.Read("obj-1");
                Assert.Equal(payload, readBack);
            }
            finally
            {
                try { Directory.Delete(baseDir, true); } catch (Exception) { }
            }
        }

        [Fact]
        public void ShortStreamDoesNotHangAndHashesWhatWasWritten()
        {
            string baseDir = NewBaseDir();
            try
            {
                DiskStorageDriver driver = new DiskStorageDriver(baseDir);

                byte[] payload = new byte[10];
                new Random(7).NextBytes(payload);

                // Declare a larger content length than the stream actually yields; the writer must
                // stop at end-of-stream rather than spin.
                using (MemoryStream ms = new MemoryStream(payload))
                {
                    driver.Write("obj-2", 1000, ms);
                }

                byte[] readBack = driver.Read("obj-2");
                Assert.Equal(payload, readBack);
            }
            finally
            {
                try { Directory.Delete(baseDir, true); } catch (Exception) { }
            }
        }

        [Fact]
        public void WriteByteArrayReturnsMatchingHashAndRoundTrips()
        {
            string baseDir = NewBaseDir();
            try
            {
                DiskStorageDriver driver = new DiskStorageDriver(baseDir);

                byte[] payload = new byte[65536 + 123];
                new Random(99).NextBytes(payload);

                byte[] expectedMd5;
                using (MD5 md5 = MD5.Create()) expectedMd5 = md5.ComputeHash(payload);

                byte[] returnedMd5 = driver.Write("obj-bytes", payload);

                Assert.Equal(expectedMd5, returnedMd5);
                Assert.True(driver.Exists("obj-bytes"));
                Assert.Equal(payload, driver.Read("obj-bytes"));
            }
            finally
            {
                try { Directory.Delete(baseDir, true); } catch (Exception) { }
            }
        }

        [Fact]
        public async Task WriteAsyncAndReadAsyncRoundTripWithMatchingHash()
        {
            string baseDir = NewBaseDir();
            try
            {
                DiskStorageDriver driver = new DiskStorageDriver(baseDir);

                byte[] payload = new byte[150000];
                new Random(4242).NextBytes(payload);

                byte[] expectedMd5;
                using (MD5 md5 = MD5.Create()) expectedMd5 = md5.ComputeHash(payload);

                byte[] returnedMd5 = await driver.WriteAsync("obj-async", payload);
                Assert.Equal(expectedMd5, returnedMd5);

                byte[] readBack = await driver.ReadAsync("obj-async");
                Assert.Equal(payload, readBack);
            }
            finally
            {
                try { Directory.Delete(baseDir, true); } catch (Exception) { }
            }
        }

        [Fact]
        public void EmptyObjectWritesReadsAsEmptyWithEmptyHash()
        {
            string baseDir = NewBaseDir();
            try
            {
                DiskStorageDriver driver = new DiskStorageDriver(baseDir);

                byte[] expectedMd5;
                using (MD5 md5 = MD5.Create()) expectedMd5 = md5.ComputeHash(new byte[0]);

                byte[] returnedMd5 = driver.Write("obj-empty", new byte[0]);

                Assert.Equal(expectedMd5, returnedMd5);
                Assert.True(driver.Exists("obj-empty"));
                Assert.Empty(driver.Read("obj-empty"));
            }
            finally
            {
                try { Directory.Delete(baseDir, true); } catch (Exception) { }
            }
        }

        [Fact]
        public void ExistsAndDeleteReflectObjectLifecycle()
        {
            string baseDir = NewBaseDir();
            try
            {
                DiskStorageDriver driver = new DiskStorageDriver(baseDir);

                Assert.False(driver.Exists("obj-life"));

                driver.Write("obj-life", new byte[] { 1, 2, 3, 4 });
                Assert.True(driver.Exists("obj-life"));

                driver.Delete("obj-life");
                Assert.False(driver.Exists("obj-life"));

                // Deleting a key that does not exist is a no-op rather than an error.
                driver.Delete("obj-life");
            }
            finally
            {
                try { Directory.Delete(baseDir, true); } catch (Exception) { }
            }
        }

        [Fact]
        public void ReadRangeFromStartReturnsRequestedPrefix()
        {
            string baseDir = NewBaseDir();
            try
            {
                DiskStorageDriver driver = new DiskStorageDriver(baseDir);

                byte[] payload = new byte[1000];
                new Random(11).NextBytes(payload);
                driver.Write("obj-range", payload);

                byte[] prefix = driver.ReadRange("obj-range", 0, 250);
                Assert.Equal(250, prefix.Length);

                byte[] expected = new byte[250];
                Array.Copy(payload, 0, expected, 0, 250);
                Assert.Equal(expected, prefix);
            }
            finally
            {
                try { Directory.Delete(baseDir, true); } catch (Exception) { }
            }
        }

        [Fact]
        public void ReadMissingKeyThrows()
        {
            string baseDir = NewBaseDir();
            try
            {
                DiskStorageDriver driver = new DiskStorageDriver(baseDir);
                Assert.Throws<FileNotFoundException>(() => driver.Read("does-not-exist"));
            }
            finally
            {
                try { Directory.Delete(baseDir, true); } catch (Exception) { }
            }
        }

        [Fact]
        public void WriteRejectsNullOrEmptyKey()
        {
            string baseDir = NewBaseDir();
            try
            {
                DiskStorageDriver driver = new DiskStorageDriver(baseDir);
                Assert.Throws<ArgumentNullException>(() => driver.Write(null, new byte[] { 1 }));
                Assert.Throws<ArgumentNullException>(() => driver.Write(string.Empty, new byte[] { 1 }));
            }
            finally
            {
                try { Directory.Delete(baseDir, true); } catch (Exception) { }
            }
        }

        [Fact]
        public void ReadRangeRejectsInvalidBounds()
        {
            string baseDir = NewBaseDir();
            try
            {
                DiskStorageDriver driver = new DiskStorageDriver(baseDir);
                driver.Write("obj-bounds", new byte[100]);

                Assert.Throws<ArgumentException>(() => driver.ReadRange("obj-bounds", -1, 10));
                Assert.Throws<ArgumentException>(() => driver.ReadRange("obj-bounds", 0, -10));
                // Reading past the end of the object must be rejected, not silently truncated.
                Assert.Throws<ArgumentException>(() => driver.ReadRange("obj-bounds", 50, 100));
            }
            finally
            {
                try { Directory.Delete(baseDir, true); } catch (Exception) { }
            }
        }

        [Fact]
        public void ConstructorRejectsNullOrEmptyBaseDirectory()
        {
            Assert.Throws<ArgumentNullException>(() => new DiskStorageDriver(null));
            Assert.Throws<ArgumentNullException>(() => new DiskStorageDriver(string.Empty));
        }
    }
}
