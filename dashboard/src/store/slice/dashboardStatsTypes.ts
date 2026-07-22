export interface BucketStatisticSummary {
  Name: string;
  Id: string;
  Objects: number;
  Bytes: number;
}

export interface DashboardStatsResponse {
  BucketCount: number;
  TotalObjectCount: number;
  TotalBytes: number;
  GeneratedUtc: string;
  Buckets: BucketStatisticSummary[];
}

export interface AdminHealthStatusResponse {
  ServerVersion: string;
  UptimeSeconds: number;
  DatabaseType: string;
  DatabaseReachable: boolean;
  StoragePath: string;
  StoragePathWritable: boolean;
  FreeDiskBytes: number;
  TempPath: string;
  TempUploadCount: number;
  RequestHistoryRetentionDays: number;
  LastCleanupRunUtc: string | null;
  GeneratedUtc: string;
}
