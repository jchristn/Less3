import { buildAdminApiHeaders, buildApiUrl, buildApiUrlFromEndpoint, getBaseUrl } from '#/services/sdk.service';
import { buildSignedS3Headers, selectS3Credential, type S3CredentialLike } from '#/utils/s3Auth';

export type BackendMethod = 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH' | 'HEAD';
export type BackendParseAs = 'auto' | 'json' | 'text';

export interface BackendRequestOptions {
  method?: BackendMethod;
  headers?: Record<string, string | undefined>;
  body?: BodyInit | Record<string, unknown> | null;
  cache?: RequestCache;
  signal?: AbortSignal;
  endpoint?: string;
  apiKey?: string;
  parseAs?: BackendParseAs;
}

export interface BackendRtkError {
  status: number | string;
  data: string;
}

export class BackendApiError extends Error {
  status: number | string;
  statusText: string;
  data: string;
  url: string;

  constructor(message: string, options: { status: number | string; statusText?: string; data?: string; url?: string }) {
    super(message);
    this.name = 'BackendApiError';
    this.status = options.status;
    this.statusText = options.statusText || '';
    this.data = options.data || message;
    this.url = options.url || '';
  }
}

const isAbsoluteUrl = (value: string): boolean => /^https?:\/\//i.test(value);

const normalizeHeaders = (headers?: Record<string, string | undefined>): Record<string, string> =>
  Object.entries(headers || {}).reduce<Record<string, string>>((accumulator, [key, value]) => {
    if (typeof value === 'string' && value.length > 0) {
      accumulator[key] = value;
    }

    return accumulator;
  }, {});

const hasHeader = (headers: Record<string, string>, name: string): boolean =>
  Object.keys(headers).some((key) => key.toLowerCase() === name.toLowerCase());

const isBodyInit = (body: unknown): body is BodyInit =>
  typeof body === 'string' ||
  body instanceof Blob ||
  body instanceof FormData ||
  body instanceof URLSearchParams ||
  body instanceof ArrayBuffer ||
  ArrayBuffer.isView(body);

const prepareBody = (
  body: BackendRequestOptions['body'],
  headers: Record<string, string>
): BodyInit | null | undefined => {
  if (body == null) {
    return undefined;
  }

  if (isBodyInit(body)) {
    return body;
  }

  if (!hasHeader(headers, 'Content-Type')) {
    headers['Content-Type'] = 'application/json';
  }

  return JSON.stringify(body);
};

export const buildBackendUrl = (pathOrUrl: string, endpoint?: string): string => {
  if (isAbsoluteUrl(pathOrUrl)) {
    return pathOrUrl;
  }

  return endpoint ? buildApiUrlFromEndpoint(endpoint, pathOrUrl) : buildApiUrl(pathOrUrl);
};

export const buildS3Url = (pathOrUrl: string): string => {
  if (isAbsoluteUrl(pathOrUrl)) {
    return pathOrUrl;
  }

  const path = pathOrUrl.startsWith('/') ? pathOrUrl : `/${pathOrUrl}`;
  return `${getBaseUrl()}${path}`;
};

export const readResponseText = async (response: Response): Promise<string> => {
  try {
    return (await response.text()).trim();
  } catch {
    return '';
  }
};

export const readBackendErrorMessage = async (response: Response, fallbackMessage: string): Promise<string> => {
  const responseText = await readResponseText(response);

  if (responseText) {
    const codeMatch = responseText.match(/<Code>([^<]+)<\/Code>/i);
    const messageMatch = responseText.match(/<Message>([^<]+)<\/Message>/i);
    const code = codeMatch?.[1];
    const message = messageMatch?.[1];

    if (code || message) {
      return `${fallbackMessage}: ${[code, message].filter(Boolean).join(' - ')}`;
    }

    return responseText;
  }

  return `${fallbackMessage}: ${response.statusText || response.status}`;
};

const parseBackendResponse = async <T>(response: Response, parseAs: BackendParseAs = 'auto'): Promise<T> => {
  const responseText = await readResponseText(response);

  if (!responseText) {
    return null as T;
  }

  const contentType = response.headers.get('content-type') || '';
  if (parseAs === 'json' || (parseAs === 'auto' && contentType.includes('application/json'))) {
    return JSON.parse(responseText) as T;
  }

  return responseText as T;
};

export const rawBackendFetch = async (url: string, init: RequestInit): Promise<Response> => fetch(url, init);

export const buildAdminRequestHeaders = (
  headers?: Record<string, string | undefined>,
  apiKey?: string
): Record<string, string> => buildAdminApiHeaders(normalizeHeaders(headers), apiKey);

export const adminFetch = async (
  pathOrUrl: string,
  options: BackendRequestOptions = {}
): Promise<Response> => {
  const headers = buildAdminRequestHeaders(options.headers, options.apiKey);
  const body = prepareBody(options.body, headers);

  return rawBackendFetch(buildBackendUrl(pathOrUrl, options.endpoint), {
    method: options.method || 'GET',
    headers,
    body,
    cache: options.cache,
    signal: options.signal,
  });
};

export const adminRequest = async <T>(pathOrUrl: string, options: BackendRequestOptions = {}): Promise<T> => {
  const response = await adminFetch(pathOrUrl, options);

  if (!response.ok) {
    const data = await readBackendErrorMessage(response, 'Backend request failed');
    throw new BackendApiError(data, {
      status: response.status,
      statusText: response.statusText,
      data,
      url: response.url,
    });
  }

  return parseBackendResponse<T>(response, options.parseAs);
};

export const toRtkQueryError = (error: unknown, fallbackMessage: string): BackendRtkError => {
  if (error instanceof BackendApiError) {
    return {
      status: error.status,
      data: error.data || error.message || fallbackMessage,
    };
  }

  const message = error instanceof Error ? error.message : String(error || fallbackMessage);
  return {
    status: 'FETCH_ERROR',
    data: message || fallbackMessage,
  };
};

const fetchAdminCredentials = async (): Promise<S3CredentialLike[]> => {
  try {
    const credentials = await adminRequest<S3CredentialLike[]>('admin/credentials', { cache: 'no-store' });
    return Array.isArray(credentials) ? credentials : [];
  } catch {
    return [];
  }
};

const fetchCredentialById = async (id: string): Promise<S3CredentialLike | null> => {
  try {
    return await adminRequest<S3CredentialLike>(`admin/credentials/${id}`, { cache: 'no-store' });
  } catch {
    return null;
  }
};

export const resolveS3Credential = async (): Promise<S3CredentialLike | null> => {
  const credentials = await fetchAdminCredentials();
  const selectedCredential = selectS3Credential(credentials);

  if (!selectedCredential) {
    return null;
  }

  if (selectedCredential.SecretKey?.trim()) {
    return selectedCredential;
  }

  if (!selectedCredential.Id) {
    return null;
  }

  return fetchCredentialById(selectedCredential.Id);
};

export const buildS3RequestHeaders = async (
  method: string,
  url: string,
  options?: {
    headers?: Record<string, string | undefined>;
    body?: BodyInit | null;
  }
): Promise<Record<string, string>> => {
  const normalizedHeaders = buildAdminRequestHeaders(options?.headers);
  const credential = await resolveS3Credential();

  if (!credential?.AccessKey || !credential.SecretKey) {
    return normalizedHeaders;
  }

  return buildSignedS3Headers({
    method,
    url,
    accessKey: credential.AccessKey,
    secretKey: credential.SecretKey,
    headers: normalizedHeaders,
    body: options?.body,
  });
};

export const s3Fetch = async (
  pathOrUrl: string,
  options: {
    method: BackendMethod;
    headers?: Record<string, string | undefined>;
    body?: BodyInit | null;
    cache?: RequestCache;
    signal?: AbortSignal;
  }
): Promise<Response> => {
  const url = buildS3Url(pathOrUrl);
  const headers = await buildS3RequestHeaders(options.method, url, {
    headers: options.headers,
    body: options.body,
  });

  return rawBackendFetch(url, {
    method: options.method,
    headers,
    body: options.body,
    cache: options.cache,
    signal: options.signal,
  });
};
