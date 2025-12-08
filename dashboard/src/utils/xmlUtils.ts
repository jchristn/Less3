import { xml2js } from 'xml-js';

/**
 * Options for XML to JSON conversion
 */
export interface XmlToJsonOptions {
  compact?: boolean;
  textKey?: string;
  ignoreAttributes?: boolean;
}

/**
 * Default options for XML to JSON conversion
 */
const DEFAULT_XML_OPTIONS: XmlToJsonOptions = {
  compact: true,
  textKey: '_text',
  ignoreAttributes: true,
};

/**
 * Converts XML string to JSON object
 * @param xmlString - The XML string to convert
 * @param options - Optional conversion options
 * @returns The converted JSON object
 */
export const xmlToJson = (xmlString: string, options?: XmlToJsonOptions): any => {
  const mergedOptions = { ...DEFAULT_XML_OPTIONS, ...options };
  return xml2js(xmlString, mergedOptions);
};

/**
 * Extracts text value from XML element (handles both compact and non-compact formats)
 * @param element - The XML element
 * @param textKey - The key used for text content (default: '_text')
 * @returns The text value or empty string
 */
export const extractXmlText = (element: any, textKey: string = '_text'): string => {
  if (!element) return '';
  if (typeof element === 'string') return element;
  if (element[textKey]) return element[textKey];
  if (typeof element === 'object' && !Array.isArray(element)) {
    // Try to find text in nested structure
    const keys = Object.keys(element);
    if (keys.length === 1 && typeof element[keys[0]] === 'string') {
      return element[keys[0]];
    }
  }
  return '';
};

/**
 * Interface for bucket object owner
 */
export interface BucketObjectOwner {
  ID: string;
  DisplayName: string;
}

/**
 * Interface for bucket object
 */
export interface BucketObject {
  Key: string;
  LastModified: string;
  ETag: string;
  ContentType: string;
  Size: number;
  StorageClass: string;
  Owner: BucketObjectOwner;
}

/**
 * Interface for ListBucketResult
 */
export interface ListBucketResult {
  Name: string;
  Prefix: string;
  KeyCount: number;
  MaxKeys: number;
  Delimiter: string;
  EncodingType: string;
  IsTruncated: boolean;
  Contents: BucketObject[];
}

/**
 * Parses a single Contents element from XML
 * @param item - The XML Contents element
 * @param textKey - The key used for text content (default: '_text')
 * @returns Parsed BucketObject
 */
const parseBucketObject = (item: any, textKey: string = '_text'): BucketObject => {
  const getValue = (field: string): string => {
    const element = item[field];
    return extractXmlText(element, textKey) || '';
  };

  const getNestedValue = (parent: string, child: string): string => {
    const parentElement = item[parent];
    if (!parentElement) return '';
    const childElement = parentElement[child];
    return extractXmlText(childElement, textKey) || '';
  };

  return {
    Key: getValue('Key'),
    LastModified: getValue('LastModified'),
    ETag: getValue('ETag'),
    ContentType: getValue('ContentType'),
    Size: parseInt(getValue('Size') || '0', 10),
    StorageClass: getValue('StorageClass'),
    Owner: {
      ID: getNestedValue('Owner', 'ID'),
      DisplayName: getNestedValue('Owner', 'DisplayName'),
    },
  };
};

/**
 * Parses ListBucketResult XML response to structured data
 * @param xmlString - The XML string from the API
 * @returns Parsed ListBucketResult object
 * @throws Error if the XML format is invalid
 */
export const parseListBucketResult = (xmlString: string): ListBucketResult => {
  const jsonResult = xmlToJson(xmlString);
  const listBucketResult = jsonResult.ListBucketResult;

  if (!listBucketResult) {
    throw new Error('Invalid response format: ListBucketResult not found');
  }

  const textKey = '_text';
  const getValue = (field: string): string => {
    return extractXmlText(listBucketResult[field], textKey) || '';
  };

  // Handle Contents - can be single object or array
  const contents = listBucketResult.Contents;
  let objects: BucketObject[] = [];

  if (contents) {
    if (Array.isArray(contents)) {
      objects = contents.map((item: any) => parseBucketObject(item, textKey));
    } else {
      // Single object
      objects = [parseBucketObject(contents, textKey)];
    }
  }

  return {
    Name: getValue('Name'),
    Prefix: getValue('Prefix'),
    KeyCount: parseInt(getValue('KeyCount') || '0', 10),
    MaxKeys: parseInt(getValue('MaxKeys') || '0', 10),
    Delimiter: getValue('Delimiter'),
    EncodingType: getValue('EncodingType'),
    IsTruncated: getValue('IsTruncated') === 'true',
    Contents: objects,
  };
};

/**
 * Interface for bucket owner
 */
export interface BucketOwner {
  ID: string;
  DisplayName: string;
}

/**
 * Interface for a bucket in ListAllMyBucketsResult
 */
export interface BucketItem {
  Name: string;
  CreationDate: string;
}

/**
 * Interface for ListAllMyBucketsResult
 */
export interface ListAllMyBucketsResult {
  Owner: BucketOwner;
  Buckets: BucketItem[];
}

/**
 * Parses a single Bucket element from XML
 * @param item - The XML Bucket element
 * @param textKey - The key used for text content (default: '_text')
 * @returns Parsed BucketItem
 */
const parseBucketItem = (item: any, textKey: string = '_text'): BucketItem => {
  const getValue = (field: string): string => {
    const element = item[field];
    return extractXmlText(element, textKey) || '';
  };

  return {
    Name: getValue('Name'),
    CreationDate: getValue('CreationDate'),
  };
};

/**
 * Parses ListAllMyBucketsResult XML response to structured data
 * @param xmlString - The XML string from the API
 * @returns Parsed ListAllMyBucketsResult object
 * @throws Error if the XML format is invalid
 */
export const parseListAllMyBucketsResult = (xmlString: string): ListAllMyBucketsResult => {
  const jsonResult = xmlToJson(xmlString);
  const listAllMyBucketsResult = jsonResult.ListAllMyBucketsResult;

  if (!listAllMyBucketsResult) {
    throw new Error('Invalid response format: ListAllMyBucketsResult not found');
  }

  const textKey = '_text';

  // Parse Owner
  const ownerElement = listAllMyBucketsResult.Owner;
  const owner: BucketOwner = {
    ID: extractXmlText(ownerElement?.ID, textKey) || '',
    DisplayName: extractXmlText(ownerElement?.DisplayName, textKey) || '',
  };

  // Parse Buckets - can be single object or array
  const bucketsElement = listAllMyBucketsResult.Buckets;
  let buckets: BucketItem[] = [];

  if (bucketsElement) {
    const bucketItems = bucketsElement.Bucket;
    if (bucketItems) {
      if (Array.isArray(bucketItems)) {
        buckets = bucketItems.map((item: any) => parseBucketItem(item, textKey));
      } else {
        // Single bucket
        buckets = [parseBucketItem(bucketItems, textKey)];
      }
    }
  }

  return {
    Owner: owner,
    Buckets: buckets,
  };
};

/**
 * Interface for bucket tag
 */
export interface BucketTag {
  Key: string;
  Value: string;
}

/**
 * Parses Tagging XML response to structured data
 * @param xmlString - The XML string from the API
 * @returns Parsed array of BucketTag objects
 * @throws Error if the XML format is invalid
 */
export const parseBucketTagging = (xmlString: string): BucketTag[] => {
  const jsonResult = xmlToJson(xmlString);
  const tagging = jsonResult.Tagging;

  if (!tagging) {
    throw new Error('Invalid response format: Tagging not found');
  }

  const textKey = '_text';
  const tagSet = tagging.TagSet;

  if (!tagSet) {
    return [];
  }

  const tagElement = tagSet.Tag;
  if (!tagElement) {
    return [];
  }

  // Handle single tag or array of tags
  const tags: BucketTag[] = [];
  if (Array.isArray(tagElement)) {
    tags.push(
      ...tagElement.map((tag: any) => ({
        Key: extractXmlText(tag.Key, textKey) || '',
        Value: extractXmlText(tag.Value, textKey) || '',
      }))
    );
  } else {
    tags.push({
      Key: extractXmlText(tagElement.Key, textKey) || '',
      Value: extractXmlText(tagElement.Value, textKey) || '',
    });
  }

  return tags;
};

/**
 * Generates XML string for bucket tagging
 * @param tags - Array of tags to write
 * @returns XML string for the tagging request
 */
export const generateBucketTaggingXml = (tags: BucketTag[]): string => {
  const tagElements = tags
    .map(
      (tag) => `        <Tag>
            <Key>${escapeXml(tag.Key)}</Key>
            <Value>${escapeXml(tag.Value)}</Value>
        </Tag>`
    )
    .join('\n');

  return `<?xml version="1.0" encoding="utf-8"?>
<Tagging xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
    <TagSet>
${tagElements}
    </TagSet>
</Tagging>`;
};

/**
 * Escapes XML special characters
 * @param text - Text to escape
 * @returns Escaped text
 */
const escapeXml = (text: string): string => {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');
};

/**
 * Interface for ACL Owner
 */
export interface ACLOwner {
  ID: string;
  DisplayName: string;
}

/**
 * Interface for ACL Grantee
 */
export interface ACLGrantee {
  ID: string;
  DisplayName: string;
  Type: string;
  URI?: string | null;
  EmailAddress?: string | null;
}

/**
 * Interface for ACL Grant
 */
export interface ACLGrant {
  Grantee: ACLGrantee;
  Permission: string;
}

/**
 * Interface for Bucket ACL
 */
export interface BucketACL {
  Owner: ACLOwner;
  AccessControlList: {
    Grant: ACLGrant | ACLGrant[];
  };
}

/**
 * Parses AccessControlPolicy XML response to structured data
 * @param xmlString - The XML string from the API
 * @returns Parsed BucketACL object
 * @throws Error if the XML format is invalid
 */
export const parseBucketACL = (xmlString: string): BucketACL => {
  const jsonResult = xmlToJson(xmlString);
  const aclPolicy = jsonResult.AccessControlPolicy;

  if (!aclPolicy) {
    throw new Error('Invalid response format: AccessControlPolicy not found');
  }

  const textKey = '_text';

  // Parse Owner
  const ownerElement = aclPolicy.Owner;
  const owner: ACLOwner = {
    ID: extractXmlText(ownerElement?.ID, textKey) || '',
    DisplayName: extractXmlText(ownerElement?.DisplayName, textKey) || '',
  };

  // Parse AccessControlList
  const aclElement = aclPolicy.AccessControlList;
  if (!aclElement) {
    throw new Error('Invalid response format: AccessControlList not found');
  }

  const grantElement = aclElement.Grant;
  if (!grantElement) {
    throw new Error('Invalid response format: Grant not found');
  }

  // Handle single grant or array of grants
  const grants: ACLGrant[] = [];
  if (Array.isArray(grantElement)) {
    grants.push(
      ...grantElement.map((grant: any) => {
        const grantee = grant.Grantee;
        return {
          Grantee: {
            ID: extractXmlText(grantee?.ID, textKey) || '',
            DisplayName: extractXmlText(grantee?.DisplayName, textKey) || '',
            Type: extractXmlText(grantee?.Type, textKey) || 'CanonicalUser',
            URI: grantee?.URI?.[textKey] || null,
            EmailAddress: grantee?.EmailAddress?.[textKey] || null,
          },
          Permission: extractXmlText(grant.Permission, textKey) || '',
        };
      })
    );
  } else {
    const grantee = grantElement.Grantee;
    grants.push({
      Grantee: {
        ID: extractXmlText(grantee?.ID, textKey) || '',
        DisplayName: extractXmlText(grantee?.DisplayName, textKey) || '',
        Type: extractXmlText(grantee?.Type, textKey) || 'CanonicalUser',
        URI: grantee?.URI?.[textKey] || null,
        EmailAddress: grantee?.EmailAddress?.[textKey] || null,
      },
      Permission: extractXmlText(grantElement.Permission, textKey) || '',
    });
  }

  return {
    Owner: owner,
    AccessControlList: {
      Grant: grants.length === 1 ? grants[0] : grants,
    },
  };
};

/**
 * Generates XML string for bucket ACL
 * @param owner - Owner information
 * @param grants - Array of grants
 * @returns XML string for the ACL request
 */
export const generateBucketACLXml = (owner: ACLOwner, grants: ACLGrant[]): string => {
  const ownerXml = `  <Owner>
    <ID>${escapeXml(owner.ID)}</ID>
    <DisplayName>${escapeXml(owner.DisplayName)}</DisplayName>
  </Owner>`;

  const grantElements = grants
    .map(
      (grant) => `    <Grant>
      <Grantee xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:type="CanonicalUser">
        <ID>${escapeXml(grant.Grantee.ID)}</ID>
        <DisplayName>${escapeXml(grant.Grantee.DisplayName)}</DisplayName>
      </Grantee>
      <Permission>${escapeXml(grant.Permission)}</Permission>
    </Grant>`
    )
    .join('\n');

  return `<AccessControlPolicy xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns="http://s3.amazonaws.com/doc/2006-03-01/">
${ownerXml}
  <AccessControlList>
${grantElements}
  </AccessControlList>
</AccessControlPolicy>`;
};
