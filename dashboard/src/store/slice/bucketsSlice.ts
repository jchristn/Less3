import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import { buildApiUrl } from '#/services/sdk.service';
import {
  parseListBucketResult,
  parseListAllMyBucketsResult,
  parseBucketTagging,
  generateBucketTaggingXml,
  parseBucketACL,
  generateBucketACLXml,
  type ListBucketResult,
} from '#/utils/xmlUtils';
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
  addTagTypes: [BucketsSliceTags.BUCKETS, BucketsSliceTags.BUCKET_TAGS],
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

const getBucketTagsCacheTag = (bucketName: string) => ({
  type: BucketsSliceTags.BUCKET_TAGS as const,
  id: bucketName,
});

const AWS_AUTH_HEADER =
  'AWS4-HMAC-SHA256 Credential=default/20130524/us-east-1/s3/aws4_request, SignedHeaders=host;range;x-amz-date, Signature=fe5f80f77d5fa3beca038a248ff027d0445342fe2855ddc963176630326f1024';

const bucketsSliceInstance = enhancedSdk.injectEndpoints({
  overrideExisting: true,
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
      invalidatesTags: (
        _result: DeleteBucketResponse | undefined,
        _error: unknown,
        { bucketName }: DeleteBucketParams
      ) => getBucketTags(bucketName),
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

    deleteBucketObject: build.mutation<DeleteBucketObjectResponse, DeleteBucketObjectParams>({
      async queryFn({ bucketGUID, objectKey }) {
        try {
          const baseUrl = apiEndpointURL.endsWith('/') ? apiEndpointURL.slice(0, -1) : apiEndpointURL;
          const response = await fetch(`${baseUrl}/${bucketGUID}/${objectKey}`, {
            method: 'DELETE',
            headers: {
              Authorization: AWS_AUTH_HEADER,
            },
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: `Failed to delete object: ${response.statusText}`,
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
              data: error?.message || 'Failed to delete object',
            },
          };
        }
      },
    }),

    deleteMultipleObjects: build.mutation<
      { deleted: string[]; errors: Array<{ key: string; error: string }> },
      { bucketGUID: string; objectKeys: string[] }
    >({
      async queryFn({ bucketGUID, objectKeys }) {
        try {
          const baseUrl = apiEndpointURL.endsWith('/') ? apiEndpointURL.slice(0, -1) : apiEndpointURL;

          // Build the XML body for S3 DeleteObjects API
          const objectsXml = objectKeys.map((key) => `<Object><Key>${key}</Key></Object>`).join('');
          const xmlBody = `<?xml version="1.0" encoding="UTF-8"?><Delete><Quiet>false</Quiet>${objectsXml}</Delete>`;

          const response = await fetch(`${baseUrl}/${bucketGUID}?delete`, {
            method: 'POST',
            headers: {
              Authorization: AWS_AUTH_HEADER,
              'Content-Type': 'application/xml',
            },
            body: xmlBody,
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: `Failed to delete objects: ${response.statusText}`,
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
        } catch (error: any) {
          return {
            error: {
              status: 'FETCH_ERROR',
              data: error?.message || 'Failed to delete objects',
            },
          };
        }
      },
    }),

    writeBucketTags: build.mutation<WriteBucketTagsResponse, WriteBucketTagsParams>({
      async queryFn({ bucketName, tags }) {
        try {
          const baseUrl = apiEndpointURL.endsWith('/') ? apiEndpointURL.slice(0, -1) : apiEndpointURL;
          const xmlBody = generateBucketTaggingXml(tags);
          const response = await fetch(`${baseUrl}/${bucketName}?tagging`, {
            method: 'PUT',
            headers: {
              Authorization: AWS_AUTH_HEADER,
              'Content-Type': 'application/xml',
            },
            body: xmlBody,
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: `Failed to write bucket tags: ${response.statusText}`,
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
              data: error?.message || 'Failed to write bucket tags',
            },
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
          const baseUrl = apiEndpointURL.endsWith('/') ? apiEndpointURL.slice(0, -1) : apiEndpointURL;
          const response = await fetch(`${baseUrl}/${bucketName}?tagging`, {
            method: 'GET',
            headers: {
              Authorization: AWS_AUTH_HEADER,
            },
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
                data: `Failed to get bucket tags: ${response.statusText}`,
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
        } catch (error: any) {
          return {
            error: {
              status: 'FETCH_ERROR',
              data: error?.message || 'Failed to get bucket tags',
            },
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
          const baseUrl = apiEndpointURL.endsWith('/') ? apiEndpointURL.slice(0, -1) : apiEndpointURL;
          const response = await fetch(`${baseUrl}/${bucketName}?tagging`, {
            method: 'DELETE',
            headers: {
              Authorization: AWS_AUTH_HEADER,
            },
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: `Failed to delete bucket tags: ${response.statusText}`,
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
              data: error?.message || 'Failed to delete bucket tags',
            },
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
      async queryFn({ bucketGUID, objectKey, tags }) {
        try {
          const baseUrl = apiEndpointURL.endsWith('/') ? apiEndpointURL.slice(0, -1) : apiEndpointURL;
          const xmlBody = generateBucketTaggingXml(tags);
          const response = await fetch(`${baseUrl}/${bucketGUID}/${objectKey}?tagging`, {
            method: 'PUT',
            headers: {
              Authorization: AWS_AUTH_HEADER,
              'Content-Type': 'application/xml',
            },
            body: xmlBody,
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: `Failed to write object tags: ${response.statusText}`,
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
              data: error?.message || 'Failed to write object tags',
            },
          };
        }
      },
    }),

    getObjectTags: build.query<GetObjectTagsResponse, GetObjectTagsParams>({
      async queryFn({ bucketGUID, objectKey }) {
        try {
          const baseUrl = apiEndpointURL.endsWith('/') ? apiEndpointURL.slice(0, -1) : apiEndpointURL;
          const response = await fetch(`${baseUrl}/${bucketGUID}/${objectKey}?tagging`, {
            method: 'GET',
            headers: {
              Authorization: AWS_AUTH_HEADER,
            },
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
                data: `Failed to get object tags: ${response.statusText}`,
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
        } catch (error: any) {
          return {
            error: {
              status: 'FETCH_ERROR',
              data: error?.message || 'Failed to get object tags',
            },
          };
        }
      },
    }),

    deleteObjectTags: build.mutation<DeleteObjectTagsResponse, DeleteObjectTagsParams>({
      async queryFn({ bucketGUID, objectKey }) {
        try {
          const baseUrl = apiEndpointURL.endsWith('/') ? apiEndpointURL.slice(0, -1) : apiEndpointURL;
          const response = await fetch(`${baseUrl}/${bucketGUID}/${objectKey}?tagging`, {
            method: 'DELETE',
            headers: {
              Authorization: AWS_AUTH_HEADER,
            },
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: `Failed to delete object tags: ${response.statusText}`,
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
              data: error?.message || 'Failed to delete object tags',
            },
          };
        }
      },
    }),

    writeBucketACL: build.mutation<WriteBucketACLResponse, WriteBucketACLParams>({
      async queryFn({ bucketName, owner, grants }) {
        try {
          const baseUrl = apiEndpointURL.endsWith('/') ? apiEndpointURL.slice(0, -1) : apiEndpointURL;
          const xmlBody = generateBucketACLXml(owner, grants);
          const response = await fetch(`${baseUrl}/${bucketName}?acl`, {
            method: 'PUT',
            headers: {
              Authorization: AWS_AUTH_HEADER,
              'Content-Type': 'application/xml',
            },
            body: xmlBody,
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: `Failed to write bucket ACL: ${response.statusText}`,
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
              data: error?.message || 'Failed to write bucket ACL',
            },
          };
        }
      },
    }),

    getBucketACL: build.query<GetBucketACLResponse, GetBucketACLParams>({
      async queryFn({ bucketName }) {
        try {
          const baseUrl = apiEndpointURL.endsWith('/') ? apiEndpointURL.slice(0, -1) : apiEndpointURL;
          const response = await fetch(`${baseUrl}/${bucketName}?acl`, {
            method: 'GET',
            headers: {
              Authorization: AWS_AUTH_HEADER,
            },
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: `Failed to get bucket ACL: ${response.statusText}`,
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
        } catch (error: any) {
          return {
            error: {
              status: 'FETCH_ERROR',
              data: error?.message || 'Failed to get bucket ACL',
            },
          };
        }
      },
    }),

    writeObjectACL: build.mutation<WriteObjectACLResponse, WriteObjectACLParams>({
      async queryFn({ bucketGUID, objectKey, owner, grants }) {
        try {
          const baseUrl = apiEndpointURL.endsWith('/') ? apiEndpointURL.slice(0, -1) : apiEndpointURL;
          const xmlBody = generateBucketACLXml(owner, grants);
          const response = await fetch(`${baseUrl}/${bucketGUID}/${objectKey}?acl`, {
            method: 'PUT',
            headers: {
              Authorization: AWS_AUTH_HEADER,
              'Content-Type': 'application/xml',
            },
            body: xmlBody,
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: `Failed to write object ACL: ${response.statusText}`,
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
              data: error?.message || 'Failed to write object ACL',
            },
          };
        }
      },
    }),

    getObjectACL: build.query<GetObjectACLResponse, GetObjectACLParams>({
      async queryFn({ bucketGUID, objectKey }) {
        try {
          const baseUrl = apiEndpointURL.endsWith('/') ? apiEndpointURL.slice(0, -1) : apiEndpointURL;
          const response = await fetch(`${baseUrl}/${bucketGUID}/${objectKey}?acl`, {
            method: 'GET',
            headers: {
              Authorization: AWS_AUTH_HEADER,
            },
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: `Failed to get object ACL: ${response.statusText}`,
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
        } catch (error: any) {
          return {
            error: {
              status: 'FETCH_ERROR',
              data: error?.message || 'Failed to get object ACL',
            },
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
