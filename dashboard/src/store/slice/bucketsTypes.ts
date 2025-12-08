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

export interface DeleteBucketObjectParams {
  bucketGUID: string;
  objectKey: string;
}

export interface DeleteBucketObjectResponse {
  success: boolean;
}

export interface BucketTag {
  Key: string;
  Value: string;
}

export interface BucketTagging {
  TagSet: {
    Tag: BucketTag | BucketTag[];
  };
}

export interface WriteBucketTagsParams {
  bucketName: string;
  tags: BucketTag[];
}

export interface WriteBucketTagsResponse {
  success: boolean;
}

export interface GetBucketTagsParams {
  bucketName: string;
}

export interface GetBucketTagsResponse {
  tags: BucketTag[];
}

export interface DeleteBucketTagsParams {
  bucketName: string;
}

export interface DeleteBucketTagsResponse {
  success: boolean;
}

export interface WriteObjectTagsParams {
  bucketGUID: string;
  objectKey: string;
  tags: BucketTag[];
}

export interface WriteObjectTagsResponse {
  success: boolean;
}

export interface GetObjectTagsParams {
  bucketGUID: string;
  objectKey: string;
}

export interface GetObjectTagsResponse {
  tags: BucketTag[];
}

export interface DeleteObjectTagsParams {
  bucketGUID: string;
  objectKey: string;
}

export interface DeleteObjectTagsResponse {
  success: boolean;
}

export interface ACLOwner {
  ID: string;
  DisplayName: string;
}

export interface ACLGrantee {
  ID: string;
  DisplayName: string;
  Type: string;
  URI?: string | null;
  EmailAddress?: string | null;
}

export interface ACLGrant {
  Grantee: ACLGrantee;
  Permission: string;
}

export interface BucketACL {
  Owner: ACLOwner;
  AccessControlList: {
    Grant: ACLGrant | ACLGrant[];
  };
}

export interface WriteBucketACLParams {
  bucketName: string;
  owner: ACLOwner;
  grants: ACLGrant[];
}

export interface WriteBucketACLResponse {
  success: boolean;
}

export interface GetBucketACLParams {
  bucketName: string;
}

export interface GetBucketACLResponse {
  acl: BucketACL;
}

export interface WriteObjectACLParams {
  bucketGUID: string;
  objectKey: string;
  owner: ACLOwner;
  grants: ACLGrant[];
}

export interface WriteObjectACLResponse {
  success: boolean;
}

export interface GetObjectACLParams {
  bucketGUID: string;
  objectKey: string;
}

export interface GetObjectACLResponse {
  acl: BucketACL;
}
