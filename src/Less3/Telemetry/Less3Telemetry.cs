namespace Less3.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;

    /// <summary>
    /// Base-class-library instruments for Less3. These meters carry no dependency on any telemetry
    /// SDK; they cost almost nothing until a host (Radiant) subscribes to them by name. Identifiers
    /// (bucket, key, upload) are deliberately kept off metric labels to bound cardinality — those
    /// belong on spans and logs. The fencing-conflict counter is the key data-integrity signal: it
    /// should stay at zero, and any increment means a lock holder was correctly rejected after its
    /// lease lapsed.
    /// Thread-safe.
    /// </summary>
    public static class Less3Telemetry
    {
        #region Meter-Names

        /// <summary>
        /// Meter name for lock manager instruments.
        /// </summary>
        public const string LocksMeterName = "Less3.Locks";

        /// <summary>
        /// Meter name for storage instruments.
        /// </summary>
        public const string StorageMeterName = "Less3.Storage";

        /// <summary>
        /// Meter name for multipart instruments.
        /// </summary>
        public const string MultipartMeterName = "Less3.Multipart";

        /// <summary>
        /// Meter name for bucket-cache instruments.
        /// </summary>
        public const string BucketsMeterName = "Less3.Buckets";

        /// <summary>
        /// Meter name for cleanup instruments.
        /// </summary>
        public const string CleanupMeterName = "Less3.Cleanup";

        /// <summary>
        /// Meter name for per-operation API instruments (every S3 and REST operation).
        /// </summary>
        public const string ApiMeterName = "Less3.Api";

        /// <summary>
        /// Meter name for object-operation stage-timing instruments.
        /// </summary>
        public const string ObjectMeterName = "Less3.Object";

        /// <summary>
        /// All Less3 meter names, for a host to subscribe to in one call.
        /// </summary>
        public static readonly string[] AllMeterNames = new string[]
        {
            LocksMeterName,
            StorageMeterName,
            MultipartMeterName,
            BucketsMeterName,
            CleanupMeterName,
            ApiMeterName,
            ObjectMeterName
        };

        /// <summary>
        /// Activity source name for Less3 spans.
        /// </summary>
        public const string ActivitySourceName = "Less3";

        #endregion

        #region Private-Members

        private static readonly Meter _Locks = new Meter(LocksMeterName);
        private static readonly Meter _Storage = new Meter(StorageMeterName);
        private static readonly Meter _Multipart = new Meter(MultipartMeterName);
        private static readonly Meter _Buckets = new Meter(BucketsMeterName);
        private static readonly Meter _Cleanup = new Meter(CleanupMeterName);

        private static readonly Counter<long> _LockAcquired = _Locks.CreateCounter<long>("less3.locks.acquired", "{acquire}", "Locks acquired.");
        private static readonly Counter<long> _LockDenied = _Locks.CreateCounter<long>("less3.locks.denied", "{denial}", "Lock acquisitions denied or timed out.");
        private static readonly Counter<long> _FencingConflicts = _Locks.CreateCounter<long>("less3.locks.fencing_conflicts", "{conflict}", "Mutations rejected because the holder's lease lapsed (data-integrity guard).");
        private static readonly Histogram<double> _LockAcquireMs = _Locks.CreateHistogram<double>("less3.locks.acquire.duration", "ms", "Time spent acquiring a lock.");

        private static readonly Counter<long> _BytesWritten = _Storage.CreateCounter<long>("less3.storage.bytes.written", "By", "Object bytes written to storage.");
        private static readonly Counter<long> _BlobWrites = _Storage.CreateCounter<long>("less3.storage.blob.writes", "{write}", "Blob write operations.");
        private static readonly Counter<long> _BlobDeletes = _Storage.CreateCounter<long>("less3.storage.blob.deletes", "{delete}", "Blob delete operations.");

        private static readonly Counter<long> _PartsUploaded = _Multipart.CreateCounter<long>("less3.multipart.parts.uploaded", "{part}", "Multipart parts uploaded.");
        private static readonly Counter<long> _Completes = _Multipart.CreateCounter<long>("less3.multipart.completes", "{complete}", "Multipart uploads completed.");
        private static readonly Counter<long> _Aborts = _Multipart.CreateCounter<long>("less3.multipart.aborts", "{abort}", "Multipart uploads aborted.");

        private static readonly Counter<long> _CacheHits = _Buckets.CreateCounter<long>("less3.buckets.cache.hits", "{hit}", "Bucket client cache hits.");
        private static readonly Counter<long> _CacheMisses = _Buckets.CreateCounter<long>("less3.buckets.cache.misses", "{miss}", "Bucket client cache misses or revalidations.");

        private static readonly Counter<long> _CleanupLeaderPasses = _Cleanup.CreateCounter<long>("less3.cleanup.leader_passes", "{pass}", "Cleanup passes run as the elected leader.");
        private static readonly Counter<long> _TempFilesDeleted = _Cleanup.CreateCounter<long>("less3.cleanup.temp_files.deleted", "{file}", "Orphan temporary files deleted.");

        private static readonly Meter _Api = new Meter(ApiMeterName);
        private static readonly Counter<long> _ApiRequests = _Api.CreateCounter<long>("less3.api.requests", "{request}", "API operations served, by surface, operation, and result. Covers every S3 and REST operation.");
        private static readonly Histogram<double> _ApiDuration = _Api.CreateHistogram<double>("less3.api.duration", "ms", "End-to-end duration of API operations, by surface and operation.");

        private static readonly Meter _ObjectMeter = new Meter(ObjectMeterName);
        private static readonly Histogram<double> _ObjectStageDuration = _ObjectMeter.CreateHistogram<double>("less3.object.stage.duration", "ms", "Time spent in each stage of an object operation (lock acquire, storage read/write, metadata commit, blob delete).");

        private static readonly ActivitySource _Activity = new ActivitySource(ActivitySourceName);

        #endregion

        #region Public-Methods

        /// <summary>
        /// Record a successful lock acquisition.
        /// </summary>
        /// <param name="provider">Lock provider name.</param>
        /// <param name="mode">Lock mode.</param>
        /// <param name="durationMs">Acquisition duration in milliseconds.</param>
        public static void LockAcquired(string provider, string mode, double durationMs)
        {
            _LockAcquired.Add(1, new KeyValuePair<string, object>("provider", provider), new KeyValuePair<string, object>("mode", mode));
            _LockAcquireMs.Record(durationMs, new KeyValuePair<string, object>("provider", provider), new KeyValuePair<string, object>("mode", mode));
        }

        /// <summary>
        /// Record a denied or timed-out lock acquisition.
        /// </summary>
        /// <param name="provider">Lock provider name.</param>
        /// <param name="mode">Lock mode.</param>
        /// <param name="reason">Denial reason.</param>
        public static void LockDenied(string provider, string mode, string reason)
        {
            _LockDenied.Add(1, new KeyValuePair<string, object>("provider", provider), new KeyValuePair<string, object>("mode", mode), new KeyValuePair<string, object>("reason", reason));
        }

        /// <summary>
        /// Record a fencing conflict: a mutation was rejected because the holder no longer owns the
        /// lock. This is the data-integrity guard firing and should be rare.
        /// </summary>
        /// <param name="operation">Operation that was rejected.</param>
        public static void FencingConflict(string operation)
        {
            _FencingConflicts.Add(1, new KeyValuePair<string, object>("operation", operation));
        }

        /// <summary>
        /// Record a blob write of the given size.
        /// </summary>
        /// <param name="bytes">Bytes written.</param>
        public static void BlobWritten(long bytes)
        {
            _BlobWrites.Add(1);
            if (bytes > 0) _BytesWritten.Add(bytes);
        }

        /// <summary>
        /// Record a blob delete.
        /// </summary>
        public static void BlobDeleted()
        {
            _BlobDeletes.Add(1);
        }

        /// <summary>
        /// Record a multipart part upload.
        /// </summary>
        public static void PartUploaded()
        {
            _PartsUploaded.Add(1);
        }

        /// <summary>
        /// Record a completed multipart upload.
        /// </summary>
        public static void MultipartCompleted()
        {
            _Completes.Add(1);
        }

        /// <summary>
        /// Record an aborted multipart upload.
        /// </summary>
        public static void MultipartAborted()
        {
            _Aborts.Add(1);
        }

        /// <summary>
        /// Record a bucket client cache hit.
        /// </summary>
        public static void CacheHit()
        {
            _CacheHits.Add(1);
        }

        /// <summary>
        /// Record a bucket client cache miss or revalidation.
        /// </summary>
        public static void CacheMiss()
        {
            _CacheMisses.Add(1);
        }

        /// <summary>
        /// Record a cleanup pass performed by the elected leader, and the number of temporary files
        /// it deleted.
        /// </summary>
        /// <param name="deletedFiles">Number of orphan files deleted.</param>
        public static void CleanupLeaderPass(int deletedFiles)
        {
            _CleanupLeaderPasses.Add(1);
            if (deletedFiles > 0) _TempFilesDeleted.Add(deletedFiles);
        }

        /// <summary>
        /// Record one completed API operation (every S3 and REST operation flows through here),
        /// with its duration and result. Identifiers stay off the labels to keep cardinality bounded.
        /// </summary>
        /// <param name="surface">API surface: "s3", "rest", or "admin".</param>
        /// <param name="operation">Operation name (S3 request type, or REST method+resource).</param>
        /// <param name="statusCode">HTTP status code the operation returned.</param>
        /// <param name="durationMs">End-to-end duration in milliseconds.</param>
        public static void ApiOperation(string surface, string operation, int statusCode, double durationMs)
        {
            string result = statusCode < 400 ? "ok" : (statusCode < 500 ? "client_error" : "server_error");
            KeyValuePair<string, object>[] tags = new KeyValuePair<string, object>[]
            {
                new KeyValuePair<string, object>("surface", surface),
                new KeyValuePair<string, object>("operation", operation)
            };

            _ApiDuration.Record(durationMs, tags);
            _ApiRequests.Add(1,
                new KeyValuePair<string, object>("surface", surface),
                new KeyValuePair<string, object>("operation", operation),
                new KeyValuePair<string, object>("result", result));
        }

        /// <summary>
        /// Start a trace span for an object operation so its stages can be timestamped end to end.
        /// Returns null when no trace listener is attached (the aggregate stage histograms still
        /// record regardless). Dispose the returned activity when the operation finishes.
        /// </summary>
        /// <param name="operation">Operation name, for example "PutObject".</param>
        /// <returns>An activity, or null.</returns>
        public static Activity StartObjectOperation(string operation)
        {
            return _Activity.StartActivity("object." + operation, ActivityKind.Internal);
        }

        /// <summary>
        /// Record the duration of one stage of an object operation, and stamp a timestamped event on
        /// the operation's span so the full execution timeline is captured on the trace.
        /// </summary>
        /// <param name="activity">The object operation's activity, or null.</param>
        /// <param name="operation">Operation name, for example "PutObject".</param>
        /// <param name="stage">Stage name, for example "lock_acquire", "storage_write", "db_commit".</param>
        /// <param name="durationMs">Stage duration in milliseconds.</param>
        public static void ObjectStage(Activity activity, string operation, string stage, double durationMs)
        {
            _ObjectStageDuration.Record(durationMs,
                new KeyValuePair<string, object>("operation", operation),
                new KeyValuePair<string, object>("stage", stage));

            if (activity != null)
            {
                ActivityTagsCollection tags = new ActivityTagsCollection
                {
                    { "stage", stage },
                    { "duration_ms", durationMs }
                };
                activity.AddEvent(new ActivityEvent(stage, DateTimeOffset.UtcNow, tags));
            }
        }

        #endregion
    }
}
