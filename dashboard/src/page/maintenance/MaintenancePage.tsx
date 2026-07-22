'use client';

import React, { useEffect } from 'react';
import { Descriptions, Form, InputNumber, Tag } from 'antd';
import {
  ClearOutlined,
  DatabaseOutlined,
  ReloadOutlined,
  SaveOutlined,
  SearchOutlined,
} from '@ant-design/icons';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import Less3Button from '#/components/base/button/Button';
import Less3Card from '#/components/base/card/Card';
import Less3Flex from '#/components/base/flex/Flex';
import Less3FormItem from '#/components/base/form/FormItem';
import Less3Text from '#/components/base/typograpghy/Text';
import {
  useGetAdminHealthQuery,
  useGetMaintenanceStatusQuery,
  useRunMaintenanceActionMutation,
  useUpdateMaintenanceSettingsMutation,
} from '#/store/slice/dashboardStatsSlice';
import { formatDate } from '#/utils/dateUtils';
import { message } from '#/utils/message';

interface MaintenanceFormValues {
  RequestHistoryRetentionDays: number;
  CleanupIntervalMs: number;
}

const MaintenancePage: React.FC = () => {
  const [form] = Form.useForm<MaintenanceFormValues>();
  const { data: status, isLoading: statusLoading, refetch: refetchStatus } = useGetMaintenanceStatusQuery();
  const { data: health, isLoading: healthLoading, refetch: refetchHealth } = useGetAdminHealthQuery();
  const [updateSettings, { isLoading: isSaving }] = useUpdateMaintenanceSettingsMutation();
  const [runAction, { isLoading: isRunning }] = useRunMaintenanceActionMutation();

  useEffect(() => {
    if (!status) return;
    form.setFieldsValue({
      RequestHistoryRetentionDays: status.RequestHistoryRetentionDays,
      CleanupIntervalMs: status.CleanupIntervalMs,
    });
  }, [form, status]);

  const refresh = () => {
    refetchStatus();
    refetchHealth();
  };

  const saveSettings = async () => {
    try {
      const values = await form.validateFields();
      await updateSettings(values).unwrap();
      message.success('Maintenance settings updated');
      refresh();
    } catch (error: any) {
      message.error(error?.data?.message || 'Failed to update maintenance settings');
    }
  };

  const runMaintenanceAction = async (action: string, successMessage: string) => {
    try {
      await runAction({ action }).unwrap();
      message.success(successMessage);
      refresh();
    } catch (error: any) {
      message.error(error?.data?.message || 'Maintenance action failed');
    }
  };

  return (
    <PageContainer
      pageTitle="Maintenance"
      pageTitleRightContent={
        <Less3Button icon={<ReloadOutlined />} loading={statusLoading || healthLoading} onClick={refresh}>
          Refresh
        </Less3Button>
      }
    >
      <Less3Flex vertical gap={18}>
        <Less3Card>
          <Less3Flex vertical gap={16}>
            <Less3Text weight={600} fontSize={16}>Runtime Settings</Less3Text>
            <Form form={form} layout="vertical" autoComplete="off">
              <Less3Flex gap={16} wrap="wrap">
                <Less3FormItem
                  label="Request History Retention Days"
                  name="RequestHistoryRetentionDays"
                  rules={[{ required: true, message: 'Enter retention days' }]}
                  style={{ minWidth: 260 }}
                >
                  <InputNumber min={1} max={3650} style={{ width: '100%' }} />
                </Less3FormItem>
                <Less3FormItem
                  label="Cleanup Interval Ms"
                  name="CleanupIntervalMs"
                  rules={[{ required: true, message: 'Enter cleanup interval' }]}
                  style={{ minWidth: 260 }}
                >
                  <InputNumber min={1000} step={1000} style={{ width: '100%' }} />
                </Less3FormItem>
              </Less3Flex>
              <Less3Button type="primary" icon={<SaveOutlined />} loading={isSaving} onClick={saveSettings}>
                Save Settings
              </Less3Button>
            </Form>
          </Less3Flex>
        </Less3Card>

        <Less3Card>
          <Less3Flex vertical gap={16}>
            <Less3Text weight={600} fontSize={16}>Maintenance Actions</Less3Text>
            <Less3Flex gap={12} wrap="wrap">
              <Less3Button
                icon={<ReloadOutlined />}
                loading={isRunning}
                onClick={() => runMaintenanceAction('run-cleanup', 'Cleanup completed')}
              >
                Run Cleanup
              </Less3Button>
              <Less3Button
                icon={<ClearOutlined />}
                loading={isRunning}
                onClick={() => runMaintenanceAction('cleanup-temp-uploads', 'Temporary uploads cleaned')}
              >
                Clean Temp Uploads
              </Less3Button>
              <Less3Button
                icon={<DatabaseOutlined />}
                loading={isRunning}
                onClick={() => runMaintenanceAction('purge-request-history', 'Request history purged')}
              >
                Purge Old History
              </Less3Button>
              <Less3Button
                icon={<SearchOutlined />}
                loading={isRunning}
                onClick={() => runMaintenanceAction('verify-objects?tenantId=default', 'Object verification completed')}
              >
                Verify Objects
              </Less3Button>
            </Less3Flex>
          </Less3Flex>
        </Less3Card>

        <Less3Card>
          <Less3Flex vertical gap={16}>
            <Less3Text weight={600} fontSize={16}>Node Status</Less3Text>
            <Descriptions bordered size="small" column={{ xs: 1, sm: 1, md: 2 }}>
              <Descriptions.Item label="Database">
                <Tag color={health?.DatabaseReachable ? 'success' : 'error'}>{health?.DatabaseType ?? 'unknown'}</Tag>
              </Descriptions.Item>
              <Descriptions.Item label="Storage">
                <Tag color={health?.StoragePathWritable ? 'success' : 'error'}>
                  {health?.StoragePathWritable ? 'Writable' : 'Not writable'}
                </Tag>
              </Descriptions.Item>
              <Descriptions.Item label="Retention">
                {status?.RequestHistoryRetentionDays ?? health?.RequestHistoryRetentionDays ?? 30} days
              </Descriptions.Item>
              <Descriptions.Item label="Cleanup Interval">
                {status?.CleanupIntervalMs ?? 0} ms
              </Descriptions.Item>
              <Descriptions.Item label="Last Cleanup">
                {status?.LastCleanupRunUtc ? formatDate(status.LastCleanupRunUtc) : 'Never'}
              </Descriptions.Item>
              <Descriptions.Item label="Temp Uploads">
                {health?.TempUploadCount ?? 0}
              </Descriptions.Item>
              <Descriptions.Item label="Runtime Editable">
                {(status?.RuntimeEditableSettings ?? []).map((item) => <Tag key={item}>{item}</Tag>)}
              </Descriptions.Item>
              <Descriptions.Item label="Restart Required">
                {(status?.RestartRequiredSettings ?? []).map((item) => <Tag key={item} color="warning">{item}</Tag>)}
              </Descriptions.Item>
            </Descriptions>
          </Less3Flex>
        </Less3Card>
      </Less3Flex>
    </PageContainer>
  );
};

export default MaintenancePage;
