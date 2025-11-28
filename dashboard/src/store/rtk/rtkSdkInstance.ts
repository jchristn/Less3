import { createApi, fetchBaseQuery, BaseQueryFn, BaseQueryApi } from '@reduxjs/toolkit/query/react';
import { keepUnusedDataFor, API_KEY } from '#/constants/config';
import { getApiEndpoint } from '#/services/sdk.service';

export interface ApiBaseQueryArgs {
  url: string;
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';
  body?: any;
  headers?: Record<string, string>;
}

// Custom base query that dynamically gets the API endpoint
const dynamicBaseQuery: BaseQueryFn<ApiBaseQueryArgs, unknown, unknown> = async (
  args: ApiBaseQueryArgs,
  api: unknown,
  extraOptions: unknown
) => {
  const baseUrl = getApiEndpoint();

  // Use fetchBaseQuery with dynamic baseUrl
  const fetchBaseQueryInstance = fetchBaseQuery({
    baseUrl: baseUrl,
    prepareHeaders: (headers: Headers) => {
      headers.set('Content-Type', 'application/json');
      headers.set('x-api-key', API_KEY);
      return headers;
    },
  });

  // If body is FormData, remove Content-Type header to let browser set it
  if (args.body instanceof FormData) {
    const modifiedArgs = {
      ...args,
      headers: {
        ...args.headers,
        // Don't set Content-Type for FormData
      },
    };
    return fetchBaseQueryInstance(
      modifiedArgs as ApiBaseQueryArgs,
      api as BaseQueryApi,
      extraOptions as BaseQueryFn<ApiBaseQueryArgs, unknown, unknown>
    );
  }

  return fetchBaseQueryInstance(
    args as ApiBaseQueryArgs,
    api as BaseQueryApi,
    extraOptions as BaseQueryFn<ApiBaseQueryArgs, unknown, unknown>
  );
};

const sdkSlice = createApi({
  reducerPath: 'sdk',
  baseQuery: dynamicBaseQuery,
  tagTypes: [],
  endpoints: () => ({}),
  keepUnusedDataFor: keepUnusedDataFor,
});

export default sdkSlice;
