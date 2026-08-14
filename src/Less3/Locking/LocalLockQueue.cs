namespace Less3.Locking
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// In-process fair FIFO read/write/delete queue for a single lock key. Reads are shared and
    /// unbounded; writes and deletes are exclusive and are granted only once every earlier-arrived
    /// holder has released. The per-key fencing counter persists across acquisitions so tokens are
    /// strictly increasing for the life of the process.
    /// Thread-safe.
    /// </summary>
    internal sealed class LocalLockQueue
    {
        private readonly object _Lock = new object();
        private readonly List<LocalLockEntry> _Entries = new List<LocalLockEntry>();
        private readonly string _Key;
        private readonly string _Provider;
        private long _Fence = 0;

        internal LocalLockQueue(string key, string provider)
        {
            _Key = key;
            _Provider = provider;
        }

        internal long Enqueue(string holderId, LockMode mode, long seq)
        {
            lock (_Lock)
            {
                _Entries.Add(new LocalLockEntry(holderId, mode, seq));
                return seq;
            }
        }

        internal LockHandle TryGrant(string holderId, DateTime leaseExpiresUtc)
        {
            lock (_Lock)
            {
                LocalLockEntry me = _Entries.FirstOrDefault(e => e.HolderId == holderId);
                if (me == null) return null;

                if (!me.Granted)
                {
                    bool earlierExclusive = _Entries.Any(e => e.Seq < me.Seq && (e.Mode == LockMode.Write || e.Mode == LockMode.Delete));
                    long minSeq = _Entries.Min(e => e.Seq);
                    int grantedCount = _Entries.Count(e => e.Granted);

                    bool grantable;
                    if (me.Mode == LockMode.Read)
                    {
                        // A shared read may proceed once no earlier-arrived exclusive is ahead of it.
                        grantable = !earlierExclusive;
                    }
                    else
                    {
                        // An exclusive (write/delete) may proceed only when it is the oldest waiter
                        // and nothing is currently held — i.e. all earlier reads have flushed.
                        grantable = me.Seq == minSeq && grantedCount == 0;
                    }

                    if (!grantable) return null;

                    me.Granted = true;
                    me.FencingToken = ++_Fence;
                }

                return new LockHandle(_Key, me.Mode, holderId, me.FencingToken, leaseExpiresUtc, _Provider);
            }
        }

        internal bool Release(string holderId)
        {
            lock (_Lock)
            {
                int removed = _Entries.RemoveAll(e => e.HolderId == holderId);
                return removed > 0;
            }
        }

        internal bool IsGranted(string holderId)
        {
            lock (_Lock)
            {
                LocalLockEntry me = _Entries.FirstOrDefault(e => e.HolderId == holderId);
                return me != null && me.Granted;
            }
        }
    }
}
