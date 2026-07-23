import { createApi, BaseQueryFn } from '@reduxjs/toolkit/query/react';
import { keepUnusedDataFor } from '#/constants/config';
import { adminRequest, toRtkQueryError } from '#/services/backendApi.service';

export interface ApiBaseQueryArgs {
  url: string;
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';
  body?: any;
  headers?: Record<string, string>;
  cache?: RequestCache;
}

// Standard dashboard base query. All Less3 backend calls flow through backendApi.service.
export const dynamicBaseQuery: BaseQueryFn<ApiBaseQueryArgs, unknown, unknown> = async (
  args: ApiBaseQueryArgs
) => {
  try {
    const data = await adminRequest<unknown>(args.url, {
      method: args.method || 'GET',
      headers: args.headers,
      body: args.body,
      cache: args.cache,
    });

    return { data };
  } catch (error) {
    return {
      error: toRtkQueryError(error, 'Backend request failed'),
    };
  }
};

const sdkSlice = createApi({
  reducerPath: 'sdk',
  baseQuery: dynamicBaseQuery,
  tagTypes: [],
  endpoints: () => ({}),
  keepUnusedDataFor: keepUnusedDataFor,
});

export default sdkSlice;
