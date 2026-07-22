import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import { buildAdminApiHeaders, buildApiUrl } from '#/services/sdk.service';
import sdkSlice, { ApiBaseQueryArgs } from '#/store/rtk/rtkSdkInstance';
import type {
  AdminHealthStatusResponse,
  DashboardStatsResponse,
  MaintenanceActionResult,
  MaintenanceSettingsRequest,
  MaintenanceStatusResponse,
  RequestReportResponse,
} from './dashboardStatsTypes';

export enum DashboardStatsSliceTags {
  DASHBOARD_STATS = 'DASHBOARD_STATS',
}

export type {
  AdminHealthStatusResponse,
  DashboardStatsResponse,
  MaintenanceActionResult,
  MaintenanceSettingsRequest,
  MaintenanceStatusResponse,
  RequestReportResponse,
};

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
    getRequestReport: build.query<RequestReportResponse, { startUtc: string; endUtc: string; tenantId?: string }>({
      async queryFn({ startUtc, endUtc, tenantId = 'default' }) {
        try {
          const query = new URLSearchParams({ tenantId, startUtc, endUtc });
          const response = await fetch(buildApiUrl(`admin/reports/requests?${query.toString()}`), {
            method: 'GET',
            headers: buildAdminApiHeaders(),
            cache: 'no-store',
          });

          if (!response.ok) {
            const responseText = (await response.text()).trim();
            return {
              error: {
                status: response.status,
                data: responseText || `Failed to fetch request report: ${response.status}`,
              },
            };
          }

          return {
            data: await response.json() as RequestReportResponse,
          };
        } catch (error: any) {
          return {
            error: {
              status: 'FETCH_ERROR',
              data: error?.message || 'Failed to fetch request report',
            },
          };
        }
      },
      providesTags: [{ type: DashboardStatsSliceTags.DASHBOARD_STATS, id: 'REPORT' }],
    }),
    getMaintenanceStatus: build.query<MaintenanceStatusResponse, void>({
      async queryFn() {
        try {
          const response = await fetch(buildApiUrl('admin/maintenance/status'), {
            method: 'GET',
            headers: buildAdminApiHeaders(),
            cache: 'no-store',
          });

          if (!response.ok) {
            const responseText = (await response.text()).trim();
            return {
              error: {
                status: response.status,
                data: responseText || `Failed to fetch maintenance status: ${response.status}`,
              },
            };
          }

          return {
            data: await response.json() as MaintenanceStatusResponse,
          };
        } catch (error: any) {
          return {
            error: {
              status: 'FETCH_ERROR',
              data: error?.message || 'Failed to fetch maintenance status',
            },
          };
        }
      },
      providesTags: [{ type: DashboardStatsSliceTags.DASHBOARD_STATS, id: 'MAINTENANCE' }],
    }),
    updateMaintenanceSettings: build.mutation<MaintenanceActionResult, MaintenanceSettingsRequest>({
      async queryFn(body) {
        try {
          const response = await fetch(buildApiUrl('admin/maintenance/settings'), {
            method: 'POST',
            headers: buildAdminApiHeaders(),
            body: JSON.stringify(body),
            cache: 'no-store',
          });

          if (!response.ok) {
            const responseText = (await response.text()).trim();
            return {
              error: {
                status: response.status,
                data: responseText || `Failed to update maintenance settings: ${response.status}`,
              },
            };
          }

          return {
            data: await response.json() as MaintenanceActionResult,
          };
        } catch (error: any) {
          return {
            error: {
              status: 'FETCH_ERROR',
              data: error?.message || 'Failed to update maintenance settings',
            },
          };
        }
      },
      invalidatesTags: [{ type: DashboardStatsSliceTags.DASHBOARD_STATS, id: 'MAINTENANCE' }, { type: DashboardStatsSliceTags.DASHBOARD_STATS, id: 'HEALTH' }],
    }),
    runMaintenanceAction: build.mutation<MaintenanceActionResult, { action: string; body?: MaintenanceSettingsRequest }>({
      async queryFn({ action, body = {} }) {
        try {
          const response = await fetch(buildApiUrl(`admin/maintenance/${action}`), {
            method: 'POST',
            headers: buildAdminApiHeaders(),
            body: JSON.stringify(body),
            cache: 'no-store',
          });

          if (!response.ok) {
            const responseText = (await response.text()).trim();
            return {
              error: {
                status: response.status,
                data: responseText || `Failed to run maintenance action: ${response.status}`,
              },
            };
          }

          return {
            data: await response.json() as MaintenanceActionResult,
          };
        } catch (error: any) {
          return {
            error: {
              status: 'FETCH_ERROR',
              data: error?.message || 'Failed to run maintenance action',
            },
          };
        }
      },
      invalidatesTags: [
        { type: DashboardStatsSliceTags.DASHBOARD_STATS, id: 'MAINTENANCE' },
        { type: DashboardStatsSliceTags.DASHBOARD_STATS, id: 'HEALTH' },
        { type: DashboardStatsSliceTags.DASHBOARD_STATS, id: 'REPORT' },
      ],
    }),
  }),
});

export const {
  useGetAdminHealthQuery,
  useGetDashboardStatsQuery,
  useGetRequestReportQuery,
  useGetMaintenanceStatusQuery,
  useUpdateMaintenanceSettingsMutation,
  useRunMaintenanceActionMutation,
} = dashboardStatsSliceInstance;
