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

const getSubtleCrypto = (): SubtleCrypto | null => globalThis.crypto?.subtle ?? null;

const rotateRight = (value: number, amount: number): number => (value >>> amount) | (value << (32 - amount));

const SHA256_INITIAL_HASH = new Uint32Array([
  0x6a09e667,
  0xbb67ae85,
  0x3c6ef372,
  0xa54ff53a,
  0x510e527f,
  0x9b05688c,
  0x1f83d9ab,
  0x5be0cd19,
]);

const SHA256_K = new Uint32Array([
  0x428a2f98,
  0x71374491,
  0xb5c0fbcf,
  0xe9b5dba5,
  0x3956c25b,
  0x59f111f1,
  0x923f82a4,
  0xab1c5ed5,
  0xd807aa98,
  0x12835b01,
  0x243185be,
  0x550c7dc3,
  0x72be5d74,
  0x80deb1fe,
  0x9bdc06a7,
  0xc19bf174,
  0xe49b69c1,
  0xefbe4786,
  0x0fc19dc6,
  0x240ca1cc,
  0x2de92c6f,
  0x4a7484aa,
  0x5cb0a9dc,
  0x76f988da,
  0x983e5152,
  0xa831c66d,
  0xb00327c8,
  0xbf597fc7,
  0xc6e00bf3,
  0xd5a79147,
  0x06ca6351,
  0x14292967,
  0x27b70a85,
  0x2e1b2138,
  0x4d2c6dfc,
  0x53380d13,
  0x650a7354,
  0x766a0abb,
  0x81c2c92e,
  0x92722c85,
  0xa2bfe8a1,
  0xa81a664b,
  0xc24b8b70,
  0xc76c51a3,
  0xd192e819,
  0xd6990624,
  0xf40e3585,
  0x106aa070,
  0x19a4c116,
  0x1e376c08,
  0x2748774c,
  0x34b0bcb5,
  0x391c0cb3,
  0x4ed8aa4a,
  0x5b9cca4f,
  0x682e6ff3,
  0x748f82ee,
  0x78a5636f,
  0x84c87814,
  0x8cc70208,
  0x90befffa,
  0xa4506ceb,
  0xbef9a3f7,
  0xc67178f2,
]);

const sha256BytesFallback = (input: Uint8Array): Uint8Array => {
  const bitLength = input.length * 8;
  const paddedLength = (((input.length + 9 + 63) >> 6) << 6);
  const padded = new Uint8Array(paddedLength);

  padded.set(input);
  padded[input.length] = 0x80;

  const bitLengthHigh = Math.floor(bitLength / 0x100000000);
  const bitLengthLow = bitLength >>> 0;
  const view = new DataView(padded.buffer);
  view.setUint32(paddedLength - 8, bitLengthHigh, false);
  view.setUint32(paddedLength - 4, bitLengthLow, false);

  const hash = new Uint32Array(SHA256_INITIAL_HASH);
  const words = new Uint32Array(64);

  for (let offset = 0; offset < padded.length; offset += 64) {
    for (let index = 0; index < 16; index += 1) {
      words[index] = view.getUint32(offset + index * 4, false);
    }

    for (let index = 16; index < 64; index += 1) {
      const s0 =
        rotateRight(words[index - 15], 7) ^
        rotateRight(words[index - 15], 18) ^
        (words[index - 15] >>> 3);
      const s1 =
        rotateRight(words[index - 2], 17) ^
        rotateRight(words[index - 2], 19) ^
        (words[index - 2] >>> 10);

      words[index] = (((words[index - 16] + s0) >>> 0) + ((words[index - 7] + s1) >>> 0)) >>> 0;
    }

    let [a, b, c, d, e, f, g, h] = hash;

    for (let index = 0; index < 64; index += 1) {
      const s1 = rotateRight(e, 6) ^ rotateRight(e, 11) ^ rotateRight(e, 25);
      const choice = (e & f) ^ (~e & g);
      const temp1 = (((((h + s1) >>> 0) + choice) >>> 0) + ((SHA256_K[index] + words[index]) >>> 0)) >>> 0;
      const s0 = rotateRight(a, 2) ^ rotateRight(a, 13) ^ rotateRight(a, 22);
      const majority = (a & b) ^ (a & c) ^ (b & c);
      const temp2 = (s0 + majority) >>> 0;

      h = g;
      g = f;
      f = e;
      e = (d + temp1) >>> 0;
      d = c;
      c = b;
      b = a;
      a = (temp1 + temp2) >>> 0;
    }

    hash[0] = (hash[0] + a) >>> 0;
    hash[1] = (hash[1] + b) >>> 0;
    hash[2] = (hash[2] + c) >>> 0;
    hash[3] = (hash[3] + d) >>> 0;
    hash[4] = (hash[4] + e) >>> 0;
    hash[5] = (hash[5] + f) >>> 0;
    hash[6] = (hash[6] + g) >>> 0;
    hash[7] = (hash[7] + h) >>> 0;
  }

  const digest = new Uint8Array(32);
  const digestView = new DataView(digest.buffer);

  hash.forEach((value, index) => {
    digestView.setUint32(index * 4, value, false);
  });

  return digest;
};

const hmacSha256Fallback = (key: Uint8Array, data: Uint8Array): Uint8Array => {
  const blockSize = 64;
  let normalizedKey = key;

  if (normalizedKey.length > blockSize) {
    normalizedKey = sha256BytesFallback(normalizedKey);
  }

  if (normalizedKey.length < blockSize) {
    const paddedKey = new Uint8Array(blockSize);
    paddedKey.set(normalizedKey);
    normalizedKey = paddedKey;
  }

  const outerPad = new Uint8Array(blockSize);
  const innerPad = new Uint8Array(blockSize);

  for (let index = 0; index < blockSize; index += 1) {
    outerPad[index] = normalizedKey[index] ^ 0x5c;
    innerPad[index] = normalizedKey[index] ^ 0x36;
  }

  const innerMessage = new Uint8Array(innerPad.length + data.length);
  innerMessage.set(innerPad);
  innerMessage.set(data, innerPad.length);

  const innerHash = sha256BytesFallback(innerMessage);

  const outerMessage = new Uint8Array(outerPad.length + innerHash.length);
  outerMessage.set(outerPad);
  outerMessage.set(innerHash, outerPad.length);

  return sha256BytesFallback(outerMessage);
};

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
  const data = typeof input === 'string' ? encoder.encode(input) : new Uint8Array(input);
  const subtleCrypto = getSubtleCrypto();

  if (subtleCrypto) {
    const digest = await subtleCrypto.digest('SHA-256', data);
    return bytesToHex(new Uint8Array(digest));
  }

  return bytesToHex(sha256BytesFallback(data));
};

const hmacSha256 = async (key: string | Uint8Array, data: string): Promise<Uint8Array> => {
  const rawKey = typeof key === 'string' ? encoder.encode(key) : key;
  const subtleCrypto = getSubtleCrypto();

  if (subtleCrypto) {
    const cryptoKey = await subtleCrypto.importKey(
      'raw',
      toCryptoArrayBuffer(rawKey),
      {
        name: 'HMAC',
        hash: 'SHA-256',
      },
      false,
      ['sign']
    );
    const signature = await subtleCrypto.sign('HMAC', cryptoKey, encoder.encode(data));
    return new Uint8Array(signature);
  }

  return hmacSha256Fallback(rawKey, encoder.encode(data));
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
