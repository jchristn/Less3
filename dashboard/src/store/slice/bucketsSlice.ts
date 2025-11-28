import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import { buildApiUrl } from '#/services/sdk.service';
import { MOCK_BUCKETS } from '#/constants/config';
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

// Mock bucket data
const mockBuckets: Bucket[] = [
  {
    GUID: 'default',
    OwnerGUID: 'default',
    Name: 'default',
    RegionString: 'us-west-1',
    StorageType: 'Disk',
    DiskDirectory: './disk/default/Objects/',
    EnableVersioning: false,
    EnablePublicWrite: false,
    EnablePublicRead: true,
    CreatedUtc: '2025-11-28T05:04:21.867597',
  },
  {
    GUID: 'testbucket',
    OwnerGUID: 'default',
    Name: 'testbucket',
    RegionString: 'us-east-1',
    StorageType: 'Disk',
    DiskDirectory: './Storage/testbucket/Objects/',
    EnableVersioning: true,
    EnablePublicWrite: false,
    EnablePublicRead: true,
    CreatedUtc: '2025-01-15T10:30:00.000000Z',
  },
  {
    GUID: 's3-backup',
    OwnerGUID: 'default',
    Name: 's3-backup',
    RegionString: 'eu-central-1',
    StorageType: 'S3',
    DiskDirectory: '',
    EnableVersioning: true,
    EnablePublicWrite: false,
    EnablePublicRead: false,
    CreatedUtc: '2025-02-20T14:20:00.000000Z',
  },
];

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
    getBuckets: MOCK_BUCKETS
      ? build.query<BucketListResponse, GetBucketsParams>({
          async queryFn(params: GetBucketsParams) {
            // Filter mock data based on search if provided
            let filteredBuckets = [...mockBuckets];
            if (params?.search) {
              const searchLower = params.search.toLowerCase();
              filteredBuckets = mockBuckets.filter(
                (bucket: Bucket) =>
                  bucket.Name.toLowerCase().includes(searchLower) ||
                  bucket.GUID.toLowerCase().includes(searchLower) ||
                  bucket.StorageType.toLowerCase().includes(searchLower)
              );
            }
            return { data: filteredBuckets };
          },
          providesTags: (result: Bucket[] | undefined) =>
            result
              ? [
                  ...result.map(({ GUID }: Bucket) => ({ type: BucketsSliceTags.BUCKETS as const, id: GUID })),
                  { type: BucketsSliceTags.BUCKETS, id: 'LIST' },
                ]
              : [{ type: BucketsSliceTags.BUCKETS, id: 'LIST' }],
        })
      : build.query<BucketListResponse, GetBucketsParams>({
          query: (params: GetBucketsParams = {}) => ({
            url: buildApiUrl(`admin/buckets${buildQueryString(params) ? `?${buildQueryString(params)}` : ''}`),
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

    getBucketById: MOCK_BUCKETS
      ? build.query<BucketResponse, string>({
          async queryFn(guid: string) {
            const bucket = mockBuckets.find((b: Bucket) => b.GUID === guid);
            if (!bucket) {
              return { error: { status: 404, data: 'Bucket not found' } };
            }
            return { data: bucket };
          },
          providesTags: (_result: Bucket | undefined, _error: unknown, guid: string) => getBucketTags(guid),
        })
      : build.query<BucketResponse, string>({
          query: (guid: string) => ({ url: buildApiUrl(`admin/buckets/${guid}`), method: 'GET' }),
          transformResponse: (response: any): Bucket => response,
          providesTags: (_result: Bucket | undefined, _error: unknown, guid: string) => getBucketTags(guid),
        }),

    createBucket: MOCK_BUCKETS
      ? build.mutation<BucketResponse, CreateBucketRequest>({
          async queryFn(body: CreateBucketRequest) {
            // Create mock bucket response
            const newBucket: Bucket = {
              GUID: body.GUID,
              OwnerGUID: body.OwnerGUID || 'default',
              Name: body.Name,
              RegionString: body.RegionString || 'us-west-1',
              StorageType: body.StorageType || 'Disk',
              DiskDirectory: body.DiskDirectory || `./Storage/${body.GUID}/Objects/`,
              EnableVersioning: body.EnableVersioning || false,
              EnablePublicWrite: body.EnablePublicWrite || false,
              EnablePublicRead: body.EnablePublicRead !== undefined ? body.EnablePublicRead : true,
              CreatedUtc: body.CreatedUtc || new Date().toISOString(),
            };
            // Add to mock data
            mockBuckets.push(newBucket);
            return { data: newBucket };
          },
          invalidatesTags: [{ type: BucketsSliceTags.BUCKETS, id: 'LIST' }],
        })
      : build.mutation<BucketResponse, CreateBucketRequest>({
          query: (body: CreateBucketRequest) => ({ url: buildApiUrl('admin/buckets'), method: 'POST', body }),
          transformResponse: (response: any): Bucket => response,
          invalidatesTags: [{ type: BucketsSliceTags.BUCKETS, id: 'LIST' }],
        }),

    deleteBucket: MOCK_BUCKETS
      ? build.mutation<DeleteBucketResponse, DeleteBucketParams>({
          async queryFn({ guid }: DeleteBucketParams) {
            // Remove from mock data
            const index = mockBuckets.findIndex((bucket: Bucket) => bucket.GUID === guid);
            if (index > -1) {
              mockBuckets.splice(index, 1);
            }
            return { data: { success: true } };
          },
          invalidatesTags: (_result: DeleteBucketResponse | undefined, _error: unknown, { guid }: DeleteBucketParams) =>
            getBucketTags(guid),
        })
      : build.mutation<DeleteBucketResponse, DeleteBucketParams>({
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
