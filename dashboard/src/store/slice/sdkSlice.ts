import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import { buildApiUrl } from '#/services/sdk.service';
import { MOCK_VALIDATE_CONNECTIVITY } from '#/constants/config';

const enhancedSdk = sdkSlice.enhanceEndpoints({
  addTagTypes: [],
});

const sdkSliceInstance = enhancedSdk.injectEndpoints({
  endpoints: (build: EndpointBuilder<BaseQueryFn<ApiBaseQueryArgs, unknown, unknown>, never, 'sdk'>) => ({
    validateConnectivity: MOCK_VALIDATE_CONNECTIVITY
      ? build.mutation<boolean, void>({
          async queryFn() {
            return { data: true };
          },
        })
      : build.mutation<boolean, void>({
          query: () => ({ url: buildApiUrl(''), method: 'GET' }),
          transformResponse: (response: any) =>
            response?.status === 'ok' || response === true || response?.success === true,
        }),
  }),
});

export const { useValidateConnectivityMutation } = sdkSliceInstance;
