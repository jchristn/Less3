// User API types and interfaces

export interface User {
  Id: string;
  Name: string;
  Email: string;
  CreatedUtc: string;
  [key: string]: any;
}

export type UserListResponse = User[];
export type UserResponse = User;

export interface CreateUserRequest {
  Id?: string;
  Name: string;
  Email: string;
  [key: string]: any;
}

export interface UpdateUserRequest {
  Id: string;
  Name: string;
  Email: string;
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
