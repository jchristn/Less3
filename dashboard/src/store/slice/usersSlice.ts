import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import type {
  User,
  UserListResponse,
  UserResponse,
  CreateUserRequest,
  UpdateUserRequest,
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
  UpdateUserRequest,
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

const getUserTags = (id: string) => [
  { type: UsersSliceTags.USERS as const, id: id },
  { type: UsersSliceTags.USERS, id: 'LIST' },
];

const usersSliceInstance = enhancedSdk.injectEndpoints({
  overrideExisting: true,
  endpoints: (build: EndpointBuilder<BaseQueryFn<ApiBaseQueryArgs, unknown, unknown>, UsersSliceTags, 'sdk'>) => ({
    getUsers: build.query<UserListResponse, void>({
      query: () => ({
        url: 'admin/users',
        method: 'GET',
      }),
      transformResponse: (response: any): User[] => (Array.isArray(response) ? response : []),
      providesTags: (result: User[] | undefined) =>
        result
          ? [
              ...result.map(({ Id }: User) => ({ type: UsersSliceTags.USERS as const, id: Id })),
              { type: UsersSliceTags.USERS, id: 'LIST' },
            ]
          : [{ type: UsersSliceTags.USERS, id: 'LIST' }],
    }),

    getUserById: build.query<UserResponse, string>({
      query: (id: string) => ({ url: `admin/users/${id}`, method: 'GET' }),
      transformResponse: (response: any): User => response,
      providesTags: (_result: User | undefined, _error: unknown, id: string) => getUserTags(id),
    }),

    createUser: build.mutation<UserResponse, CreateUserRequest>({
      query: (body: CreateUserRequest) => ({ url: 'admin/users', method: 'POST', body }),
      transformResponse: (response: any): User => response,
      invalidatesTags: [{ type: UsersSliceTags.USERS, id: 'LIST' }],
    }),

    updateUser: build.mutation<UserResponse, UpdateUserRequest>({
      query: ({ Id, ...body }: UpdateUserRequest) => ({
        url: `admin/users/${Id}`,
        method: 'PUT',
        body: {
          Id,
          ...body,
        },
      }),
      transformResponse: (response: any): User => response,
      invalidatesTags: (_result: User | undefined, _error: unknown, { Id }: UpdateUserRequest) =>
        getUserTags(Id),
    }),

    deleteUser: build.mutation<DeleteUserResponse, DeleteUserParams>({
      query: ({ id }: DeleteUserParams) => ({
        url: `admin/users/${id}`,
        method: 'DELETE',
      }),
      transformResponse: (): DeleteUserResponse => ({ success: true }),
      invalidatesTags: (_result: DeleteUserResponse | undefined, _error: unknown, { id }: DeleteUserParams) =>
        getUserTags(id),
    }),
  }),
});

export const {
  useGetUsersQuery,
  useGetUserByIdQuery,
  useCreateUserMutation,
  useUpdateUserMutation,
  useDeleteUserMutation,
} =
  usersSliceInstance;
