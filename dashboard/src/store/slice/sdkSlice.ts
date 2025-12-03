import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import { buildApiUrl } from '#/services/sdk.service';
import { API_KEY } from '#/constants/config';

const enhancedSdk = sdkSlice.enhanceEndpoints({
  addTagTypes: [],
});

const sdkSliceInstance = enhancedSdk.injectEndpoints({
  endpoints: (build: EndpointBuilder<BaseQueryFn<ApiBaseQueryArgs, unknown, unknown>, never, 'sdk'>) => ({
    validateConnectivity: build.mutation<boolean, void>({
      async queryFn() {
        try {
          const url = buildApiUrl('');
          const response = await fetch(url, {
            method: 'HEAD',
            headers: {
              'Content-Type': 'application/json',
              'x-api-key': API_KEY,
            },
          });

          // If we got a successful HTTP response (200-299), consider it valid connectivity
          if (response.ok) {
            // Try to parse JSON, but don't fail if it's empty or invalid
            let data: any = null;
            const text = await response.text();
            if (text) {
              try {
                data = JSON.parse(text);
              } catch {
                // If parsing fails, that's okay - we still got a 200 response
                data = text;
              }
            }

            // Check for specific response formats
            if (data?.status === 'ok' || data === true || data?.success === true) {
              return { data: true };
            }

            // Any successful HTTP response (200-299) indicates valid connectivity
            return { data: true };
          }

          // If response is not ok, return error
          return {
            error: {
              status: response.status,
              data: `HTTP ${response.status}: ${response.statusText}`,
            },
          };
        } catch (error: any) {
          return {
            error: {
              status: 'FETCH_ERROR',
              error: error?.message || String(error),
            },
          };
        }
      },
    }),
  }),
});

export const { useValidateConnectivityMutation } = sdkSliceInstance;
