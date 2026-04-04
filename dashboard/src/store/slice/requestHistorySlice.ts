import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import { buildApiUrl } from '#/services/sdk.service';
import type {
  RequestHistoryEntry,
  RequestHistoryListResponse,
  RequestHistoryResponse,
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
  DeleteRequestHistoryParams,
  DeleteRequestHistoryResponse,
  RequestHistorySummaryResult,
  RequestHistorySummaryParams,
};

const enhancedSdk = sdkSlice.enhanceEndpoints({
  addTagTypes: [RequestHistorySliceTags.REQUEST_HISTORY],
});

const getRequestHistoryTags = (guid: string) => [
  { type: RequestHistorySliceTags.REQUEST_HISTORY as const, id: guid },
  { type: RequestHistorySliceTags.REQUEST_HISTORY, id: 'LIST' },
];

const requestHistorySliceInstance = enhancedSdk.injectEndpoints({
  endpoints: (
    build: EndpointBuilder<BaseQueryFn<ApiBaseQueryArgs, unknown, unknown>, RequestHistorySliceTags, 'sdk'>
  ) => ({
    getRequestHistory: build.query<RequestHistoryListResponse, void>({
      query: () => ({
        url: buildApiUrl('admin/requesthistory'),
        method: 'GET',
      }),
      transformResponse: (response: any): RequestHistoryEntry[] => (Array.isArray(response) ? response : []),
      providesTags: (result: RequestHistoryEntry[] | undefined) =>
        result
          ? [
              ...result.map(({ GUID }: RequestHistoryEntry) => ({
                type: RequestHistorySliceTags.REQUEST_HISTORY as const,
                id: GUID,
              })),
              { type: RequestHistorySliceTags.REQUEST_HISTORY, id: 'LIST' },
            ]
          : [{ type: RequestHistorySliceTags.REQUEST_HISTORY, id: 'LIST' }],
    }),

    getRequestHistoryById: build.query<RequestHistoryResponse, string>({
      query: (guid: string) => ({ url: buildApiUrl(`admin/requesthistory/${guid}`), method: 'GET' }),
      transformResponse: (response: any): RequestHistoryEntry => response,
      providesTags: (_result: RequestHistoryEntry | undefined, _error: unknown, guid: string) =>
        getRequestHistoryTags(guid),
    }),

    getRequestHistorySummary: build.query<RequestHistorySummaryResult, RequestHistorySummaryParams>({
      query: ({ startUtc, endUtc, interval }: RequestHistorySummaryParams) => ({
        url: buildApiUrl(
          `admin/requesthistory/summary?startUtc=${encodeURIComponent(startUtc)}&endUtc=${encodeURIComponent(endUtc)}&interval=${encodeURIComponent(interval)}`
        ),
        method: 'GET',
      }),
      transformResponse: (response: any): RequestHistorySummaryResult => response,
      providesTags: [{ type: RequestHistorySliceTags.REQUEST_HISTORY, id: 'SUMMARY' }],
      keepUnusedDataFor: 10,
    }),

    deleteRequestHistory: build.mutation<DeleteRequestHistoryResponse, DeleteRequestHistoryParams>({
      query: ({ guid }: DeleteRequestHistoryParams) => ({
        url: buildApiUrl(`admin/requesthistory/${guid}`),
        method: 'DELETE',
      }),
      transformResponse: (): DeleteRequestHistoryResponse => ({ success: true }),
      invalidatesTags: (
        _result: DeleteRequestHistoryResponse | undefined,
        _error: unknown,
        { guid }: DeleteRequestHistoryParams
      ) => getRequestHistoryTags(guid),
    }),
  }),
});

export const {
  useGetRequestHistoryQuery,
  useGetRequestHistoryByIdQuery,
  useGetRequestHistorySummaryQuery,
  useDeleteRequestHistoryMutation,
} = requestHistorySliceInstance;
