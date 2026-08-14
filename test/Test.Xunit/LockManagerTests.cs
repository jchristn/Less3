#pragma warning disable CS1591 // Missing XML comment for publicly visible test member
#pragma warning disable CS8600 // Converting null literal in test scaffolding

namespace Test.Xunit
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Less3.Locking;
    using Less3.Settings;
    using SyslogLogging;
    using global::Xunit;

    /// <summary>
    /// Unit tests for the in-process lock manager. These exercise the mutual-exclusion, fencing,
    /// non-blocking-read, wait/timeout, and validation guarantees that the Postgres provider must
    /// also uphold, without requiring a database.
    /// </summary>
    public class LockManagerTests
    {
        private static LocalLockManager NewManager()
        {
            LoggingModule logging = new LoggingModule("127.0.0.1", 514, false);
            return new LocalLockManager(new LockSettings(), logging);
        }

        [Fact]
        public async Task WriteLockIsExclusiveAndReleasable()
        {
            using LocalLockManager mgr = NewManager();

            LockHandle first = await mgr.AcquireAsync("obj:t:b:k", LockMode.Write);
            Assert.NotNull(first);

            await Assert.ThrowsAsync<LockDeniedException>(async () =>
                await mgr.AcquireAsync("obj:t:b:k", LockMode.Write));

            Assert.True(await mgr.ReleaseAsync(first));

            LockHandle second = await mgr.AcquireAsync("obj:t:b:k", LockMode.Write);
            Assert.NotNull(second);
            await mgr.ReleaseAsync(second);
        }

        [Fact]
        public async Task FencingTokenIsMonotonicPerKey()
        {
            using LocalLockManager mgr = NewManager();

            LockHandle a = await mgr.AcquireAsync("obj:t:b:k", LockMode.Write);
            long first = a.FencingToken;
            await mgr.ReleaseAsync(a);

            LockHandle b = await mgr.AcquireAsync("obj:t:b:k", LockMode.Write);
            long second = b.FencingToken;
            await mgr.ReleaseAsync(b);

            Assert.True(second > first, "fencing token must strictly increase across acquisitions");
        }

        [Fact]
        public async Task ReadLocksDoNotContend()
        {
            using LocalLockManager mgr = NewManager();

            LockHandle r1 = await mgr.AcquireAsync("obj:t:b:k", LockMode.Read);
            LockHandle r2 = await mgr.AcquireAsync("obj:t:b:k", LockMode.Read);

            Assert.NotNull(r1);
            Assert.NotNull(r2);

            await mgr.ReleaseAsync(r1);
            await mgr.ReleaseAsync(r2);
        }

        [Fact]
        public async Task ValidateReflectsOwnership()
        {
            using LocalLockManager mgr = NewManager();

            LockHandle h = await mgr.AcquireAsync("obj:t:b:k", LockMode.Write);
            Assert.True(await mgr.ValidateAsync(h));

            await mgr.ReleaseAsync(h);
            Assert.False(await mgr.ValidateAsync(h));
        }

        [Fact]
        public async Task WaitBehaviorTimesOutWhenHeld()
        {
            using LocalLockManager mgr = NewManager();

            LockHandle held = await mgr.AcquireAsync("obj:t:b:k", LockMode.Write);

            LockDeniedException ex = await Assert.ThrowsAsync<LockDeniedException>(async () =>
                await mgr.AcquireAsync("obj:t:b:k", LockMode.Write, new AcquireOptions(200)));

            Assert.Equal(AcquireResult.Timeout, ex.Result);
            await mgr.ReleaseAsync(held);
        }

        [Fact]
        public async Task ReadsAreSharedAndUnbounded()
        {
            using LocalLockManager mgr = NewManager();

            List<LockHandle> reads = new List<LockHandle>();
            for (int i = 0; i < 64; i++)
            {
                reads.Add(await mgr.AcquireAsync("obj:t:b:k", LockMode.Read));
            }

            Assert.Equal(64, reads.Count);
            foreach (LockHandle h in reads) Assert.NotNull(h);

            foreach (LockHandle h in reads) await mgr.ReleaseAsync(h);
        }

        [Fact]
        public async Task WriteWaitsForPendingReadsToFlush()
        {
            using LocalLockManager mgr = NewManager();

            LockHandle r1 = await mgr.AcquireAsync("obj:t:b:k", LockMode.Read);
            LockHandle r2 = await mgr.AcquireAsync("obj:t:b:k", LockMode.Read);

            Task<LockHandle> writeTask = Task.Run(() => mgr.AcquireAsync("obj:t:b:k", LockMode.Write, new AcquireOptions(5000)));

            await Task.Delay(500);
            Assert.False(writeTask.IsCompleted, "write must wait while reads are held");

            await mgr.ReleaseAsync(r1);
            await mgr.ReleaseAsync(r2);

            LockHandle wh = await writeTask;
            Assert.NotNull(wh);
            await mgr.ReleaseAsync(wh);
        }

        [Fact]
        public async Task LateReadDoesNotOvertakeQueuedExclusive()
        {
            using LocalLockManager mgr = NewManager();

            LockHandle r1 = await mgr.AcquireAsync("obj:t:b:k", LockMode.Read);

            // A delete arrives and must drain everything ahead of it.
            Task<LockHandle> deleteTask = Task.Run(() => mgr.AcquireAsync("obj:t:b:k", LockMode.Delete, new AcquireOptions(5000)));
            await Task.Delay(400);

            // A read that arrives after the queued delete must wait behind it (FIFO fairness),
            // otherwise a steady read stream would starve the delete.
            Task<LockHandle> lateReadTask = Task.Run(() => mgr.AcquireAsync("obj:t:b:k", LockMode.Read, new AcquireOptions(5000)));
            await Task.Delay(400);

            Assert.False(deleteTask.IsCompleted, "delete must wait for the earlier read to flush");
            Assert.False(lateReadTask.IsCompleted, "late read must queue behind the pending delete");

            await mgr.ReleaseAsync(r1);

            LockHandle dh = await deleteTask;
            Assert.NotNull(dh);
            Assert.False(lateReadTask.IsCompleted, "late read must not be granted until the delete releases");
            await mgr.ReleaseAsync(dh);

            LockHandle lrh = await lateReadTask;
            Assert.NotNull(lrh);
            await mgr.ReleaseAsync(lrh);
        }

        [Fact]
        public async Task ConcurrentWritersNeverOverlap()
        {
            using LocalLockManager mgr = NewManager();

            int concurrent = 0;
            int maxConcurrent = 0;
            int successful = 0;
            object gate = new object();

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 32; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    for (int j = 0; j < 20; j++)
                    {
                        LockHandle h = null;
                        try
                        {
                            h = await mgr.AcquireAsync("obj:t:b:hot", LockMode.Write, new AcquireOptions(2000));
                        }
                        catch (LockDeniedException)
                        {
                            continue;
                        }

                        try
                        {
                            lock (gate)
                            {
                                concurrent++;
                                successful++;
                                if (concurrent > maxConcurrent) maxConcurrent = concurrent;
                            }

                            // Hold briefly to widen the window for any overlap to appear.
                            await Task.Delay(1);

                            lock (gate) { concurrent--; }
                        }
                        finally
                        {
                            await mgr.ReleaseAsync(h);
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            Assert.Equal(1, maxConcurrent);
            Assert.True(successful > 0);
        }
    }
}
