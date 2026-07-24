import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import {
  adminRequest,
  readBackendErrorMessage,
  s3Fetch,
  toRtkQueryError,
} from '#/services/backendApi.service';
import {
  parseListBucketResult,
  parseBucketTagging,
  generateBucketTaggingXml,
  parseBucketACL,
  generateBucketACLXml,
  type ListBucketResult,
} from '#/utils/xmlUtils';
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
  UploadBucketObjectParams,
  UploadBucketObjectResponse,
  DeleteBucketObjectParams,
  DeleteBucketObjectResponse,
  WriteBucketTagsParams,
  WriteBucketTagsResponse,
  GetBucketTagsParams,
  GetBucketTagsResponse,
  DeleteBucketTagsParams,
  DeleteBucketTagsResponse,
  WriteObjectTagsParams,
  WriteObjectTagsResponse,
  GetObjectTagsParams,
  GetObjectTagsResponse,
  DeleteObjectTagsParams,
  DeleteObjectTagsResponse,
  WriteBucketACLParams,
  WriteBucketACLResponse,
  GetBucketACLParams,
  GetBucketACLResponse,
  WriteObjectACLParams,
  WriteObjectACLResponse,
  GetObjectACLParams,
  GetObjectACLResponse,
} from './bucketsTypes';
import { DashboardStatsSliceTags } from './dashboardStatsSlice';

export enum BucketsSliceTags {
  BUCKETS = 'BUCKETS',
  BUCKET_TAGS = 'BUCKET_TAGS',
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
  UploadBucketObjectParams,
  UploadBucketObjectResponse,
  DeleteBucketObjectParams,
  DeleteBucketObjectResponse,
  WriteBucketTagsParams,
  WriteBucketTagsResponse,
  GetBucketTagsParams,
  GetBucketTagsResponse,
  DeleteBucketTagsParams,
  DeleteBucketTagsResponse,
  WriteObjectTagsParams,
  WriteObjectTagsResponse,
  GetObjectTagsParams,
  GetObjectTagsResponse,
  DeleteObjectTagsParams,
  DeleteObjectTagsResponse,
  WriteBucketACLParams,
  WriteBucketACLResponse,
  GetBucketACLParams,
  GetBucketACLResponse,
  WriteObjectACLParams,
  WriteObjectACLResponse,
  GetObjectACLParams,
  GetObjectACLResponse,
};

const enhancedSdk = sdkSlice.enhanceEndpoints({
  addTagTypes: [
    BucketsSliceTags.BUCKETS,
    BucketsSliceTags.BUCKET_TAGS,
    DashboardStatsSliceTags.DASHBOARD_STATS,
  ],
});

// Helper functions
const buildQueryString = (params: GetBucketsParams): string => {
  const queryParams = new URLSearchParams();
  if (params.search) queryParams.append('search', params.search);
  return queryParams.toString();
};

const getBucketCacheId = (bucket: Pick<Bucket, 'Id' | 'Name'> | string): string =>
  typeof bucket === 'string' ? bucket : bucket.Id || bucket.Name;

const normalizeBucket = (bucket: any): Bucket => ({
  ...bucket,
  Name: bucket?.Name || '',
  Id: bucket?.Id,
  CreatedUtc: bucket?.CreatedUtc || bucket?.CreationDate || '',
  CreationDate: bucket?.CreationDate || bucket?.CreatedUtc || '',
});

const getBucketTags = (bucket: Pick<Bucket, 'Id' | 'Name'> | string) => [
  { type: BucketsSliceTags.BUCKETS as const, id: getBucketCacheId(bucket) },
  { type: BucketsSliceTags.BUCKETS, id: 'LIST' },
];

const getBucketTagsCacheTag = (bucketName: string) => ({
  type: BucketsSliceTags.BUCKET_TAGS as const,
  id: bucketName,
});

const getDashboardStatsInvalidationTags = () => [
  { type: DashboardStatsSliceTags.DASHBOARD_STATS as const, id: 'SUMMARY' },
  { type: DashboardStatsSliceTags.DASHBOARD_STATS as const, id: 'REPORT' },
];

const bucketsSliceInstance = enhancedSdk.injectEndpoints({
  overrideExisting: true,
  endpoints: (
    build: EndpointBuilder<
      BaseQueryFn<ApiBaseQueryArgs, unknown, unknown>,
      BucketsSliceTags | DashboardStatsSliceTags,
      'sdk'
    >
  ) => ({
    getBuckets: build.query<BucketListResponse, void>({
      async queryFn() {
        try {
          const responseData = await adminRequest<unknown>('admin/buckets', { cache: 'no-store' });
          const buckets: Bucket[] = Array.isArray(responseData) ? responseData.map(normalizeBucket) : [];

          return { data: buckets };
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to fetch buckets'),
          };
        }
      },
      providesTags: (result: Bucket[] | undefined) =>
        result
          ? [
              ...result.map((bucket: Bucket) => ({
                type: BucketsSliceTags.BUCKETS as const,
                id: getBucketCacheId(bucket),
              })),
              { type: BucketsSliceTags.BUCKETS, id: 'LIST' },
            ]
          : [{ type: BucketsSliceTags.BUCKETS, id: 'LIST' }],
    }),

    getBucketById: build.query<BucketResponse, string>({
      query: (id: string) => ({ url: `admin/buckets/${id}`, method: 'GET' }),
      transformResponse: (response: any): Bucket => normalizeBucket(response),
      providesTags: (_result: Bucket | undefined, _error: unknown, id: string) => getBucketTags(id),
    }),

    createBucket: build.mutation<BucketResponse, CreateBucketRequest>({
      async queryFn({ Name: bucketName }) {
        try {
          const response = await adminRequest<unknown>('admin/buckets', {
            method: 'POST',
            body: { Name: bucketName },
          });

          return {
            data: normalizeBucket(response || {
              Name: bucketName,
              CreatedUtc: new Date().toISOString(),
            }),
          };
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to create bucket'),
          };
        }
      },
      invalidatesTags: [
        { type: BucketsSliceTags.BUCKETS, id: 'LIST' },
        ...getDashboardStatsInvalidationTags(),
      ],
    }),

    deleteBucket: build.mutation<DeleteBucketResponse, DeleteBucketParams>({
      async queryFn({ id }) {
        try {
          await adminRequest<unknown>(`admin/buckets/${id}?destroy=true`, {
            method: 'DELETE',
          });

          return {
            data: {
              success: true,
            },
          };
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to delete bucket'),
          };
        }
      },
      invalidatesTags: (
        _result: DeleteBucketResponse | undefined,
        _error: unknown,
        { id }: DeleteBucketParams
      ) => [...getBucketTags(id), ...getDashboardStatsInvalidationTags()],
    }),

    listBucketObjects: build.query<ListBucketResult, ListBucketObjectsParams>({
      async queryFn({ bucketId }) {
        try {
          const response = await s3Fetch(`/${bucketId}/`, {
            method: 'GET',
            cache: 'no-store',
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readBackendErrorMessage(response, 'Failed to fetch objects'),
              },
            };
          }

          const xmlText = await response.text();
          const listBucketResult = parseListBucketResult(xmlText);

          return { data: listBucketResult };
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to fetch bucket objects'),
          };
        }
      },
    }),

    downloadBucketObject: build.query<DownloadBucketObjectResponse, DownloadBucketObjectParams>({
      async queryFn({ bucketId, objectKey }) {
        try {
          const response = await s3Fetch(`/${bucketId}/${objectKey}`, {
            method: 'GET',
            cache: 'no-store',
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readBackendErrorMessage(response, 'Failed to download object'),
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
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to download object'),
          };
        }
      },
    }),

    writeBucketObject: build.mutation<WriteBucketObjectResponse, WriteBucketObjectParams>({
      async queryFn({ bucketId, objectKey, content, contentType }) {
        try {
          const response = await s3Fetch(`/${bucketId}/${objectKey}`, {
            method: 'PUT',
            headers: {
              'Content-Type': contentType || 'text/plain',
            },
            body: content,
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readBackendErrorMessage(response, 'Failed to write object'),
              },
            };
          }

          return {
            data: {
              success: true,
            },
          };
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to write object'),
          };
        }
      },
      invalidatesTags: getDashboardStatsInvalidationTags,
    }),

    uploadBucketObject: build.mutation<UploadBucketObjectResponse, UploadBucketObjectParams>({
      async queryFn({ bucketId, objectKey, file }) {
        try {
          const response = await s3Fetch(`/${bucketId}/${objectKey}`, {
            method: 'PUT',
            headers: {
              'Content-Type': file.type || 'application/octet-stream',
            },
            body: file,
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readBackendErrorMessage(response, 'Failed to upload object'),
              },
            };
          }

          return {
            data: {
              success: true,
            },
          };
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to upload object'),
          };
        }
      },
      invalidatesTags: getDashboardStatsInvalidationTags,
    }),

    deleteBucketObject: build.mutation<DeleteBucketObjectResponse, DeleteBucketObjectParams>({
      async queryFn({ bucketId, objectKey }) {
        try {
          const response = await s3Fetch(`/${bucketId}/${objectKey}`, {
            method: 'DELETE',
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readBackendErrorMessage(response, 'Failed to delete object'),
              },
            };
          }

          return {
            data: {
              success: true,
            },
          };
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to delete object'),
          };
        }
      },
      invalidatesTags: getDashboardStatsInvalidationTags,
    }),

    deleteMultipleObjects: build.mutation<
      { deleted: string[]; errors: Array<{ key: string; error: string }> },
      { bucketId: string; objectKeys: string[] }
    >({
      async queryFn({ bucketId, objectKeys }) {
        try {
          // Build the XML body for S3 DeleteObjects API
          const objectsXml = objectKeys.map((key) => `<Object><Key>${key}</Key></Object>`).join('');
          const xmlBody = `<?xml version="1.0" encoding="UTF-8"?><Delete><Quiet>false</Quiet>${objectsXml}</Delete>`;

          const response = await s3Fetch(`/${bucketId}?delete`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/xml',
            },
            body: xmlBody,
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readBackendErrorMessage(response, 'Failed to delete objects'),
              },
            };
          }

          // Parse the response XML to get deleted keys and errors
          const responseText = await response.text();
          const deleted: string[] = [];
          const errors: Array<{ key: string; error: string }> = [];

          // Simple regex parsing for deleted keys
          const deletedRegex = /<Deleted>\s*<Key>([^<]+)<\/Key>/g;
          let deletedMatch;
          while ((deletedMatch = deletedRegex.exec(responseText)) !== null) {
            deleted.push(deletedMatch[1]);
          }

          // Simple regex parsing for errors
          const errorRegex = /<Error>\s*<Key>([^<]+)<\/Key>\s*<Code>([^<]+)<\/Code>\s*<Message>([^<]+)<\/Message>/g;
          let errorMatch;
          while ((errorMatch = errorRegex.exec(responseText)) !== null) {
            errors.push({ key: errorMatch[1], error: `${errorMatch[2]}: ${errorMatch[3]}` });
          }

          return {
            data: {
              deleted,
              errors,
            },
          };
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to delete objects'),
          };
        }
      },
      invalidatesTags: getDashboardStatsInvalidationTags,
    }),

    writeBucketTags: build.mutation<WriteBucketTagsResponse, WriteBucketTagsParams>({
      async queryFn({ bucketName, tags }) {
        try {
          const xmlBody = generateBucketTaggingXml(tags);
          const response = await s3Fetch(`/${bucketName}?tagging`, {
            method: 'PUT',
            headers: {
              'Content-Type': 'application/xml',
            },
            body: xmlBody,
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readBackendErrorMessage(response, 'Failed to write bucket tags'),
              },
            };
          }

          return {
            data: {
              success: true,
            },
          };
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to write bucket tags'),
          };
        }
      },
      invalidatesTags: (
        _result: WriteBucketTagsResponse | undefined,
        _error: unknown,
        { bucketName }: WriteBucketTagsParams
      ) => [getBucketTagsCacheTag(bucketName)],
    }),

    getBucketTags: build.query<GetBucketTagsResponse, GetBucketTagsParams>({
      async queryFn({ bucketName }) {
        try {
          const response = await s3Fetch(`/${bucketName}?tagging`, {
            method: 'GET',
            cache: 'no-store',
          });

          if (!response.ok) {
            if (response.status === 404) {
              // No tags found, return empty array
              return {
                data: {
                  tags: [],
                },
              };
            }
            return {
              error: {
                status: response.status,
                data: await readBackendErrorMessage(response, 'Failed to get bucket tags'),
              },
            };
          }

          const xmlText = await response.text();
          const tags = parseBucketTagging(xmlText);

          return {
            data: {
              tags,
            },
          };
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to get bucket tags'),
          };
        }
      },
      providesTags: (
        _result: GetBucketTagsResponse | undefined,
        _error: unknown,
        { bucketName }: GetBucketTagsParams
      ) => [getBucketTagsCacheTag(bucketName)],
    }),

    deleteBucketTags: build.mutation<DeleteBucketTagsResponse, DeleteBucketTagsParams>({
      async queryFn({ bucketName }) {
        try {
          const response = await s3Fetch(`/${bucketName}?tagging`, {
            method: 'DELETE',
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readBackendErrorMessage(response, 'Failed to delete bucket tags'),
              },
            };
          }

          return {
            data: {
              success: true,
            },
          };
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to delete bucket tags'),
          };
        }
      },
      invalidatesTags: (
        _result: DeleteBucketTagsResponse | undefined,
        _error: unknown,
        { bucketName }: DeleteBucketTagsParams
      ) => [getBucketTagsCacheTag(bucketName)],
    }),

    writeObjectTags: build.mutation<WriteObjectTagsResponse, WriteObjectTagsParams>({
      async queryFn({ bucketId, objectKey, tags }) {
        try {
          const xmlBody = generateBucketTaggingXml(tags);
          const response = await s3Fetch(`/${bucketId}/${objectKey}?tagging`, {
            method: 'PUT',
            headers: {
              'Content-Type': 'application/xml',
            },
            body: xmlBody,
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readBackendErrorMessage(response, 'Failed to write object tags'),
              },
            };
          }

          return {
            data: {
              success: true,
            },
          };
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to write object tags'),
          };
        }
      },
    }),

    getObjectTags: build.query<GetObjectTagsResponse, GetObjectTagsParams>({
      async queryFn({ bucketId, objectKey }) {
        try {
          const response = await s3Fetch(`/${bucketId}/${objectKey}?tagging`, {
            method: 'GET',
            cache: 'no-store',
          });

          if (!response.ok) {
            if (response.status === 404) {
              // No tags found, return empty array
              return {
                data: {
                  tags: [],
                },
              };
            }
            return {
              error: {
                status: response.status,
                data: await readBackendErrorMessage(response, 'Failed to get object tags'),
              },
            };
          }

          const xmlText = await response.text();
          const tags = parseBucketTagging(xmlText);

          return {
            data: {
              tags,
            },
          };
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to get object tags'),
          };
        }
      },
    }),

    deleteObjectTags: build.mutation<DeleteObjectTagsResponse, DeleteObjectTagsParams>({
      async queryFn({ bucketId, objectKey }) {
        try {
          const response = await s3Fetch(`/${bucketId}/${objectKey}?tagging`, {
            method: 'DELETE',
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readBackendErrorMessage(response, 'Failed to delete object tags'),
              },
            };
          }

          return {
            data: {
              success: true,
            },
          };
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to delete object tags'),
          };
        }
      },
    }),

    writeBucketACL: build.mutation<WriteBucketACLResponse, WriteBucketACLParams>({
      async queryFn({ bucketName, owner, grants }) {
        try {
          const xmlBody = generateBucketACLXml(owner, grants);
          const response = await s3Fetch(`/${bucketName}?acl`, {
            method: 'PUT',
            headers: {
              'Content-Type': 'application/xml',
            },
            body: xmlBody,
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readBackendErrorMessage(response, 'Failed to write bucket ACL'),
              },
            };
          }

          return {
            data: {
              success: true,
            },
          };
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to write bucket ACL'),
          };
        }
      },
    }),

    getBucketACL: build.query<GetBucketACLResponse, GetBucketACLParams>({
      async queryFn({ bucketName }) {
        try {
          const response = await s3Fetch(`/${bucketName}?acl`, {
            method: 'GET',
            cache: 'no-store',
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readBackendErrorMessage(response, 'Failed to get bucket ACL'),
              },
            };
          }

          const xmlText = await response.text();
          const acl = parseBucketACL(xmlText);

          return {
            data: {
              acl,
            },
          };
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to get bucket ACL'),
          };
        }
      },
    }),

    writeObjectACL: build.mutation<WriteObjectACLResponse, WriteObjectACLParams>({
      async queryFn({ bucketId, objectKey, owner, grants }) {
        try {
          const xmlBody = generateBucketACLXml(owner, grants);
          const response = await s3Fetch(`/${bucketId}/${objectKey}?acl`, {
            method: 'PUT',
            headers: {
              'Content-Type': 'application/xml',
            },
            body: xmlBody,
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readBackendErrorMessage(response, 'Failed to write object ACL'),
              },
            };
          }

          return {
            data: {
              success: true,
            },
          };
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to write object ACL'),
          };
        }
      },
    }),

    getObjectACL: build.query<GetObjectACLResponse, GetObjectACLParams>({
      async queryFn({ bucketId, objectKey }) {
        try {
          const response = await s3Fetch(`/${bucketId}/${objectKey}?acl`, {
            method: 'GET',
            cache: 'no-store',
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readBackendErrorMessage(response, 'Failed to get object ACL'),
              },
            };
          }

          const xmlText = await response.text();
          const acl = parseBucketACL(xmlText);

          return {
            data: {
              acl,
            },
          };
        } catch (error) {
          return {
            error: toRtkQueryError(error, 'Failed to get object ACL'),
          };
        }
      },
    }),
  }),
});

// Export underlying API instance for testing/introspection
export const bucketsSliceApi = bucketsSliceInstance;

export const {
  useGetBucketsQuery,
  useGetBucketByIdQuery,
  useCreateBucketMutation,
  useDeleteBucketMutation,
  useListBucketObjectsQuery,
  useLazyDownloadBucketObjectQuery,
  useWriteBucketObjectMutation,
  useUploadBucketObjectMutation,
  useDeleteBucketObjectMutation,
  useDeleteMultipleObjectsMutation,
  useWriteBucketTagsMutation,
  useGetBucketTagsQuery,
  useDeleteBucketTagsMutation,
  useWriteObjectTagsMutation,
  useGetObjectTagsQuery,
  useDeleteObjectTagsMutation,
  useWriteBucketACLMutation,
  useGetBucketACLQuery,
  useWriteObjectACLMutation,
  useGetObjectACLQuery,
} = bucketsSliceInstance;
