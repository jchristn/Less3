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

export interface RequestReportTopItem {
  Id?: string | null;
  Name: string;
  Count: number;
  Bytes?: number;
}

export interface RequestReportResponse {
  TenantId: string;
  StartUtc: string;
  EndUtc: string;
  RequestCount: number;
  SuccessCount: number;
  FailureCount: number;
  RequestsPerMinute: number;
  FailureRate: number;
  P50LatencyMs: number;
  P95LatencyMs: number;
  TopBucketsByBytes: RequestReportTopItem[];
  TopBucketsByRequestCount: RequestReportTopItem[];
  TopFailedRequestTypes: RequestReportTopItem[];
  TopAccessKeys: RequestReportTopItem[];
  GeneratedUtc: string;
}

export interface MaintenanceStatusResponse {
  RequestHistoryRetentionDays: number;
  CleanupIntervalMs: number;
  LastCleanupRunUtc: string | null;
  RuntimeEditableSettings: string[];
  RestartRequiredSettings: string[];
  Configuration: Record<string, any>;
  GeneratedUtc: string;
}

export interface MaintenanceSettingsRequest {
  Configuration?: Record<string, any>;
  RequestHistoryRetentionDays?: number;
  CleanupIntervalMs?: number;
  OlderThanUtc?: string;
}

export interface MaintenanceActionResult {
  Action: string;
  Success: boolean;
  DeletedRequestHistoryCount: number;
  DeletedTempFileCount: number;
  ObjectRowCount: number;
  MissingBlobFileCount: number;
  MissingBlobFiles: string[];
  RuntimeAppliedSettings: string[];
  RestartRequiredSettings: string[];
  GeneratedUtc: string;
}
