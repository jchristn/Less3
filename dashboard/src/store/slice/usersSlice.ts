import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import { buildApiUrl } from '#/services/sdk.service';
import type {
  User,
  UserListResponse,
  UserResponse,
  CreateUserRequest,
  DeleteUserParams,
  DeleteUserResponse,
  GetUsersParams,
} from './usersTypes';

export enum UsersSliceTags {
  USERS = 'USERS',
}

// Re-export types for convenience
export type {
  User,
  UserListResponse,
  UserResponse,
  CreateUserRequest,
  DeleteUserParams,
  DeleteUserResponse,
  GetUsersParams,
};

const enhancedSdk = sdkSlice.enhanceEndpoints({
  addTagTypes: [UsersSliceTags.USERS],
});

// Helper functions
const buildQueryString = (params: GetUsersParams): string => {
  const queryParams = new URLSearchParams();
  if (params.search) queryParams.append('search', params.search);
  return queryParams.toString();
};

const getUserTags = (guid: string) => [
  { type: UsersSliceTags.USERS as const, id: guid },
  { type: UsersSliceTags.USERS, id: 'LIST' },
];

const usersSliceInstance = enhancedSdk.injectEndpoints({
  endpoints: (build: EndpointBuilder<BaseQueryFn<ApiBaseQueryArgs, unknown, unknown>, UsersSliceTags, 'sdk'>) => ({
    getUsers: build.query<UserListResponse, void>({
      query: () => ({
        url: buildApiUrl('admin/users'),
        method: 'GET',
      }),
      transformResponse: (response: any): User[] => (Array.isArray(response) ? response : []),
      providesTags: (result: User[] | undefined) =>
        result
          ? [
              ...result.map(({ GUID }: User) => ({ type: UsersSliceTags.USERS as const, id: GUID })),
              { type: UsersSliceTags.USERS, id: 'LIST' },
            ]
          : [{ type: UsersSliceTags.USERS, id: 'LIST' }],
    }),

    getUserById: build.query<UserResponse, string>({
      query: (guid: string) => ({ url: buildApiUrl(`admin/users/${guid}`), method: 'GET' }),
      transformResponse: (response: any): User => response,
      providesTags: (_result: User | undefined, _error: unknown, guid: string) => getUserTags(guid),
    }),

    createUser: build.mutation<UserResponse, CreateUserRequest>({
      query: (body: CreateUserRequest) => ({ url: buildApiUrl('admin/users'), method: 'POST', body }),
      transformResponse: (response: any): User => response,
      invalidatesTags: [{ type: UsersSliceTags.USERS, id: 'LIST' }],
    }),

    deleteUser: build.mutation<DeleteUserResponse, DeleteUserParams>({
      query: ({ guid }: DeleteUserParams) => ({
        url: buildApiUrl(`admin/users/${guid}`),
        method: 'DELETE',
      }),
      transformResponse: (): DeleteUserResponse => ({ success: true }),
      invalidatesTags: (_result: DeleteUserResponse | undefined, _error: unknown, { guid }: DeleteUserParams) =>
        getUserTags(guid),
    }),
  }),
});

export const { useGetUsersQuery, useGetUserByIdQuery, useCreateUserMutation, useDeleteUserMutation } =
  usersSliceInstance;
