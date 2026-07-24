// Credential API types and interfaces

export interface Credential {
  Id: string;
  UserId: string;
  Description: string;
  AccessKey: string;
  SecretKey?: string | null;
  IsBase64: boolean;
  Active?: boolean;
  LastUsedUtc?: string | null;
  LastFailedUtc?: string | null;
  CreatedUtc: string;
  [key: string]: any;
}

export type CredentialListResponse = Credential[];
export type CredentialResponse = Credential;

export interface CreateCredentialRequest {
  Id?: string;
  UserId: string;
  Description: string;
  AccessKey?: string;
  SecretKey?: string;
  [key: string]: any;
}

export interface UpdateCredentialRequest {
  Id: string;
  UserId: string;
  Description: string;
  AccessKey: string;
  SecretKey?: string;
  Active?: boolean;
  IsBase64?: boolean;
  [key: string]: any;
}

export interface CredentialActionParams {
  id: string;
}

export interface DeleteCredentialParams {
  id: string;
}

export interface DeleteCredentialResponse {
  success: boolean;
}

export interface GetCredentialsParams {
  search?: string;
}
