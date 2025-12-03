import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import { buildApiUrl } from '#/services/sdk.service';
import { parseListBucketResult, type ListBucketResult } from '#/utils/xmlUtils';
import type {
  Bucket,
  BucketListResponse,
  BucketResponse,
  CreateBucketRequest,
  UpdateBucketRequest,
  DeleteBucketParams,
  DeleteBucketResponse,
  GetBucketsParams,
  ListBucketObjectsParams,
  DownloadBucketObjectParams,
  DownloadBucketObjectResponse,
  WriteBucketObjectParams,
  WriteBucketObjectResponse,
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
  ListBucketObjectsParams,
  DownloadBucketObjectParams,
  DownloadBucketObjectResponse,
  WriteBucketObjectParams,
  WriteBucketObjectResponse,
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

    listBucketObjects: build.query<ListBucketResult, ListBucketObjectsParams>({
      async queryFn({ bucketGUID }) {
        try {
          const response = await fetch(`http://localhost:8000/${bucketGUID}/`, {
            method: 'GET',
            headers: {
              Authorization:
                'AWS4-HMAC-SHA256 Credential=default/20130524/us-east-1/s3/aws4_request, SignedHeaders=host;range;x-amz-date, Signature=fe5f80f77d5fa3beca038a248ff027d0445342fe2855ddc963176630326f1024',
            },
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: `Failed to fetch objects: ${response.statusText}`,
              },
            };
          }

          const xmlText = await response.text();
          const listBucketResult = parseListBucketResult(xmlText);

          return { data: listBucketResult };
        } catch (error: any) {
          return {
            error: {
              status: 'FETCH_ERROR',
              data: error?.message || 'Failed to fetch bucket objects',
            },
          };
        }
      },
    }),

    downloadBucketObject: build.query<DownloadBucketObjectResponse, DownloadBucketObjectParams>({
      async queryFn({ bucketGUID, objectKey }) {
        try {
          const response = await fetch(`http://localhost:8000/${bucketGUID}/${objectKey}`, {
            method: 'GET',
            headers: {
              Authorization:
                'AWS4-HMAC-SHA256 Credential=default/20130524/us-east-1/s3/aws4_request, SignedHeaders=host;range;x-amz-date, Signature=fe5f80f77d5fa3beca038a248ff027d0445342fe2855ddc963176630326f1024',
            },
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: `Failed to download object: ${response.statusText}`,
              },
            };
          }

          const content = await response.text();
          const contentType = response.headers.get('content-type') || 'text/plain';

          return {
            data: {
              content,
              contentType,
            },
          };
        } catch (error: any) {
          return {
            error: {
              status: 'FETCH_ERROR',
              data: error?.message || 'Failed to download object',
            },
          };
        }
      },
    }),

    writeBucketObject: build.mutation<WriteBucketObjectResponse, WriteBucketObjectParams>({
      async queryFn({ bucketGUID, objectKey, content }) {
        try {
          const response = await fetch(`http://localhost:8000/${bucketGUID}/${objectKey}`, {
            method: 'PUT',
            headers: {
              Authorization:
                'AWS4-HMAC-SHA256 Credential=default/20130524/us-east-1/s3/aws4_request, SignedHeaders=host;range;x-amz-date, Signature=fe5f80f77d5fa3beca038a248ff027d0445342fe2855ddc963176630326f1024',
              'Content-Type': 'text/plain',
            },
            body: content,
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: `Failed to write object: ${response.statusText}`,
              },
            };
          }

          return {
            data: {
              success: true,
            },
          };
        } catch (error: any) {
          return {
            error: {
              status: 'FETCH_ERROR',
              data: error?.message || 'Failed to write object',
            },
          };
        }
      },
    }),
  }),
});

export const {
  useGetBucketsQuery,
  useGetBucketByIdQuery,
  useCreateBucketMutation,
  useDeleteBucketMutation,
  useListBucketObjectsQuery,
  useLazyDownloadBucketObjectQuery,
  useWriteBucketObjectMutation,
} = bucketsSliceInstance;
