// Credential API types and interfaces

export interface Credential {
  GUID: string;
  UserGUID: string;
  Description: string;
  AccessKey: string;
  SecretKey: string;
  IsBase64: boolean;
  CreatedUtc: string;
  [key: string]: any;
}

export type CredentialListResponse = Credential[];
export type CredentialResponse = Credential;

export interface CreateCredentialRequest {
  GUID?: string;
  UserGUID: string;
  Description: string;
  AccessKey: string;
  SecretKey: string;
  [key: string]: any;
}

export interface DeleteCredentialParams {
  guid: string;
}

export interface DeleteCredentialResponse {
  success: boolean;
}

export interface GetCredentialsParams {
  search?: string;
}
