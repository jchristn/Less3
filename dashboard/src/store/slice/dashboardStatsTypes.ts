export interface BucketStatisticSummary {
  Name: string;
  GUID: string;
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
