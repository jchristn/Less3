import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import { buildApiUrl } from '#/services/sdk.service';
import { parseListBucketResult, parseListAllMyBucketsResult, type ListBucketResult } from '#/utils/xmlUtils';
import { apiEndpointURL } from '#/constants/config';
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

const getBucketTags = (bucketName: string) => [
  { type: BucketsSliceTags.BUCKETS as const, id: bucketName },
  { type: BucketsSliceTags.BUCKETS, id: 'LIST' },
];

const AWS_AUTH_HEADER =
  'AWS4-HMAC-SHA256 Credential=default/20130524/us-east-1/s3/aws4_request, SignedHeaders=host;range;x-amz-date, Signature=fe5f80f77d5fa3beca038a248ff027d0445342fe2855ddc963176630326f1024';

const bucketsSliceInstance = enhancedSdk.injectEndpoints({
  endpoints: (build: EndpointBuilder<BaseQueryFn<ApiBaseQueryArgs, unknown, unknown>, BucketsSliceTags, 'sdk'>) => ({
    getBuckets: build.query<BucketListResponse, void>({
      async queryFn() {
        try {
          const baseUrl = apiEndpointURL.endsWith('/') ? apiEndpointURL.slice(0, -1) : apiEndpointURL;
          const response = await fetch(`${baseUrl}/`, {
            method: 'GET',
            headers: {
              Authorization: AWS_AUTH_HEADER,
            },
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: `Failed to fetch buckets: ${response.statusText}`,
              },
            };
          }

          const xmlText = await response.text();
          const listAllMyBucketsResult = parseListAllMyBucketsResult(xmlText);

          // Transform to Bucket[] format
          const buckets: Bucket[] = listAllMyBucketsResult.Buckets.map((bucket) => ({
            Name: bucket.Name,
            CreationDate: bucket.CreationDate,
          }));

          return { data: buckets };
        } catch (error: any) {
          return {
            error: {
              status: 'FETCH_ERROR',
              data: error?.message || 'Failed to fetch buckets',
            },
          };
        }
      },
      providesTags: (result: Bucket[] | undefined) =>
        result
          ? [
              ...result.map(({ Name }: Bucket) => ({ type: BucketsSliceTags.BUCKETS as const, id: Name })),
              { type: BucketsSliceTags.BUCKETS, id: 'LIST' },
            ]
          : [{ type: BucketsSliceTags.BUCKETS, id: 'LIST' }],
    }),

    getBucketById: build.query<BucketResponse, string>({
      query: (bucketName: string) => ({ url: buildApiUrl(`admin/buckets/${bucketName}`), method: 'GET' }),
      transformResponse: (response: any): Bucket => response,
      providesTags: (_result: Bucket | undefined, _error: unknown, bucketName: string) => getBucketTags(bucketName),
    }),

    createBucket: build.mutation<BucketResponse, CreateBucketRequest>({
      async queryFn({ Name: bucketName }) {
        try {
          const baseUrl = apiEndpointURL.endsWith('/') ? apiEndpointURL.slice(0, -1) : apiEndpointURL;
          const response = await fetch(`${baseUrl}/${bucketName}`, {
            method: 'PUT',
            headers: {
              Authorization: AWS_AUTH_HEADER,
            },
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: `Failed to create bucket: ${response.statusText}`,
              },
            };
          }

          // Create bucket response - typically empty or minimal
          const bucket: Bucket = {
            Name: bucketName,
            CreationDate: new Date().toISOString(),
          };

          return { data: bucket };
        } catch (error: any) {
          return {
            error: {
              status: 'FETCH_ERROR',
              data: error?.message || 'Failed to create bucket',
            },
          };
        }
      },
      invalidatesTags: [{ type: BucketsSliceTags.BUCKETS, id: 'LIST' }],
    }),

    deleteBucket: build.mutation<DeleteBucketResponse, DeleteBucketParams>({
      async queryFn({ bucketName }) {
        try {
          const baseUrl = apiEndpointURL.endsWith('/') ? apiEndpointURL.slice(0, -1) : apiEndpointURL;
          const response = await fetch(`${baseUrl}/${bucketName}`, {
            method: 'DELETE',
            headers: {
              Authorization: AWS_AUTH_HEADER,
            },
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: `Failed to delete bucket: ${response.statusText}`,
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
              data: error?.message || 'Failed to delete bucket',
            },
          };
        }
      },
      invalidatesTags: (_result: DeleteBucketResponse | undefined, _error: unknown, { bucketName }: DeleteBucketParams) =>
        getBucketTags(bucketName),
    }),

    listBucketObjects: build.query<ListBucketResult, ListBucketObjectsParams>({
      async queryFn({ bucketGUID }) {
        try {
          const baseUrl = apiEndpointURL.endsWith('/') ? apiEndpointURL.slice(0, -1) : apiEndpointURL;
          const response = await fetch(`${baseUrl}/${bucketGUID}/`, {
            method: 'GET',
            headers: {
              Authorization: AWS_AUTH_HEADER,
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
          const baseUrl = apiEndpointURL.endsWith('/') ? apiEndpointURL.slice(0, -1) : apiEndpointURL;
          const response = await fetch(`${baseUrl}/${bucketGUID}/${objectKey}`, {
            method: 'GET',
            headers: {
              Authorization: AWS_AUTH_HEADER,
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
          const baseUrl = apiEndpointURL.endsWith('/') ? apiEndpointURL.slice(0, -1) : apiEndpointURL;
          const response = await fetch(`${baseUrl}/${bucketGUID}/${objectKey}`, {
            method: 'PUT',
            headers: {
              Authorization: AWS_AUTH_HEADER,
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
