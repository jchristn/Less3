import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query/react';
import { adminRequest, toRtkQueryError } from '#/services/backendApi.service';
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

const adminQuery = async <T,>(path: string, fallbackMessage: string, body?: Record<string, unknown>) => {
  try {
    const data = await adminRequest<T>(path, {
      method: body ? 'POST' : 'GET',
      body,
      cache: 'no-store',
    });

    return { data };
  } catch (error) {
    return {
      error: toRtkQueryError(error, fallbackMessage),
    };
  }
};

const dashboardStatsSliceInstance = enhancedSdk.injectEndpoints({
  overrideExisting: true,
  endpoints: (
    build: EndpointBuilder<BaseQueryFn<ApiBaseQueryArgs, unknown, unknown>, DashboardStatsSliceTags, 'sdk'>
  ) => ({
    getDashboardStats: build.query<DashboardStatsResponse, void>({
      async queryFn() {
        return adminQuery<DashboardStatsResponse>('admin/stats', 'Failed to fetch dashboard stats');
      },
      providesTags: [{ type: DashboardStatsSliceTags.DASHBOARD_STATS, id: 'SUMMARY' }],
    }),
    getAdminHealth: build.query<AdminHealthStatusResponse, void>({
      async queryFn() {
        return adminQuery<AdminHealthStatusResponse>('admin/health', 'Failed to fetch admin health');
      },
      providesTags: [{ type: DashboardStatsSliceTags.DASHBOARD_STATS, id: 'HEALTH' }],
    }),
    getRequestReport: build.query<RequestReportResponse, { startUtc: string; endUtc: string; tenantId?: string }>({
      async queryFn({ startUtc, endUtc, tenantId = 'default' }) {
        const query = new URLSearchParams({ tenantId, startUtc, endUtc });
        return adminQuery<RequestReportResponse>(
          `admin/reports/requests?${query.toString()}`,
          'Failed to fetch request report'
        );
      },
      providesTags: [{ type: DashboardStatsSliceTags.DASHBOARD_STATS, id: 'REPORT' }],
    }),
    getMaintenanceStatus: build.query<MaintenanceStatusResponse, void>({
      async queryFn() {
        return adminQuery<MaintenanceStatusResponse>('admin/maintenance/status', 'Failed to fetch maintenance status');
      },
      providesTags: [{ type: DashboardStatsSliceTags.DASHBOARD_STATS, id: 'MAINTENANCE' }],
    }),
    updateMaintenanceSettings: build.mutation<MaintenanceActionResult, MaintenanceSettingsRequest>({
      async queryFn(body) {
        return adminQuery<MaintenanceActionResult>(
          'admin/maintenance/settings',
          'Failed to update maintenance settings',
          body as unknown as Record<string, unknown>
        );
      },
      invalidatesTags: [{ type: DashboardStatsSliceTags.DASHBOARD_STATS, id: 'MAINTENANCE' }, { type: DashboardStatsSliceTags.DASHBOARD_STATS, id: 'HEALTH' }],
    }),
    runMaintenanceAction: build.mutation<MaintenanceActionResult, { action: string; body?: MaintenanceSettingsRequest }>({
      async queryFn({ action, body = {} }) {
        return adminQuery<MaintenanceActionResult>(
          `admin/maintenance/${action}`,
          'Failed to run maintenance action',
          body as unknown as Record<string, unknown>
        );
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
