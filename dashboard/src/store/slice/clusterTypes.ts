// Cluster and distributed lock API types and interfaces.
// All backend JSON is PascalCase and served from the versioned /api/v1 surface.

export interface ClusterNodeInfo {
  NodeId: string;
  Hostname: string;
  Version: string;
  StartedUtc: string;
  LastSeenUtc: string;
  Healthy: boolean;
  IsSelf: boolean;
}

export interface ClusterHealthResponse {
  ClusterEnabled: boolean;
  LockProvider: string;
  SelfNodeId: string;
  TotalNodes: number;
  HealthyNodes: number;
  Nodes: ClusterNodeInfo[];
  GeneratedUtc: string;
}

export interface ClusterLeaderResponse {
  LeaderNodeId: string | null;
}

export interface LockInfo {
  LockKey: string;
  Mode: string;
  HolderId: string;
  FencingToken: number;
  NodeId: string;
  AcquiredUtc: string;
  LeaseExpiresUtc: string;
}

export type ClusterNodeListResponse = ClusterNodeInfo[];
export type LockListResponse = LockInfo[];

export interface GetLockByKeyParams {
  key: string;
}
