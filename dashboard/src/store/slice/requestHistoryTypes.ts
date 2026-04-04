// Request History API types and interfaces

export interface RequestHistoryEntry {
  GUID: string;
  HttpMethod: string;
  RequestUrl: string;
  SourceIp: string;
  StatusCode: number;
  Success: boolean;
  DurationMs: number;
  RequestType: string;
  UserGUID: string;
  AccessKey: string;
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

export interface DeleteRequestHistoryParams {
  guid: string;
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
