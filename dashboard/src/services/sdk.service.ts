import { apiEndpointURL } from '#/constants/config';
import { localStorageKeys } from '#/constants/constant';

const legacyDashboardApiEndpoint = 'http://localhost:3000';

const normalizeApiEndpoint = (endpoint: string): string => {
  const trimmedEndpoint = endpoint.trim();
  return trimmedEndpoint.endsWith('/') ? trimmedEndpoint : `${trimmedEndpoint}/`;
};

// Store the API endpoint URL
let apiEndpoint = normalizeApiEndpoint(apiEndpointURL);

const getStoredApiEndpoint = (): string | null => {
  if (typeof window === 'undefined') {
    return null;
  }

  try {
    const storedEndpoint = window.localStorage.getItem(localStorageKeys.less3APIUrl)?.trim();
    if (!storedEndpoint) {
      return null;
    }

    if (normalizeApiEndpoint(storedEndpoint) === normalizeApiEndpoint(legacyDashboardApiEndpoint)) {
      window.localStorage.removeItem(localStorageKeys.less3APIUrl);
      return null;
    }

    return storedEndpoint;
  } catch {
    return null;
  }
};

export const getInitialApiEndpoint = (): string => getStoredApiEndpoint() || apiEndpointURL;

export const getApiEndpoint = (): string => {
  const storedEndpoint = getStoredApiEndpoint();
  if (storedEndpoint) {
    apiEndpoint = normalizeApiEndpoint(storedEndpoint);
  }

  return apiEndpoint;
};

export const updateSdkEndPoint = (endpoint: string) => {
  apiEndpoint = normalizeApiEndpoint(endpoint);
};

// Helper function to get API base URL without trailing slash
export const getBaseUrl = (): string => {
  const endpoint = getApiEndpoint();
  return endpoint.endsWith('/') ? endpoint.slice(0, -1) : endpoint;
};

// Helper function to build full API URL
export const buildApiUrl = (path: string): string => {
  const baseUrl = getBaseUrl();
  const cleanPath = path.startsWith('/') ? path.slice(1) : path;
  return `${baseUrl}/${cleanPath}`;
};
