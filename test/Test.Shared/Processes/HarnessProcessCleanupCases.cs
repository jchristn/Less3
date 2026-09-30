namespace Test.Shared.Processes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Shared.S3Compatibility;
    using Touchstone.Core;

    /// <summary>
    /// Test harness cleanup: Less3 test servers must not outlive the test process that started them.
    /// </summary>
    public static class HarnessProcessCleanupCases
    {
        #region Private-Members

        private const string _SuiteId = "HarnessProcessCleanup";
        private static readonly DateTime _LongAgoUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        #endregion

        #region Public-Methods

        /// <summary>
        /// The suite descriptor.
        /// </summary>
        /// <returns>Suite descriptor.</returns>
        public static TestSuiteDescriptor Suite()
        {
            return new TestSuiteDescriptor(
                suiteId: _SuiteId,
                displayName: "Test Harness Process Cleanup",
                cases: new List<TestCaseDescriptor>
                {
                    S3CompatibilitySuites.Case(_SuiteId, "OwnerRecordWritten", "Every test server records its owning test process and its own identity", OwnerRecordWrittenAsync),
                    S3CompatibilitySuites.Case(_SuiteId, "KillOnCloseJob", "On Windows every test server is in a kill-on-close job, so it dies with the test process", KillOnCloseJobAsync),
                    S3CompatibilitySuites.Case(_SuiteId, "SweepKillsOrphan", "A server whose owning test process is gone is killed and its directory removed", SweepKillsOrphanAsync),
                    S3CompatibilitySuites.Case(_SuiteId, "SweepSparesLiveOwner", "A server whose owner is still running is left alone", SweepSparesLiveOwnerAsync),
                    S3CompatibilitySuites.Case(_SuiteId, "SweepSparesReusedPid", "A process id reused by another process is never killed", SweepSparesReusedPidAsync),
                    S3CompatibilitySuites.Case(_SuiteId, "SweepSkipsUnrecorded", "Directories without an owner record or with another prefix are left alone", SweepSkipsUnrecordedAsync),
                    S3CompatibilitySuites.Case(_SuiteId, "IdentifyMissingProcess", "A process id that is not running is reported as not running", IdentifyMissingProcessAsync)
                });
        }

        #endregion

        #region Private-Methods

        private static async Task OwnerRecordWrittenAsync(CancellationToken token)
        {
            using Less3TestServer server = await StartServerAsync(token).ConfigureAwait(false);

            ServerOwnerRecord? record = ServerOwnerRecord.TryRead(server.TempDirectory);
            Check.True(record != null, "owner record written into the server directory");
            Check.Equal(Environment.ProcessId, record!.OwnerProcessId, "owner process id");
            Check.Equal(server.ProcessId!.Value, record.ServerProcessId, "server process id");
            Check.Equal(ProcessIdentityState.Running, ChildProcessTracker.Identify(record.OwnerProcessId, record.OwnerStartUtc), "owner identified as running");
            Check.Equal(ProcessIdentityState.Running, ChildProcessTracker.Identify(record.ServerProcessId, record.ServerStartUtc), "server identified as running");
        }

        private static async Task KillOnCloseJobAsync(CancellationToken token)
        {
            using Less3TestServer server = await StartServerAsync(token).ConfigureAwait(false);
            using Process process = Process.GetProcessById(server.ProcessId!.Value);

            if (OperatingSystem.IsWindows())
                Check.True(ChildProcessTracker.IsInKillOnCloseJob(process), "server is in the kill-on-close job");
            else
                Check.False(ChildProcessTracker.IsInKillOnCloseJob(process), "job objects are Windows-only; other platforms rely on the exit hook and sweep");
        }

        private static async Task SweepKillsOrphanAsync(CancellationToken token)
        {
            using Less3TestServer server = await StartServerAsync(token).ConfigureAwait(false);
            string root = NewRoot();

            try
            {
                string directory = RecordedDirectory(root, ChildProcessTracker.DirectoryPrefix + "orphan", DeadOwner(), ServerRecord(server));

                int killed = ChildProcessTracker.SweepOrphans(root);

                Check.Equal(1, killed, "servers killed");
                Check.Equal(ProcessIdentityState.NotRunning, ServerState(server), "orphaned server stopped");
                Check.False(Directory.Exists(directory), "orphaned server directory removed");
            }
            finally
            {
                DeleteRoot(root);
            }
        }

        private static async Task SweepSparesLiveOwnerAsync(CancellationToken token)
        {
            using Less3TestServer server = await StartServerAsync(token).ConfigureAwait(false);
            string root = NewRoot();

            try
            {
                ServerOwnerRecord owner = ServerOwnerRecord.TryRead(server.TempDirectory)!;
                string directory = RecordedDirectory(root, ChildProcessTracker.DirectoryPrefix + "live", owner, ServerRecord(server));

                int killed = ChildProcessTracker.SweepOrphans(root);

                Check.Equal(0, killed, "servers killed");
                Check.Equal(ProcessIdentityState.Running, ServerState(server), "server of a live owner still running");
                Check.True(Directory.Exists(directory), "directory of a live owner kept");
            }
            finally
            {
                DeleteRoot(root);
            }
        }

        private static async Task SweepSparesReusedPidAsync(CancellationToken token)
        {
            using Less3TestServer server = await StartServerAsync(token).ConfigureAwait(false);
            string root = NewRoot();

            try
            {
                ServerOwnerRecord reused = ServerRecord(server);
                reused.ServerStartUtc = _LongAgoUtc;
                string directory = RecordedDirectory(root, ChildProcessTracker.DirectoryPrefix + "reused", DeadOwner(), reused);

                int killed = ChildProcessTracker.SweepOrphans(root);

                Check.Equal(0, killed, "servers killed");
                Check.Equal(ProcessIdentityState.Running, ServerState(server), "process holding a reused id still running");
                Check.False(Directory.Exists(directory), "stale directory of a dead owner removed");
            }
            finally
            {
                DeleteRoot(root);
            }
        }

        private static async Task SweepSkipsUnrecordedAsync(CancellationToken token)
        {
            using Less3TestServer server = await StartServerAsync(token).ConfigureAwait(false);
            string root = NewRoot();

            try
            {
                string unrecorded = Path.Combine(root, ChildProcessTracker.DirectoryPrefix + "norecord");
                Directory.CreateDirectory(unrecorded);
                string otherPrefix = RecordedDirectory(root, "other-prefix-orphan", DeadOwner(), ServerRecord(server));

                int killed = ChildProcessTracker.SweepOrphans(root);

                Check.Equal(0, killed, "servers killed");
                Check.True(Directory.Exists(unrecorded), "directory without an owner record kept");
                Check.True(Directory.Exists(otherPrefix), "directory with another prefix kept");
                Check.Equal(ProcessIdentityState.Running, ServerState(server), "server named only under another prefix still running");
            }
            finally
            {
                DeleteRoot(root);
            }
        }

        private static Task IdentifyMissingProcessAsync(CancellationToken token)
        {
            Check.Equal(ProcessIdentityState.NotRunning, ChildProcessTracker.Identify(0, _LongAgoUtc), "process id 0");
            Check.Equal(ProcessIdentityState.NotRunning, ChildProcessTracker.Identify(-1, _LongAgoUtc), "negative process id");
            Check.Equal(ProcessIdentityState.NotRunning, ChildProcessTracker.Identify(Environment.ProcessId, _LongAgoUtc), "current process id with a different start time");
            return Task.CompletedTask;
        }

        private static async Task<Less3TestServer> StartServerAsync(CancellationToken token)
        {
            Less3TestServer server = new Less3TestServer();
            try
            {
                await server.StartAsync(token).ConfigureAwait(false);
                return server;
            }
            catch
            {
                server.Dispose();
                throw;
            }
        }

        private static ServerOwnerRecord DeadOwner()
        {
            ServerOwnerRecord record = new ServerOwnerRecord();
            record.OwnerProcessId = Environment.ProcessId;
            record.OwnerStartUtc = _LongAgoUtc;
            return record;
        }

        private static ServerOwnerRecord ServerRecord(Less3TestServer server)
        {
            using Process process = Process.GetProcessById(server.ProcessId!.Value);
            ServerOwnerRecord record = new ServerOwnerRecord();
            record.ServerProcessId = process.Id;
            record.ServerStartUtc = process.StartTime.ToUniversalTime();
            return record;
        }

        private static ProcessIdentityState ServerState(Less3TestServer server)
        {
            ServerOwnerRecord record = ServerOwnerRecord.TryRead(server.TempDirectory)!;
            return ChildProcessTracker.Identify(record.ServerProcessId, record.ServerStartUtc);
        }

        private static string RecordedDirectory(string root, string name, ServerOwnerRecord owner, ServerOwnerRecord server)
        {
            string directory = Path.Combine(root, name);
            Directory.CreateDirectory(directory);

            ServerOwnerRecord record = new ServerOwnerRecord();
            record.OwnerProcessId = owner.OwnerProcessId;
            record.OwnerStartUtc = owner.OwnerStartUtc;
            record.ServerProcessId = server.ServerProcessId;
            record.ServerStartUtc = server.ServerStartUtc;
            record.Write(directory);
            return directory;
        }

        private static string NewRoot()
        {
            string root = Path.Combine(Path.GetTempPath(), "less3-sweep-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return root;
        }

        private static void DeleteRoot(string root)
        {
            try
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
            catch (IOException)
            {
            }
        }

        #endregion
    }
}
