import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import { buildAdminApiHeaders, buildApiUrl, getBaseUrl } from '#/services/sdk.service';
import {
  parseListBucketResult,
  parseBucketTagging,
  generateBucketTaggingXml,
  parseBucketACL,
  generateBucketACLXml,
  type ListBucketResult,
} from '#/utils/xmlUtils';
import { buildSignedS3Headers, selectS3Credential, type S3CredentialLike } from '#/utils/s3Auth';
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
  addTagTypes: [BucketsSliceTags.BUCKETS, BucketsSliceTags.BUCKET_TAGS],
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

const readAdminErrorMessage = async (response: Response, fallbackMessage: string): Promise<string> => {
  const responseText = (await response.text()).trim();

  if (responseText) {
    return responseText;
  }

  const statusSuffix = response.statusText ? `${response.status} ${response.statusText}` : String(response.status);
  return `${fallbackMessage}: ${statusSuffix}`;
};

const readS3ErrorMessage = async (response: Response, fallbackMessage: string): Promise<string> => {
  const responseText = (await response.text()).trim();

  if (responseText) {
    const codeMatch = responseText.match(/<Code>([^<]+)<\/Code>/i);
    const messageMatch = responseText.match(/<Message>([^<]+)<\/Message>/i);
    const code = codeMatch?.[1];
    const message = messageMatch?.[1];

    if (code || message) {
      return `${fallbackMessage}: ${[code, message].filter(Boolean).join(' - ')}`;
    }

    return responseText;
  }

  return `${fallbackMessage}: ${response.statusText || response.status}`;
};

const getBucketTags = (bucket: Pick<Bucket, 'Id' | 'Name'> | string) => [
  { type: BucketsSliceTags.BUCKETS as const, id: getBucketCacheId(bucket) },
  { type: BucketsSliceTags.BUCKETS, id: 'LIST' },
];

const getBucketTagsCacheTag = (bucketName: string) => ({
  type: BucketsSliceTags.BUCKET_TAGS as const,
  id: bucketName,
});

const normalizeS3Headers = (headers?: Record<string, string | undefined>): Record<string, string> =>
  Object.entries(headers || {}).reduce<Record<string, string>>((accumulator, [key, value]) => {
    if (typeof value === 'string' && value.length > 0) {
      accumulator[key] = value;
    }

    return accumulator;
  }, {});

const fetchAdminCredentials = async (): Promise<S3CredentialLike[]> => {
  const response = await fetch(buildApiUrl('admin/credentials'), {
    method: 'GET',
    headers: buildAdminApiHeaders(),
    cache: 'no-store',
  });

  if (!response.ok) {
    return [];
  }

  const responseData = await response.json();
  return Array.isArray(responseData) ? responseData : [];
};

const fetchCredentialById = async (id: string): Promise<S3CredentialLike | null> => {
  const response = await fetch(buildApiUrl(`admin/credentials/${id}`), {
    method: 'GET',
    headers: buildAdminApiHeaders(),
    cache: 'no-store',
  });

  if (!response.ok) {
    return null;
  }

  return (await response.json()) as S3CredentialLike;
};

const resolveS3Credential = async (): Promise<S3CredentialLike | null> => {
  const credentials = await fetchAdminCredentials();
  const selectedCredential = selectS3Credential(credentials);

  if (!selectedCredential) {
    return null;
  }

  if (selectedCredential.SecretKey?.trim()) {
    return selectedCredential;
  }

  if (!selectedCredential.Id) {
    return null;
  }

  return fetchCredentialById(selectedCredential.Id);
};

const buildS3RequestHeaders = async (
  method: string,
  url: string,
  options?: {
    headers?: Record<string, string | undefined>;
    body?: BodyInit | null;
  }
): Promise<Record<string, string>> => {
  const normalizedHeaders = normalizeS3Headers(options?.headers);
  const credential = await resolveS3Credential();

  if (!credential?.AccessKey || !credential.SecretKey) {
    return normalizedHeaders;
  }

  return buildSignedS3Headers({
    method,
    url,
    accessKey: credential.AccessKey,
    secretKey: credential.SecretKey,
    headers: normalizedHeaders,
    body: options?.body,
  });
};

const fetchS3 = async (
  url: string,
  options: {
    method: string;
    headers?: Record<string, string | undefined>;
    body?: BodyInit | null;
    cache?: RequestCache;
  }
): Promise<Response> => {
  const headers = await buildS3RequestHeaders(options.method, url, {
    headers: options.headers,
    body: options.body,
  });

  return fetch(url, {
    method: options.method,
    headers,
    body: options.body,
    cache: options.cache,
  });
};

const bucketsSliceInstance = enhancedSdk.injectEndpoints({
  overrideExisting: true,
  endpoints: (build: EndpointBuilder<BaseQueryFn<ApiBaseQueryArgs, unknown, unknown>, BucketsSliceTags, 'sdk'>) => ({
    getBuckets: build.query<BucketListResponse, void>({
      async queryFn() {
        try {
          const response = await fetch(buildApiUrl('admin/buckets'), {
            method: 'GET',
            headers: buildAdminApiHeaders(),
            cache: 'no-store',
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readAdminErrorMessage(response, 'Failed to fetch buckets'),
              },
            };
          }

          const responseData = await response.json();
          const buckets: Bucket[] = Array.isArray(responseData) ? responseData.map(normalizeBucket) : [];

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
              ...result.map((bucket: Bucket) => ({
                type: BucketsSliceTags.BUCKETS as const,
                id: getBucketCacheId(bucket),
              })),
              { type: BucketsSliceTags.BUCKETS, id: 'LIST' },
            ]
          : [{ type: BucketsSliceTags.BUCKETS, id: 'LIST' }],
    }),

    getBucketById: build.query<BucketResponse, string>({
      query: (id: string) => ({ url: buildApiUrl(`admin/buckets/${id}`), method: 'GET' }),
      transformResponse: (response: any): Bucket => normalizeBucket(response),
      providesTags: (_result: Bucket | undefined, _error: unknown, id: string) => getBucketTags(id),
    }),

    createBucket: build.mutation<BucketResponse, CreateBucketRequest>({
      async queryFn({ Name: bucketName }) {
        try {
          const response = await fetch(buildApiUrl('admin/buckets'), {
            method: 'POST',
            headers: buildAdminApiHeaders({
              'Content-Type': 'application/json',
            }),
            body: JSON.stringify({ Name: bucketName }),
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readAdminErrorMessage(response, 'Failed to create bucket'),
              },
            };
          }

          return {
            data: normalizeBucket({
              Name: bucketName,
              CreatedUtc: new Date().toISOString(),
            }),
          };
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
      async queryFn({ id }) {
        try {
          const response = await fetch(buildApiUrl(`admin/buckets/${id}?destroy=true`), {
            method: 'DELETE',
            headers: buildAdminApiHeaders(),
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readAdminErrorMessage(response, 'Failed to delete bucket'),
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
        { id }: DeleteBucketParams
      ) => getBucketTags(id),
    }),

    listBucketObjects: build.query<ListBucketResult, ListBucketObjectsParams>({
      async queryFn({ bucketId }) {
        try {
          const baseUrl = getBaseUrl();
          const url = `${baseUrl}/${bucketId}/`;
          const response = await fetchS3(url, {
            method: 'GET',
            cache: 'no-store',
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readS3ErrorMessage(response, 'Failed to fetch objects'),
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
      async queryFn({ bucketId, objectKey }) {
        try {
          const baseUrl = getBaseUrl();
          const url = `${baseUrl}/${bucketId}/${objectKey}`;
          const response = await fetchS3(url, {
            method: 'GET',
            cache: 'no-store',
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readS3ErrorMessage(response, 'Failed to download object'),
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
      async queryFn({ bucketId, objectKey, content, contentType }) {
        try {
          const baseUrl = getBaseUrl();
          const url = `${baseUrl}/${bucketId}/${objectKey}`;
          const response = await fetchS3(url, {
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
                data: await readS3ErrorMessage(response, 'Failed to write object'),
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

    uploadBucketObject: build.mutation<UploadBucketObjectResponse, UploadBucketObjectParams>({
      async queryFn({ bucketId, objectKey, file }) {
        try {
          const baseUrl = getBaseUrl();
          const url = `${baseUrl}/${bucketId}/${objectKey}`;
          const response = await fetchS3(url, {
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
                data: await readS3ErrorMessage(response, 'Failed to upload object'),
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
              data: error?.message || 'Failed to upload object',
            },
          };
        }
      },
    }),

    deleteBucketObject: build.mutation<DeleteBucketObjectResponse, DeleteBucketObjectParams>({
      async queryFn({ bucketId, objectKey }) {
        try {
          const baseUrl = getBaseUrl();
          const url = `${baseUrl}/${bucketId}/${objectKey}`;
          const response = await fetchS3(url, {
            method: 'DELETE',
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readS3ErrorMessage(response, 'Failed to delete object'),
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
      { bucketId: string; objectKeys: string[] }
    >({
      async queryFn({ bucketId, objectKeys }) {
        try {
          const baseUrl = getBaseUrl();

          // Build the XML body for S3 DeleteObjects API
          const objectsXml = objectKeys.map((key) => `<Object><Key>${key}</Key></Object>`).join('');
          const xmlBody = `<?xml version="1.0" encoding="UTF-8"?><Delete><Quiet>false</Quiet>${objectsXml}</Delete>`;

          const url = `${baseUrl}/${bucketId}?delete`;
          const response = await fetchS3(url, {
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
                data: await readS3ErrorMessage(response, 'Failed to delete objects'),
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
          const baseUrl = getBaseUrl();
          const xmlBody = generateBucketTaggingXml(tags);
          const url = `${baseUrl}/${bucketName}?tagging`;
          const response = await fetchS3(url, {
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
                data: await readS3ErrorMessage(response, 'Failed to write bucket tags'),
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
          const baseUrl = getBaseUrl();
          const url = `${baseUrl}/${bucketName}?tagging`;
          const response = await fetchS3(url, {
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
                data: await readS3ErrorMessage(response, 'Failed to get bucket tags'),
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
          const baseUrl = getBaseUrl();
          const url = `${baseUrl}/${bucketName}?tagging`;
          const response = await fetchS3(url, {
            method: 'DELETE',
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readS3ErrorMessage(response, 'Failed to delete bucket tags'),
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
      async queryFn({ bucketId, objectKey, tags }) {
        try {
          const baseUrl = getBaseUrl();
          const xmlBody = generateBucketTaggingXml(tags);
          const url = `${baseUrl}/${bucketId}/${objectKey}?tagging`;
          const response = await fetchS3(url, {
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
                data: await readS3ErrorMessage(response, 'Failed to write object tags'),
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
      async queryFn({ bucketId, objectKey }) {
        try {
          const baseUrl = getBaseUrl();
          const url = `${baseUrl}/${bucketId}/${objectKey}?tagging`;
          const response = await fetchS3(url, {
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
                data: await readS3ErrorMessage(response, 'Failed to get object tags'),
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
      async queryFn({ bucketId, objectKey }) {
        try {
          const baseUrl = getBaseUrl();
          const url = `${baseUrl}/${bucketId}/${objectKey}?tagging`;
          const response = await fetchS3(url, {
            method: 'DELETE',
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readS3ErrorMessage(response, 'Failed to delete object tags'),
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
          const baseUrl = getBaseUrl();
          const xmlBody = generateBucketACLXml(owner, grants);
          const url = `${baseUrl}/${bucketName}?acl`;
          const response = await fetchS3(url, {
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
                data: await readS3ErrorMessage(response, 'Failed to write bucket ACL'),
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
          const baseUrl = getBaseUrl();
          const url = `${baseUrl}/${bucketName}?acl`;
          const response = await fetchS3(url, {
            method: 'GET',
            cache: 'no-store',
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readS3ErrorMessage(response, 'Failed to get bucket ACL'),
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
      async queryFn({ bucketId, objectKey, owner, grants }) {
        try {
          const baseUrl = getBaseUrl();
          const xmlBody = generateBucketACLXml(owner, grants);
          const url = `${baseUrl}/${bucketId}/${objectKey}?acl`;
          const response = await fetchS3(url, {
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
                data: await readS3ErrorMessage(response, 'Failed to write object ACL'),
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
      async queryFn({ bucketId, objectKey }) {
        try {
          const baseUrl = getBaseUrl();
          const url = `${baseUrl}/${bucketId}/${objectKey}?acl`;
          const response = await fetchS3(url, {
            method: 'GET',
            cache: 'no-store',
          });

          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: await readS3ErrorMessage(response, 'Failed to get object ACL'),
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
