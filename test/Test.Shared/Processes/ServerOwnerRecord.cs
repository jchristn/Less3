namespace Test.Shared.Processes
{
    using System;
    using System.IO;
    using System.Text.Json;

    /// <summary>
    /// Records which test process started a Less3 test server. Written into the server's working directory so a
    /// later run can find servers whose owner died without stopping them.
    /// </summary>
    public class ServerOwnerRecord
    {
        #region Public-Members

        /// <summary>
        /// The name of the record file inside a server's working directory.
        /// </summary>
        public static string FileName { get; } = "less3-test-owner.json";

        /// <summary>
        /// Process id of the test process that started the server.
        /// </summary>
        public int OwnerProcessId { get; set; } = 0;

        /// <summary>
        /// Start time (UTC) of the owning test process, used to tell it apart from a later process with the same id.
        /// </summary>
        public DateTime OwnerStartUtc { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Process id of the Less3 server.
        /// </summary>
        public int ServerProcessId { get; set; } = 0;

        /// <summary>
        /// Start time (UTC) of the Less3 server process.
        /// </summary>
        public DateTime ServerStartUtc { get; set; } = DateTime.MinValue;

        #endregion

        #region Public-Methods

        /// <summary>
        /// Write the record into a directory, replacing any existing record.
        /// </summary>
        /// <param name="directory">Server working directory.</param>
        /// <exception cref="ArgumentNullException">Thrown when directory is null or empty.</exception>
        public void Write(string directory)
        {
            if (String.IsNullOrEmpty(directory)) throw new ArgumentNullException(nameof(directory));
            File.WriteAllText(Path.Combine(directory, FileName), JsonSerializer.Serialize(this));
        }

        /// <summary>
        /// Read the record from a directory.
        /// </summary>
        /// <param name="directory">Server working directory.</param>
        /// <returns>The record, or null when the directory has no readable record.</returns>
        public static ServerOwnerRecord? TryRead(string directory)
        {
            if (String.IsNullOrEmpty(directory)) return null;

            string path = Path.Combine(directory, FileName);
            if (!File.Exists(path)) return null;

            try
            {
                return JsonSerializer.Deserialize<ServerOwnerRecord>(File.ReadAllText(path));
            }
            catch (IOException)
            {
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        #endregion
    }
}
