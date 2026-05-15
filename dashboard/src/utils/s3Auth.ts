'use client';

export interface S3CredentialLike {
  GUID?: string;
  UserGUID?: string;
  AccessKey: string;
  SecretKey?: string | null;
  Description?: string;
}

interface BuildSignedS3HeadersOptions {
  method: string;
  url: string;
  accessKey: string;
  secretKey: string;
  headers?: Record<string, string | undefined>;
  body?: BodyInit | null;
  region?: string;
  service?: string;
  timestamp?: Date;
}

export const DEFAULT_S3_REGION = 'us-west-1';
export const S3_PREFERRED_CREDENTIAL_STORAGE_KEY = 'less3S3CredentialGuid';

const encoder = new TextEncoder();

const encodeRfc3986 = (value: string): string =>
  encodeURIComponent(value).replace(/[!'()*]/g, (character) =>
    `%${character.charCodeAt(0).toString(16).toUpperCase()}`
  );

const bytesToHex = (bytes: Uint8Array): string =>
  Array.from(bytes)
    .map((byte) => byte.toString(16).padStart(2, '0'))
    .join('');

const toCryptoArrayBuffer = (bytes: Uint8Array): ArrayBuffer => {
  if (bytes.buffer instanceof ArrayBuffer) {
    return bytes.byteOffset === 0 && bytes.byteLength === bytes.buffer.byteLength
      ? bytes.buffer
      : bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength);
  }

  return Uint8Array.from(bytes).buffer;
};

const normalizeHeaderValue = (value: string): string => value.replace(/\s+/g, ' ').trim();

const dateToAmzDate = (date: Date): string =>
  date.toISOString().replace(/[:-]|\.\d{3}/g, '');

const dateToStamp = (date: Date): string => dateToAmzDate(date).slice(0, 8);

const shouldSignHeader = (headerName: string): boolean => {
  const normalized = headerName.toLowerCase();
  return normalized === 'range' || normalized.startsWith('x-amz-');
};

const buildCanonicalUri = (url: URL): string => {
  const pathname = url.pathname || '/';
  return pathname
    .split('/')
    .map((segment) => encodeRfc3986(decodeURIComponent(segment)))
    .join('/');
};

const buildCanonicalQueryString = (url: URL): string => {
  const pairs: Array<[string, string]> = [];

  url.searchParams.forEach((value, key) => {
    pairs.push([encodeRfc3986(key), encodeRfc3986(value)]);
  });

  pairs.sort((left, right) => {
    if (left[0] === right[0]) {
      return left[1].localeCompare(right[1]);
    }

    return left[0].localeCompare(right[0]);
  });

  return pairs.map(([key, value]) => `${key}=${value}`).join('&');
};

const toArrayBuffer = async (body?: BodyInit | null): Promise<ArrayBuffer> => {
  if (body == null) {
    return encoder.encode('').buffer;
  }

  if (typeof body === 'string') {
    return encoder.encode(body).buffer;
  }

  if (body instanceof Blob) {
    return body.arrayBuffer();
  }

  if (body instanceof URLSearchParams) {
    return encoder.encode(body.toString()).buffer;
  }

  if (body instanceof ArrayBuffer) {
    return body;
  }

  if (ArrayBuffer.isView(body)) {
    return body.buffer.slice(body.byteOffset, body.byteOffset + body.byteLength) as ArrayBuffer;
  }

  return encoder.encode(String(body)).buffer;
};

const sha256Hex = async (input: string | ArrayBuffer): Promise<string> => {
  const data = typeof input === 'string' ? encoder.encode(input) : input;
  const digest = await globalThis.crypto.subtle.digest('SHA-256', data);
  return bytesToHex(new Uint8Array(digest));
};

const importHmacKey = async (key: string | Uint8Array): Promise<CryptoKey> => {
  const rawKey = typeof key === 'string' ? encoder.encode(key) : key;

  return globalThis.crypto.subtle.importKey(
    'raw',
    toCryptoArrayBuffer(rawKey),
    {
      name: 'HMAC',
      hash: 'SHA-256',
    },
    false,
    ['sign']
  );
};

const hmacSha256 = async (key: string | Uint8Array, data: string): Promise<Uint8Array> => {
  const cryptoKey = await importHmacKey(key);
  const signature = await globalThis.crypto.subtle.sign('HMAC', cryptoKey, encoder.encode(data));
  return new Uint8Array(signature);
};

export const getPreferredS3CredentialGuid = (): string | null => {
  if (typeof window === 'undefined') {
    return null;
  }

  try {
    const storedGuid = window.localStorage.getItem(S3_PREFERRED_CREDENTIAL_STORAGE_KEY)?.trim();
    return storedGuid || null;
  } catch {
    return null;
  }
};

export const setPreferredS3CredentialGuid = (guid: string): void => {
  if (typeof window === 'undefined') {
    return;
  }

  try {
    window.localStorage.setItem(S3_PREFERRED_CREDENTIAL_STORAGE_KEY, guid);
  } catch {
    // Ignore storage errors and continue without persistence.
  }
};

export const clearPreferredS3CredentialGuid = (): void => {
  if (typeof window === 'undefined') {
    return;
  }

  try {
    window.localStorage.removeItem(S3_PREFERRED_CREDENTIAL_STORAGE_KEY);
  } catch {
    // Ignore storage errors and continue without persistence.
  }
};

export const selectS3Credential = (
  credentials: S3CredentialLike[],
  preferredUserGuid?: string
): S3CredentialLike | null => {
  const validCredentials = credentials.filter((credential) => credential.AccessKey?.trim());

  if (validCredentials.length === 0) {
    return null;
  }

  const preferredGuid = getPreferredS3CredentialGuid();
  if (preferredGuid) {
    const preferredCredential = validCredentials.find((credential) => credential.GUID === preferredGuid);
    if (preferredCredential) {
      return preferredCredential;
    }
  }

  if (preferredUserGuid) {
    const ownerCredential = validCredentials.find((credential) => credential.UserGUID === preferredUserGuid);
    if (ownerCredential) {
      return ownerCredential;
    }
  }

  const defaultCredential = validCredentials.find((credential) => credential.AccessKey === 'default');
  if (defaultCredential) {
    return defaultCredential;
  }

  return validCredentials[0];
};

export const buildSignedS3Headers = async ({
  method,
  url,
  accessKey,
  secretKey,
  headers = {},
  body,
  region = DEFAULT_S3_REGION,
  service = 's3',
  timestamp = new Date(),
}: BuildSignedS3HeadersOptions): Promise<Record<string, string>> => {
  const requestUrl = new URL(url);
  const requestBody = await toArrayBuffer(body);
  const payloadHash = await sha256Hex(requestBody);
  const amzDate = dateToAmzDate(timestamp);
  const dateStamp = dateToStamp(timestamp);
  const scope = `${dateStamp}/${region}/${service}/aws4_request`;

  const unsignedHeaders = Object.entries(headers).reduce<Record<string, string>>((accumulator, [key, value]) => {
    if (typeof value === 'string' && value.length > 0) {
      accumulator[key] = value;
    }

    return accumulator;
  }, {});

  const canonicalHeadersMap: Record<string, string> = {
    host: requestUrl.host,
    'x-amz-content-sha256': payloadHash,
    'x-amz-date': amzDate,
  };

  Object.entries(unsignedHeaders).forEach(([key, value]) => {
    const normalizedKey = key.toLowerCase();

    if (normalizedKey === 'authorization' || normalizedKey === 'host') {
      return;
    }

    if (shouldSignHeader(normalizedKey)) {
      canonicalHeadersMap[normalizedKey] = normalizeHeaderValue(value);
    }
  });

  const signedHeaderNames = Object.keys(canonicalHeadersMap).sort();
  const canonicalHeaders = signedHeaderNames
    .map((headerName) => `${headerName}:${canonicalHeadersMap[headerName]}\n`)
    .join('');
  const signedHeaders = signedHeaderNames.join(';');
  const canonicalRequest = [
    method.toUpperCase(),
    buildCanonicalUri(requestUrl),
    buildCanonicalQueryString(requestUrl),
    canonicalHeaders,
    signedHeaders,
    payloadHash,
  ].join('\n');

  const stringToSign = [
    'AWS4-HMAC-SHA256',
    amzDate,
    scope,
    await sha256Hex(canonicalRequest),
  ].join('\n');

  const dateKey = await hmacSha256(`AWS4${secretKey}`, dateStamp);
  const regionKey = await hmacSha256(dateKey, region);
  const serviceKey = await hmacSha256(regionKey, service);
  const signingKey = await hmacSha256(serviceKey, 'aws4_request');
  const signature = bytesToHex(await hmacSha256(signingKey, stringToSign));

  return {
    ...unsignedHeaders,
    Authorization: `AWS4-HMAC-SHA256 Credential=${accessKey}/${scope}, SignedHeaders=${signedHeaders}, Signature=${signature}`,
    'x-amz-content-sha256': payloadHash,
    'x-amz-date': amzDate,
  };
};

export const buildS3AuthorizationHeader = (accessKey: string = 'default'): string =>
  `AWS4-HMAC-SHA256 Credential=${accessKey}/<date>/${DEFAULT_S3_REGION}/s3/aws4_request, SignedHeaders=host;x-amz-content-sha256;x-amz-date, Signature=<calculated at request time>`;
