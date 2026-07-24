// Request History API types and interfaces

export interface RequestHistoryEntry {
  Id: string;
  TenantId?: string;
  HttpMethod: string;
  RequestUrl: string;
  SourceIp: string;
  StatusCode: number;
  Success: boolean;
  DurationMs: number;
  RequestType: string;
  UserId: string;
  CredentialId?: string;
  AccessKey: string;
  BucketId?: string;
  BucketName?: string;
  Bucket?: string;
  RequestContentType: string;
  RequestBodyLength: number;
  ResponseContentType: string;
  ResponseBodyLength: number;
  RequestBody: string | null;
  ResponseBody: string | null;
  CreatedUtc: string;
  [key: string]: any;
}

export type RequestHistoryListResponse = RequestHistoryEntry[];
export type RequestHistoryResponse = RequestHistoryEntry;

export interface RequestHistoryListParams {
  limit?: number;
  offset?: number;
  sortField?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface DeleteRequestHistoryParams {
  id: string;
}

export interface DeleteRequestHistoryResponse {
  success: boolean;
}

export interface RequestHistorySummaryBucket {
  TimestampUtc: string;
  SuccessCount: number;
  FailureCount: number;
}

export interface RequestHistorySummaryResult {
  Data: RequestHistorySummaryBucket[];
  StartUtc: string;
  EndUtc: string;
  Interval: string;
  TotalSuccess: number;
  TotalFailure: number;
}

export interface RequestHistorySummaryParams {
  startUtc: string;
  endUtc: string;
  interval: string;
}
