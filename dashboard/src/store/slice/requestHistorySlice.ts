import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import type {
  RequestHistoryEntry,
  RequestHistoryListResponse,
  RequestHistoryResponse,
  RequestHistoryListParams,
  DeleteRequestHistoryParams,
  DeleteRequestHistoryResponse,
  RequestHistorySummaryResult,
  RequestHistorySummaryParams,
} from './requestHistoryTypes';

export enum RequestHistorySliceTags {
  REQUEST_HISTORY = 'REQUEST_HISTORY',
}

// Re-export types for convenience
export type {
  RequestHistoryEntry,
  RequestHistoryListResponse,
  RequestHistoryResponse,
  RequestHistoryListParams,
  DeleteRequestHistoryParams,
  DeleteRequestHistoryResponse,
  RequestHistorySummaryResult,
  RequestHistorySummaryParams,
};

const enhancedSdk = sdkSlice.enhanceEndpoints({
  addTagTypes: [RequestHistorySliceTags.REQUEST_HISTORY],
});

const getRequestHistoryTags = (id: string) => [
  { type: RequestHistorySliceTags.REQUEST_HISTORY as const, id: id },
  { type: RequestHistorySliceTags.REQUEST_HISTORY, id: 'LIST' },
];

const buildRequestHistoryQueryString = (params?: RequestHistoryListParams): string => {
  const queryParams = new URLSearchParams();
  queryParams.set('limit', String(params?.limit ?? 1000));
  queryParams.set('offset', String(params?.offset ?? 0));
  queryParams.set('sortField', params?.sortField ?? 'createdUtc');
  queryParams.set('sortDirection', params?.sortDirection ?? 'desc');
  return queryParams.toString();
};

const requestHistorySliceInstance = enhancedSdk.injectEndpoints({
  overrideExisting: true,
  endpoints: (
    build: EndpointBuilder<BaseQueryFn<ApiBaseQueryArgs, unknown, unknown>, RequestHistorySliceTags, 'sdk'>
  ) => ({
    getRequestHistory: build.query<RequestHistoryListResponse, RequestHistoryListParams | void>({
      query: (params) => ({
        url: `admin/requesthistory?${buildRequestHistoryQueryString(params || undefined)}`,
        method: 'GET',
        cache: 'no-store',
      }),
      transformResponse: (response: any): RequestHistoryEntry[] => (Array.isArray(response) ? response : []),
      providesTags: (result: RequestHistoryEntry[] | undefined) =>
        result
          ? [
              ...result.map(({ Id }: RequestHistoryEntry) => ({
                type: RequestHistorySliceTags.REQUEST_HISTORY as const,
                id: Id,
              })),
              { type: RequestHistorySliceTags.REQUEST_HISTORY, id: 'LIST' },
            ]
          : [{ type: RequestHistorySliceTags.REQUEST_HISTORY, id: 'LIST' }],
    }),

    getRequestHistoryById: build.query<RequestHistoryResponse, string>({
      query: (id: string) => ({ url: `admin/requesthistory/${id}`, method: 'GET' }),
      transformResponse: (response: any): RequestHistoryEntry => response,
      providesTags: (_result: RequestHistoryEntry | undefined, _error: unknown, id: string) =>
        getRequestHistoryTags(id),
    }),

    getRequestHistorySummary: build.query<RequestHistorySummaryResult, RequestHistorySummaryParams>({
      query: ({ startUtc, endUtc, interval }: RequestHistorySummaryParams) => ({
        url: `admin/requesthistory/summary?startUtc=${encodeURIComponent(startUtc)}&endUtc=${encodeURIComponent(endUtc)}&interval=${encodeURIComponent(interval)}`,
        method: 'GET',
      }),
      transformResponse: (response: any): RequestHistorySummaryResult => response,
      providesTags: [{ type: RequestHistorySliceTags.REQUEST_HISTORY, id: 'SUMMARY' }],
      keepUnusedDataFor: 10,
    }),

    deleteRequestHistory: build.mutation<DeleteRequestHistoryResponse, DeleteRequestHistoryParams>({
      query: ({ id }: DeleteRequestHistoryParams) => ({
        url: `admin/requesthistory/${id}`,
        method: 'DELETE',
      }),
      transformResponse: (): DeleteRequestHistoryResponse => ({ success: true }),
      invalidatesTags: (
        _result: DeleteRequestHistoryResponse | undefined,
        _error: unknown,
        { id }: DeleteRequestHistoryParams
      ) => getRequestHistoryTags(id),
    }),
  }),
});

export const requestHistorySliceApi = requestHistorySliceInstance;

export const {
  useGetRequestHistoryQuery,
  useGetRequestHistoryByIdQuery,
  useGetRequestHistorySummaryQuery,
  useDeleteRequestHistoryMutation,
} = requestHistorySliceInstance;
