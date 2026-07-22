import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import { buildApiUrl } from '#/services/sdk.service';
import type {
  Credential,
  CredentialListResponse,
  CredentialResponse,
  CreateCredentialRequest,
  UpdateCredentialRequest,
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
  UpdateCredentialRequest,
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

const getCredentialTags = (id: string) => [
  { type: CredentialsSliceTags.CREDENTIALS as const, id: id },
  { type: CredentialsSliceTags.CREDENTIALS, id: 'LIST' },
];

const credentialsSliceInstance = enhancedSdk.injectEndpoints({
  overrideExisting: true,
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
              ...result.map(({ Id }: Credential) => ({ type: CredentialsSliceTags.CREDENTIALS as const, id: Id })),
              { type: CredentialsSliceTags.CREDENTIALS, id: 'LIST' },
            ]
          : [{ type: CredentialsSliceTags.CREDENTIALS, id: 'LIST' }],
    }),

    getCredentialById: build.query<CredentialResponse, string>({
      query: (id: string) => ({ url: buildApiUrl(`admin/credentials/${id}`), method: 'GET' }),
      transformResponse: (response: any): Credential => response,
      providesTags: (_result: Credential | undefined, _error: unknown, id: string) => getCredentialTags(id),
    }),

    createCredential: build.mutation<CredentialResponse, CreateCredentialRequest>({
      query: (body: CreateCredentialRequest) => ({ url: buildApiUrl('admin/credentials'), method: 'POST', body }),
      transformResponse: (response: any): Credential => response,
      invalidatesTags: [{ type: CredentialsSliceTags.CREDENTIALS, id: 'LIST' }],
    }),

    updateCredential: build.mutation<CredentialResponse, UpdateCredentialRequest>({
      query: ({ Id, ...body }: UpdateCredentialRequest) => ({
        url: buildApiUrl(`admin/credentials/${Id}`),
        method: 'PUT',
        body: {
          Id,
          ...body,
        },
      }),
      transformResponse: (response: any): Credential => response,
      invalidatesTags: (_result: Credential | undefined, _error: unknown, { Id }: UpdateCredentialRequest) =>
        getCredentialTags(Id),
    }),

    deleteCredential: build.mutation<DeleteCredentialResponse, DeleteCredentialParams>({
      query: ({ id }: DeleteCredentialParams) => ({
        url: buildApiUrl(`admin/credentials/${id}`),
        method: 'DELETE',
      }),
      transformResponse: (): DeleteCredentialResponse => ({ success: true }),
      invalidatesTags: (
        _result: DeleteCredentialResponse | undefined,
        _error: unknown,
        { id }: DeleteCredentialParams
      ) => getCredentialTags(id),
    }),
  }),
});

export const {
  useGetCredentialsQuery,
  useGetCredentialByIdQuery,
  useCreateCredentialMutation,
  useUpdateCredentialMutation,
  useDeleteCredentialMutation,
} = credentialsSliceInstance;
