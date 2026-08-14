#pragma warning disable CS1591 // Missing XML comment for publicly visible test member

namespace Test.Xunit
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
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
    }
}
