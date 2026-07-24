// User API types and interfaces

export interface User {
  Id: string;
  TenantId?: string;
  Name: string;
  Email: string;
  Active?: boolean;
  IsAdmin?: boolean;
  IsTenantAdmin?: boolean;
  CreatedUtc: string;
  [key: string]: any;
}

export type UserListResponse = User[];
export type UserResponse = User;

export interface CreateUserRequest {
  Id?: string;
  TenantId?: string;
  Name: string;
  Email: string;
  PasswordHash?: string;
  Active?: boolean;
  IsAdmin?: boolean;
  IsTenantAdmin?: boolean;
  [key: string]: any;
}

export interface UpdateUserRequest {
  Id: string;
  TenantId?: string;
  Name: string;
  Email: string;
  PasswordHash?: string;
  Active?: boolean;
  IsAdmin?: boolean;
  IsTenantAdmin?: boolean;
  [key: string]: any;
}

export interface DeleteUserParams {
  id: string;
}

export interface DeleteUserResponse {
  success: boolean;
}

export interface GetUsersParams {
  search?: string;
}
