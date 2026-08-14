namespace Less3.Locking
{
    internal sealed class LocalLockEntry
    {
        internal string HolderId { get; }
        internal LockMode Mode { get; }
        internal long Seq { get; }
        internal bool Granted { get; set; }
        internal long FencingToken { get; set; }

        internal LocalLockEntry(string holderId, LockMode mode, long seq)
        {
            HolderId = holderId;
            Mode = mode;
            Seq = seq;
            Granted = false;
            FencingToken = 0;
        }
    }
}
