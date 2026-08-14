import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import { adminRequest, toRtkQueryError } from '#/services/backendApi.service';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import type {
  ClusterHealthResponse,
  ClusterLeaderResponse,
  ClusterNodeInfo,
  ClusterNodeListResponse,
  GetLockByKeyParams,
  LockInfo,
  LockListResponse,
} from './clusterTypes';

export enum ClusterSliceTags {
  CLUSTER = 'CLUSTER',
  LOCKS = 'LOCKS',
}

// Re-export types for convenience
export type {
  ClusterHealthResponse,
  ClusterLeaderResponse,
  ClusterNodeInfo,
  ClusterNodeListResponse,
  GetLockByKeyParams,
  LockInfo,
  LockListResponse,
};

const enhancedSdk = sdkSlice.enhanceEndpoints({
  addTagTypes: [ClusterSliceTags.CLUSTER, ClusterSliceTags.LOCKS],
});

const clusterQuery = async <T,>(path: string, fallbackMessage: string) => {
  try {
    const data = await adminRequest<T>(path, { method: 'GET', cache: 'no-store' });
    return { data };
  } catch (error) {
    return {
      error: toRtkQueryError(error, fallbackMessage),
    };
  }
};

const clusterSliceInstance = enhancedSdk.injectEndpoints({
  overrideExisting: true,
  endpoints: (
    build: EndpointBuilder<BaseQueryFn<ApiBaseQueryArgs, unknown, unknown>, ClusterSliceTags, 'sdk'>
  ) => ({
    getClusterHealth: build.query<ClusterHealthResponse, void>({
      async queryFn() {
        return clusterQuery<ClusterHealthResponse>('api/v1/cluster/health', 'Failed to fetch cluster health');
      },
      providesTags: [{ type: ClusterSliceTags.CLUSTER, id: 'HEALTH' }],
    }),

    getClusterNodes: build.query<ClusterNodeListResponse, void>({
      async queryFn() {
        try {
          const data = await adminRequest<unknown>('api/v1/cluster/nodes', { method: 'GET', cache: 'no-store' });
          return { data: Array.isArray(data) ? (data as ClusterNodeInfo[]) : [] };
        } catch (error) {
          return { error: toRtkQueryError(error, 'Failed to fetch cluster nodes') };
        }
      },
      providesTags: [{ type: ClusterSliceTags.CLUSTER, id: 'NODES' }],
    }),

    getClusterLeader: build.query<ClusterLeaderResponse, void>({
      async queryFn() {
        return clusterQuery<ClusterLeaderResponse>('api/v1/cluster/leader', 'Failed to fetch cluster leader');
      },
      providesTags: [{ type: ClusterSliceTags.CLUSTER, id: 'LEADER' }],
    }),

    getLocks: build.query<LockListResponse, void>({
      async queryFn() {
        try {
          const data = await adminRequest<unknown>('api/v1/locks', { method: 'GET', cache: 'no-store' });
          return { data: Array.isArray(data) ? (data as LockInfo[]) : [] };
        } catch (error) {
          return { error: toRtkQueryError(error, 'Failed to fetch locks') };
        }
      },
      providesTags: [{ type: ClusterSliceTags.LOCKS, id: 'LIST' }],
    }),

    getLockByKey: build.query<LockInfo, GetLockByKeyParams>({
      async queryFn({ key }: GetLockByKeyParams) {
        return clusterQuery<LockInfo>(`api/v1/locks/${encodeURIComponent(key)}`, 'Failed to fetch lock');
      },
      providesTags: (_result: LockInfo | undefined, _error: unknown, { key }: GetLockByKeyParams) => [
        { type: ClusterSliceTags.LOCKS as const, id: key },
      ],
    }),
  }),
});

export const clusterSliceApi = clusterSliceInstance;

export const {
  useGetClusterHealthQuery,
  useGetClusterNodesQuery,
  useGetClusterLeaderQuery,
  useGetLocksQuery,
  useGetLockByKeyQuery,
} = clusterSliceInstance;
