import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import { buildAdminApiHeaders, buildApiUrl, buildApiUrlFromEndpoint } from '#/services/sdk.service';

const enhancedSdk = sdkSlice.enhanceEndpoints({
  addTagTypes: [],
});

const INVALID_SERVER_URL_MESSAGE =
  'The server URL did not return a Less3 admin API response. Check the Less3 Server URL.';
const INVALID_API_KEY_MESSAGE = 'We are unable to authenticate using the supplied API key.';

const sdkSliceInstance = enhancedSdk.injectEndpoints({
  overrideExisting: true,
  endpoints: (build: EndpointBuilder<BaseQueryFn<ApiBaseQueryArgs, unknown, unknown>, never, 'sdk'>) => ({
    validateConnectivity: build.mutation<boolean, { apiKey?: string; endpoint?: string } | void>({
      async queryFn(args) {
        try {
          const url = args?.endpoint
            ? buildApiUrlFromEndpoint(args.endpoint, 'admin/users')
            : buildApiUrl('admin/users');
          const response = await fetch(url, {
            method: 'GET',
            headers: buildAdminApiHeaders(
              {
                'Content-Type': 'application/json',
              },
              args?.apiKey
            ),
            cache: 'no-store',
          });

          if (!response.ok) {
            if (response.status === 401) {
              return {
                error: {
                  status: response.status,
                  data: INVALID_API_KEY_MESSAGE,
                },
              };
            }

            return {
              error: {
                status: response.status,
                data: `HTTP ${response.status}: ${response.statusText}`,
              },
            };
          }

          const responseText = await response.text();
          const trimmedResponseText = responseText.trim();
          const contentType = response.headers.get('content-type') || '';

          if (
            contentType.includes('text/html') ||
            trimmedResponseText.startsWith('<!DOCTYPE') ||
            trimmedResponseText.startsWith('<html')
          ) {
            return {
              error: {
                status: 'PARSING_ERROR',
                data: INVALID_SERVER_URL_MESSAGE,
              },
            };
          }

          let parsedResponse: unknown;
          try {
            parsedResponse = trimmedResponseText ? JSON.parse(trimmedResponseText) : null;
          } catch {
            return {
              error: {
                status: 'PARSING_ERROR',
                data: INVALID_SERVER_URL_MESSAGE,
              },
            };
          }

          if (!Array.isArray(parsedResponse)) {
            return {
              error: {
                status: 'PARSING_ERROR',
                data: INVALID_SERVER_URL_MESSAGE,
              },
            };
          }

          return { data: true };
        } catch (error: any) {
          return {
            error: {
              status: 'FETCH_ERROR',
              data: error?.message || String(error),
            },
          };
        }
      },
    }),
  }),
});

export const { useValidateConnectivityMutation } = sdkSliceInstance;
