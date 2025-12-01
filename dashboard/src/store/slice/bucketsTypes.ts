// Bucket API types and interfaces

export interface Bucket {
  GUID: string;
  OwnerGUID: string;
  Name: string;
  RegionString?: string;
  StorageType: string;
  DiskDirectory: string;
  EnableVersioning: boolean;
  EnablePublicWrite: boolean;
  EnablePublicRead: boolean;
  CreatedUtc: string;
  [key: string]: any;
}

export type BucketListResponse = Bucket[];
export type BucketResponse = Bucket;

export interface CreateBucketRequest {
  GUID?: string;
  OwnerGUID?: string;
  Name: string;
  StorageType?: string;
  DiskDirectory?: string;
  EnableVersioning?: boolean;
  EnablePublicWrite?: boolean;
  EnablePublicRead?: boolean;
  CreatedUtc?: string;
  [key: string]: any;
}

export interface UpdateBucketRequest {
  GUID: string;
  Name?: string;
  StorageType?: string;
  DiskDirectory?: string;
  EnableVersioning?: boolean;
  EnablePublicWrite?: boolean;
  EnablePublicRead?: boolean;
  [key: string]: any;
}

export interface DeleteBucketParams {
  guid: string;
  destroy?: boolean;
}

export interface DeleteBucketResponse {
  success: boolean;
}

export interface GetBucketsParams {
  search?: string;
}
