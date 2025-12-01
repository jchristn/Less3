// User API types and interfaces

export interface User {
  GUID: string;
  Name: string;
  Email: string;
  CreatedUtc: string;
  [key: string]: any;
}

export type UserListResponse = User[];
export type UserResponse = User;

export interface CreateUserRequest {
  GUID?: string;
  Name: string;
  Email: string;
  [key: string]: any;
}

export interface DeleteUserParams {
  guid: string;
}

export interface DeleteUserResponse {
  success: boolean;
}

export interface GetUsersParams {
  search?: string;
}
