// Bucket API types and interfaces

export interface Bucket {
  Name: string;
  CreationDate: string;
  [key: string]: any;
}

export type BucketListResponse = Bucket[];
export type BucketResponse = Bucket;

export interface CreateBucketRequest {
  Name: string;
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
  bucketName: string;
}

export interface DeleteBucketResponse {
  success: boolean;
}

export interface GetBucketsParams {
  search?: string;
}

export interface ListBucketObjectsParams {
  bucketGUID: string;
}

export interface DownloadBucketObjectParams {
  bucketGUID: string;
  objectKey: string;
}

export interface DownloadBucketObjectResponse {
  content: string;
  contentType: string;
}

export interface WriteBucketObjectParams {
  bucketGUID: string;
  objectKey: string;
  content: string;
}

export interface WriteBucketObjectResponse {
  success: boolean;
}
