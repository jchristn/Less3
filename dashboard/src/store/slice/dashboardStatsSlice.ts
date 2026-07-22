import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import { buildAdminApiHeaders, buildApiUrl } from '#/services/sdk.service';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import type { AdminHealthStatusResponse, DashboardStatsResponse } from './dashboardStatsTypes';

export enum DashboardStatsSliceTags {
  DASHBOARD_STATS = 'DASHBOARD_STATS',
}

export type { AdminHealthStatusResponse, DashboardStatsResponse };

const enhancedSdk = sdkSlice.enhanceEndpoints({
  addTagTypes: [DashboardStatsSliceTags.DASHBOARD_STATS],
});

const dashboardStatsSliceInstance = enhancedSdk.injectEndpoints({
  overrideExisting: true,
  endpoints: (
    build: EndpointBuilder<BaseQueryFn<ApiBaseQueryArgs, unknown, unknown>, DashboardStatsSliceTags, 'sdk'>
  ) => ({
    getDashboardStats: build.query<DashboardStatsResponse, void>({
      async queryFn() {
        try {
          const response = await fetch(buildApiUrl('admin/stats'), {
            method: 'GET',
            headers: buildAdminApiHeaders(),
            cache: 'no-store',
          });

          if (!response.ok) {
            const responseText = (await response.text()).trim();
            return {
              error: {
                status: response.status,
                data: responseText || `Failed to fetch dashboard stats: ${response.status}`,
              },
            };
          }

          return {
            data: await response.json() as DashboardStatsResponse,
          };
        } catch (error: any) {
          return {
            error: {
              status: 'FETCH_ERROR',
              data: error?.message || 'Failed to fetch dashboard stats',
            },
          };
        }
      },
      providesTags: [{ type: DashboardStatsSliceTags.DASHBOARD_STATS, id: 'SUMMARY' }],
    }),
    getAdminHealth: build.query<AdminHealthStatusResponse, void>({
      async queryFn() {
        try {
          const response = await fetch(buildApiUrl('admin/health'), {
            method: 'GET',
            headers: buildAdminApiHeaders(),
            cache: 'no-store',
          });

          if (!response.ok) {
            const responseText = (await response.text()).trim();
            return {
              error: {
                status: response.status,
                data: responseText || `Failed to fetch admin health: ${response.status}`,
              },
            };
          }

          return {
            data: await response.json() as AdminHealthStatusResponse,
          };
        } catch (error: any) {
          return {
            error: {
              status: 'FETCH_ERROR',
              data: error?.message || 'Failed to fetch admin health',
            },
          };
        }
      },
      providesTags: [{ type: DashboardStatsSliceTags.DASHBOARD_STATS, id: 'HEALTH' }],
    }),
  }),
});

export const { useGetAdminHealthQuery, useGetDashboardStatsQuery } = dashboardStatsSliceInstance;
