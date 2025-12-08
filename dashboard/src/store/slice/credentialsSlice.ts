import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import { buildApiUrl } from '#/services/sdk.service';
import type {
  Credential,
  CredentialListResponse,
  CredentialResponse,
  CreateCredentialRequest,
  DeleteCredentialParams,
  DeleteCredentialResponse,
  GetCredentialsParams,
} from './credentialsTypes';

export enum CredentialsSliceTags {
  CREDENTIALS = 'CREDENTIALS',
}

// Re-export types for convenience
export type {
  Credential,
  CredentialListResponse,
  CredentialResponse,
  CreateCredentialRequest,
  DeleteCredentialParams,
  DeleteCredentialResponse,
  GetCredentialsParams,
};

const enhancedSdk = sdkSlice.enhanceEndpoints({
  addTagTypes: [CredentialsSliceTags.CREDENTIALS],
});

// Helper functions
const buildQueryString = (params: GetCredentialsParams): string => {
  const queryParams = new URLSearchParams();
  if (params.search) queryParams.append('search', params.search);
  return queryParams.toString();
};

const getCredentialTags = (guid: string) => [
  { type: CredentialsSliceTags.CREDENTIALS as const, id: guid },
  { type: CredentialsSliceTags.CREDENTIALS, id: 'LIST' },
];

const credentialsSliceInstance = enhancedSdk.injectEndpoints({
  endpoints: (
    build: EndpointBuilder<BaseQueryFn<ApiBaseQueryArgs, unknown, unknown>, CredentialsSliceTags, 'sdk'>
  ) => ({
    getCredentials: build.query<CredentialListResponse, void>({
      query: () => ({
        url: buildApiUrl('admin/credentials'),
        method: 'GET',
      }),
      transformResponse: (response: any): Credential[] => (Array.isArray(response) ? response : []),
      providesTags: (result: Credential[] | undefined) =>
        result
          ? [
              ...result.map(({ GUID }: Credential) => ({ type: CredentialsSliceTags.CREDENTIALS as const, id: GUID })),
              { type: CredentialsSliceTags.CREDENTIALS, id: 'LIST' },
            ]
          : [{ type: CredentialsSliceTags.CREDENTIALS, id: 'LIST' }],
    }),

    getCredentialById: build.query<CredentialResponse, string>({
      query: (guid: string) => ({ url: buildApiUrl(`admin/credentials/${guid}`), method: 'GET' }),
      transformResponse: (response: any): Credential => response,
      providesTags: (_result: Credential | undefined, _error: unknown, guid: string) => getCredentialTags(guid),
    }),

    createCredential: build.mutation<CredentialResponse, CreateCredentialRequest>({
      query: (body: CreateCredentialRequest) => ({ url: buildApiUrl('admin/credentials'), method: 'POST', body }),
      transformResponse: (response: any): Credential => response,
      invalidatesTags: [{ type: CredentialsSliceTags.CREDENTIALS, id: 'LIST' }],
    }),

    deleteCredential: build.mutation<DeleteCredentialResponse, DeleteCredentialParams>({
      query: ({ guid }: DeleteCredentialParams) => ({
        url: buildApiUrl(`admin/credentials/${guid}`),
        method: 'DELETE',
      }),
      transformResponse: (): DeleteCredentialResponse => ({ success: true }),
      invalidatesTags: (
        _result: DeleteCredentialResponse | undefined,
        _error: unknown,
        { guid }: DeleteCredentialParams
      ) => getCredentialTags(guid),
    }),
  }),
});

export const {
  useGetCredentialsQuery,
  useGetCredentialByIdQuery,
  useCreateCredentialMutation,
  useDeleteCredentialMutation,
} = credentialsSliceInstance;
