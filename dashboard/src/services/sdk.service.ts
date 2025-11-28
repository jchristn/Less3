import { apiEndpointURL } from '#/constants/config';

// Store the API endpoint URL
let apiEndpoint = apiEndpointURL;

export const getApiEndpoint = (): string => {
  return apiEndpoint;
};

export const updateSdkEndPoint = (endpoint: string) => {
  apiEndpoint = endpoint.endsWith('/') ? endpoint : `${endpoint}/`;
};

// Helper function to build full API URL
export const buildApiUrl = (path: string): string => {
  const baseUrl = getApiEndpoint();
  const cleanPath = path.startsWith('/') ? path.slice(1) : path;
  return `${baseUrl}${cleanPath}`;
};
