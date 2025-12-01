import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import { buildApiUrl } from '#/services/sdk.service';
import type {
  Bucket,
  BucketListResponse,
  BucketResponse,
  CreateBucketRequest,
  UpdateBucketRequest,
  DeleteBucketParams,
  DeleteBucketResponse,
  GetBucketsParams,
} from './bucketsTypes';

export enum BucketsSliceTags {
  BUCKETS = 'BUCKETS',
}

// Re-export types for convenience
export type {
  Bucket,
  BucketListResponse,
  BucketResponse,
  CreateBucketRequest,
  UpdateBucketRequest,
  DeleteBucketParams,
  DeleteBucketResponse,
  GetBucketsParams,
};

const enhancedSdk = sdkSlice.enhanceEndpoints({
  addTagTypes: [BucketsSliceTags.BUCKETS],
});

// Helper functions
const buildQueryString = (params: GetBucketsParams): string => {
  const queryParams = new URLSearchParams();
  if (params.search) queryParams.append('search', params.search);
  return queryParams.toString();
};

const getBucketTags = (guid: string) => [
  { type: BucketsSliceTags.BUCKETS as const, id: guid },
  { type: BucketsSliceTags.BUCKETS, id: 'LIST' },
];

const bucketsSliceInstance = enhancedSdk.injectEndpoints({
  endpoints: (build: EndpointBuilder<BaseQueryFn<ApiBaseQueryArgs, unknown, unknown>, BucketsSliceTags, 'sdk'>) => ({
    getBuckets: build.query<BucketListResponse, void>({
      query: () => ({
        url: buildApiUrl('admin/buckets'),
        method: 'GET',
      }),
      transformResponse: (response: any): Bucket[] => (Array.isArray(response) ? response : []),
      providesTags: (result: Bucket[] | undefined) =>
        result
          ? [
              ...result.map(({ GUID }: Bucket) => ({ type: BucketsSliceTags.BUCKETS as const, id: GUID })),
              { type: BucketsSliceTags.BUCKETS, id: 'LIST' },
            ]
          : [{ type: BucketsSliceTags.BUCKETS, id: 'LIST' }],
    }),

    getBucketById: build.query<BucketResponse, string>({
      query: (guid: string) => ({ url: buildApiUrl(`admin/buckets/${guid}`), method: 'GET' }),
      transformResponse: (response: any): Bucket => response,
      providesTags: (_result: Bucket | undefined, _error: unknown, guid: string) => getBucketTags(guid),
    }),

    createBucket: build.mutation<BucketResponse, CreateBucketRequest>({
      query: (body: CreateBucketRequest) => ({ url: buildApiUrl('admin/buckets'), method: 'POST', body }),
      transformResponse: (response: any): Bucket => response,
      invalidatesTags: [{ type: BucketsSliceTags.BUCKETS, id: 'LIST' }],
    }),

    deleteBucket: build.mutation<DeleteBucketResponse, DeleteBucketParams>({
      query: ({ guid, destroy }: DeleteBucketParams) => ({
        url: buildApiUrl(`admin/buckets/${guid}${destroy ? '?destroy' : ''}`),
        method: 'DELETE',
      }),
      transformResponse: (): DeleteBucketResponse => ({ success: true }),
      invalidatesTags: (_result: DeleteBucketResponse | undefined, _error: unknown, { guid }: DeleteBucketParams) =>
        getBucketTags(guid),
    }),
  }),
});

export const { useGetBucketsQuery, useGetBucketByIdQuery, useCreateBucketMutation, useDeleteBucketMutation } =
  bucketsSliceInstance;
