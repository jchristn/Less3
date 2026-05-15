import { apiEndpointURL } from '#/constants/config';
import { localStorageKeys } from '#/constants/constant';

const legacyDashboardApiEndpoint = 'http://localhost:3000';

const normalizeApiEndpoint = (endpoint: string): string => {
  const trimmedEndpoint = endpoint.trim();
  if (!trimmedEndpoint) {
    return '';
  }

  return trimmedEndpoint.endsWith('/') ? trimmedEndpoint : `${trimmedEndpoint}/`;
};

// Store the API endpoint URL
let apiEndpoint = normalizeApiEndpoint(apiEndpointURL);
let adminApiKey = '';

const getStorageValue = (key: string): string | null => {
  if (typeof window === 'undefined') {
    return null;
  }

  try {
    const storedValue = window.localStorage.getItem(key)?.trim();
    return storedValue || null;
  } catch {
    return null;
  }
};

const getStoredApiEndpoint = (): string | null => {
  const storedEndpoint = getStorageValue(localStorageKeys.less3APIUrl);
  if (!storedEndpoint) {
    return null;
  }

  if (
    typeof window !== 'undefined' &&
    normalizeApiEndpoint(storedEndpoint) === normalizeApiEndpoint(legacyDashboardApiEndpoint)
  ) {
    window.localStorage.removeItem(localStorageKeys.less3APIUrl);
    return null;
  }

  return storedEndpoint;
};

export const getInitialApiEndpoint = (): string => getStoredApiEndpoint() || apiEndpointURL;
export const getInitialAdminApiKey = (): string => getStorageValue(localStorageKeys.adminApiKey) || '';

export const getApiEndpoint = (): string => {
  const storedEndpoint = getStoredApiEndpoint();
  if (storedEndpoint) {
    apiEndpoint = normalizeApiEndpoint(storedEndpoint);
  }

  return apiEndpoint;
};

export const getAdminApiKey = (): string => {
  const storedAdminApiKey = getStorageValue(localStorageKeys.adminApiKey);
  if (storedAdminApiKey) {
    adminApiKey = storedAdminApiKey;
  }

  return adminApiKey;
};

export const updateSdkEndPoint = (endpoint: string) => {
  apiEndpoint = normalizeApiEndpoint(endpoint) || normalizeApiEndpoint(apiEndpointURL);
};

export const updateAdminApiKey = (apiKey: string) => {
  adminApiKey = apiKey.trim();
};

export const persistDashboardSession = (endpoint: string, apiKey: string) => {
  const trimmedEndpoint = endpoint.trim();
  const trimmedApiKey = apiKey.trim();

  updateSdkEndPoint(trimmedEndpoint);
  updateAdminApiKey(trimmedApiKey);

  if (typeof window === 'undefined') {
    return;
  }

  if (trimmedEndpoint) {
    window.localStorage.setItem(localStorageKeys.less3APIUrl, trimmedEndpoint);
  } else {
    window.localStorage.removeItem(localStorageKeys.less3APIUrl);
  }

  if (trimmedApiKey) {
    window.localStorage.setItem(localStorageKeys.adminApiKey, trimmedApiKey);
  } else {
    window.localStorage.removeItem(localStorageKeys.adminApiKey);
  }
};

export const clearDashboardSession = () => {
  updateSdkEndPoint(apiEndpointURL);
  updateAdminApiKey('');

  if (typeof window === 'undefined') {
    return;
  }

  window.localStorage.removeItem(localStorageKeys.less3APIUrl);
  window.localStorage.removeItem(localStorageKeys.adminApiKey);
};

export const buildAdminApiHeaders = (
  headers: Record<string, string> = {},
  apiKey?: string
): Record<string, string> => {
  const resolvedApiKey = (apiKey ?? getAdminApiKey()).trim();

  if (!resolvedApiKey) {
    return headers;
  }

  return {
    ...headers,
    'x-api-key': resolvedApiKey,
  };
};

const getBaseUrlFromEndpoint = (endpoint: string): string => {
  const normalizedEndpoint = normalizeApiEndpoint(endpoint);
  return normalizedEndpoint.endsWith('/') ? normalizedEndpoint.slice(0, -1) : normalizedEndpoint;
};

// Helper function to get API base URL without trailing slash
export const getBaseUrl = (): string => getBaseUrlFromEndpoint(getApiEndpoint());

// Helper function to build full API URL
export const buildApiUrlFromEndpoint = (endpoint: string, path: string): string => {
  const baseUrl = getBaseUrlFromEndpoint(endpoint);
  const cleanPath = path.startsWith('/') ? path.slice(1) : path;

  if (!baseUrl) {
    return cleanPath ? `/${cleanPath}` : '/';
  }

  return cleanPath ? `${baseUrl}/${cleanPath}` : `${baseUrl}/`;
};

export const buildApiUrl = (path: string): string => buildApiUrlFromEndpoint(getApiEndpoint(), path);
