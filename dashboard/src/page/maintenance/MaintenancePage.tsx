'use client';

import React, { useEffect } from 'react';
import { Descriptions, Form, Input, InputNumber, Switch, Tag } from 'antd';
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
import Less3Input from '#/components/base/input/Input';
import Less3Select from '#/components/base/select/Select';
import Less3Text from '#/components/base/typograpghy/Text';
import {
  useGetAdminHealthQuery,
  useGetMaintenanceStatusQuery,
  useRunMaintenanceActionMutation,
  useUpdateMaintenanceSettingsMutation,
} from '#/store/slice/dashboardStatsSlice';
import { formatDate } from '#/utils/dateUtils';
import { message } from '#/utils/message';

type SettingsRecord = Record<string, any>;

interface MaintenanceFormValues extends SettingsRecord {
  __json?: Record<string, string>;
}

const REDACTED_VALUE = '[redacted]';

const DEFAULT_CONFIGURATION: SettingsRecord = {
  EnableConsole: false,
  ValidateSignatures: false,
  BaseDomain: null,
  HeaderApiKey: 'x-api-key',
  AdminApiKey: REDACTED_VALUE,
  RegionString: 'us-west-1',
  RequestHistoryRetentionDays: 30,
  CleanupIntervalMs: 3600000,
  Database: {
    Type: 'Sqlite',
    Filename: './less3.db',
    Hostname: null,
    Port: 0,
    Username: null,
    Password: null,
    Instance: null,
    DatabaseName: null,
    RequireEncryption: false,
    LogQueries: false,
  },
  Webserver: {
    Hostname: 'localhost',
    Port: 8000,
    UseMachineHostname: false,
    IO: {
      StreamBufferSize: 65536,
      MaxRequests: 1024,
      ReadTimeoutMs: 10000,
      MaxIncomingHeadersSize: 65536,
      EnableKeepAlive: true,
      MaxRequestBodySize: 0,
      MaxHeaderCount: 0,
    },
    Ssl: {
      Enable: false,
      SslCertificate: null,
      PfxCertificateFile: null,
      PfxCertificatePassword: null,
      MutuallyAuthenticate: false,
      AcceptInvalidAcertificates: true,
    },
    Headers: {
      IncludeContentLength: true,
      DefaultHeaders: {},
    },
    AccessControl: {
      DenyList: {},
      PermitList: {},
      Mode: 'DefaultPermit',
    },
    Debug: {
      AccessControl: false,
      Routing: false,
      Requests: false,
      Responses: false,
    },
  },
  Storage: {
    TempDirectory: './temp/',
    StorageType: 'Disk',
    DiskDirectory: './disk/',
  },
  Logging: {
    SyslogServerIp: '127.0.0.1',
    SyslogServerPort: 514,
    MinimumLevel: 'Info',
    LogHttpRequests: false,
    LogS3Requests: false,
    LogExceptions: false,
    LogSignatureValidation: false,
    ConsoleLogging: true,
    DiskLogging: true,
    DiskDirectory: './logs/',
  },
  Debug: {
    Authentication: false,
    S3Requests: false,
    Exceptions: false,
  },
};

const gridStyle: React.CSSProperties = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))',
  columnGap: 16,
};

const fullWidthGridStyle: React.CSSProperties = {
  gridColumn: '1 / -1',
};

const selectStyle: React.CSSProperties = { width: '100%' };

const cloneJson = <T,>(value: T): T => JSON.parse(JSON.stringify(value ?? {}));

const isPlainObject = (value: any) => Boolean(value) && typeof value === 'object' && !Array.isArray(value);

const deepMerge = (target: SettingsRecord, source: SettingsRecord): SettingsRecord => {
  const output = cloneJson(target);
  Object.keys(source || {}).forEach((key) => {
    const sourceValue = source[key];
    if (sourceValue === undefined) return;
    if (isPlainObject(sourceValue) && isPlainObject(output[key])) {
      output[key] = deepMerge(output[key], sourceValue);
      return;
    }
    output[key] = sourceValue;
  });
  return output;
};

const normalizeConfiguration = (configuration?: SettingsRecord): SettingsRecord =>
  deepMerge(DEFAULT_CONFIGURATION, configuration ?? {});

const stringifyJsonObject = (value: any) => JSON.stringify(value ?? {}, null, 2);

const removePropertyRecursive = (value: any, propertyName: string): any => {
  if (Array.isArray(value)) return value.map((item) => removePropertyRecursive(item, propertyName));
  if (!isPlainObject(value)) return value;

  const output: SettingsRecord = {};
  Object.keys(value).forEach((key) => {
    if (key === propertyName) return;
    output[key] = removePropertyRecursive(value[key], propertyName);
  });
  return output;
};

const nullsToUndefinedRecursive = (value: any): any => {
  if (value === null) return undefined;
  if (Array.isArray(value)) return value.map(nullsToUndefinedRecursive);
  if (!isPlainObject(value)) return value;

  const output: SettingsRecord = {};
  Object.keys(value).forEach((key) => {
    output[key] = nullsToUndefinedRecursive(value[key]);
  });
  return output;
};

const getErrorMessage = (error: any, fallback: string) =>
  error?.data?.message || error?.message || fallback;

const isPathRestartRequired = (path: string, restartRequiredSettings: string[]) =>
  restartRequiredSettings.some(
    (setting) => path === setting || path.startsWith(`${setting}.`) || setting.startsWith(`${path}.`)
  );

const MaintenancePage: React.FC = () => {
  const [form] = Form.useForm<MaintenanceFormValues>();
  const { data: status, isLoading: statusLoading, refetch: refetchStatus } = useGetMaintenanceStatusQuery();
  const { data: health, isLoading: healthLoading, refetch: refetchHealth } = useGetAdminHealthQuery();
  const [updateSettings, { isLoading: isSaving }] = useUpdateMaintenanceSettingsMutation();
  const [runAction, { isLoading: isRunning }] = useRunMaintenanceActionMutation();

  const restartRequiredSettings = status?.RestartRequiredSettings ?? [];

  useEffect(() => {
    if (!status) return;

    const configuration = normalizeConfiguration(status.Configuration);
    const formConfiguration = nullsToUndefinedRecursive(configuration);
    form.setFieldsValue({
      ...formConfiguration,
      __json: {
        WebserverHeadersDefaultHeaders: stringifyJsonObject(configuration.Webserver?.Headers?.DefaultHeaders),
        WebserverAccessControlDenyList: stringifyJsonObject(configuration.Webserver?.AccessControl?.DenyList),
        WebserverAccessControlPermitList: stringifyJsonObject(configuration.Webserver?.AccessControl?.PermitList),
        WebserverSslSslCertificate: JSON.stringify(configuration.Webserver?.Ssl?.SslCertificate ?? null, null, 2),
      },
    });
  }, [form, status]);

  const refresh = () => {
    refetchStatus();
    refetchHealth();
  };

  const labelWithRestart = (path: string, label: string) => (
    <Less3Flex align="center" gap={6} wrap="wrap">
      <span>{label}</span>
      {isPathRestartRequired(path, restartRequiredSettings) && (
        <Tag color="warning" style={{ marginInlineEnd: 0 }}>Restart required</Tag>
      )}
    </Less3Flex>
  );

  const parseJsonObjectField = (values: MaintenanceFormValues, key: string, label: string) => {
    const rawValue = values.__json?.[key] ?? '{}';
    try {
      const parsed = rawValue.trim() ? JSON.parse(rawValue) : {};
      if (!isPlainObject(parsed)) throw new Error(`${label} must be a JSON object`);
      return parsed;
    } catch (error: any) {
      const errorMessage = error?.message || `${label} must be valid JSON`;
      form.setFields([{ name: ['__json', key] as any, errors: [errorMessage] }]);
      throw new Error(errorMessage);
    }
  };

  const parseJsonValueField = (values: MaintenanceFormValues, key: string, label: string) => {
    const rawValue = values.__json?.[key] ?? 'null';
    try {
      return rawValue.trim() ? JSON.parse(rawValue) : null;
    } catch (error: any) {
      const errorMessage = error?.message || `${label} must be valid JSON`;
      form.setFields([{ name: ['__json', key] as any, errors: [errorMessage] }]);
      throw new Error(errorMessage);
    }
  };

  const buildConfigurationPayload = (values: MaintenanceFormValues) => {
    const { __json, ...settingValues } = values;
    const configuration = removePropertyRecursive(
      deepMerge(normalizeConfiguration(status?.Configuration), settingValues),
      'RequiresRestart'
    );

    configuration.Webserver = configuration.Webserver ?? {};
    configuration.Webserver.Ssl = configuration.Webserver.Ssl ?? {};
    configuration.Webserver.Headers = configuration.Webserver.Headers ?? {};
    configuration.Webserver.AccessControl = configuration.Webserver.AccessControl ?? {};
    configuration.Webserver.Ssl.SslCertificate = parseJsonValueField(
      { __json },
      'WebserverSslSslCertificate',
      'SSL Certificate'
    );
    configuration.Webserver.Headers.DefaultHeaders = parseJsonObjectField(
      { __json },
      'WebserverHeadersDefaultHeaders',
      'Default headers'
    );
    configuration.Webserver.AccessControl.DenyList = parseJsonObjectField(
      { __json },
      'WebserverAccessControlDenyList',
      'Deny list'
    );
    configuration.Webserver.AccessControl.PermitList = parseJsonObjectField(
      { __json },
      'WebserverAccessControlPermitList',
      'Permit list'
    );

    return configuration;
  };

  const saveSettings = async () => {
    try {
      const values = await form.validateFields();
      const configuration = buildConfigurationPayload(values);
      await updateSettings({ Configuration: configuration }).unwrap();
      message.success('Maintenance settings updated');
      refresh();
    } catch (error: any) {
      if (error?.errorFields) return;
      message.error(getErrorMessage(error, 'Failed to update maintenance settings'));
    }
  };

  const runMaintenanceAction = async (action: string, successMessage: string) => {
    try {
      await runAction({ action }).unwrap();
      message.success(successMessage);
      refresh();
    } catch (error: any) {
      message.error(getErrorMessage(error, 'Maintenance action failed'));
    }
  };

  const renderTextField = (name: (string | number)[], path: string, label: string, required = false) => (
    <Less3FormItem
      label={labelWithRestart(path, label)}
      name={name}
      rules={required ? [{ required: true, message: `${label} is required` }] : undefined}
    >
      <Less3Input />
    </Less3FormItem>
  );

  const renderPasswordField = (name: (string | number)[], path: string, label: string, required = false) => (
    <Less3FormItem
      label={labelWithRestart(path, label)}
      name={name}
      rules={required ? [{ required: true, message: `${label} is required` }] : undefined}
    >
      <Input.Password />
    </Less3FormItem>
  );

  const renderNumberField = (
    name: (string | number)[],
    path: string,
    label: string,
    min = 0,
    step = 1,
    required = true
  ) => (
    <Less3FormItem
      label={labelWithRestart(path, label)}
      name={name}
      rules={[{ required, message: `${label} is required` }]}
    >
      <InputNumber min={min} step={step} style={selectStyle} />
    </Less3FormItem>
  );

  const renderSwitchField = (name: (string | number)[], path: string, label: string) => (
    <Less3FormItem label={labelWithRestart(path, label)} name={name} valuePropName="checked">
      <Switch />
    </Less3FormItem>
  );

  return (
    <PageContainer
      pageTitle="Maintenance"
      pageTitleRightContent={
        <Less3Flex gap={8} wrap="wrap">
          <Less3Button icon={<ReloadOutlined />} loading={statusLoading || healthLoading} onClick={refresh}>
            Refresh
          </Less3Button>
          <Less3Button type="primary" icon={<SaveOutlined />} loading={isSaving} onClick={saveSettings}>
            Save Settings
          </Less3Button>
        </Less3Flex>
      }
    >
      <Less3Flex vertical gap={18}>
        <Form form={form} layout="vertical" autoComplete="off" disabled={statusLoading}>
          <Less3Flex vertical gap={18}>
            <Less3Card>
              <Less3Flex vertical gap={16}>
                <Less3Text weight={600} fontSize={16}>Core Settings</Less3Text>
                <div style={gridStyle}>
                  {renderSwitchField(['EnableConsole'], 'EnableConsole', 'Enable Console')}
                  {renderSwitchField(['ValidateSignatures'], 'ValidateSignatures', 'Validate Signatures')}
                  {renderTextField(['BaseDomain'], 'BaseDomain', 'Base Domain')}
                  {renderTextField(['HeaderApiKey'], 'HeaderApiKey', 'Header API Key', true)}
                  {renderPasswordField(['AdminApiKey'], 'AdminApiKey', 'Admin API Key', true)}
                  {renderTextField(['RegionString'], 'RegionString', 'Region', true)}
                  {renderNumberField(['RequestHistoryRetentionDays'], 'RequestHistoryRetentionDays', 'Request History Retention Days', 1)}
                  {renderNumberField(['CleanupIntervalMs'], 'CleanupIntervalMs', 'Cleanup Interval Ms', 60000, 1000)}
                </div>
              </Less3Flex>
            </Less3Card>

            <Less3Card>
              <Less3Flex vertical gap={16}>
                <Less3Text weight={600} fontSize={16}>Database</Less3Text>
                <div style={gridStyle}>
                  <Less3FormItem
                    label={labelWithRestart('Database.Type', 'Type')}
                    name={['Database', 'Type']}
                    rules={[{ required: true, message: 'Type is required' }]}
                  >
                    <Less3Select
                      options={[
                        { label: 'Sqlite', value: 'Sqlite' },
                        { label: 'SqlServer', value: 'SqlServer' },
                        { label: 'Mysql', value: 'Mysql' },
                        { label: 'Postgresql', value: 'Postgresql' },
                      ]}
                      style={selectStyle}
                    />
                  </Less3FormItem>
                  {renderTextField(['Database', 'Filename'], 'Database.Filename', 'Filename')}
                  {renderTextField(['Database', 'Hostname'], 'Database.Hostname', 'Hostname')}
                  {renderNumberField(['Database', 'Port'], 'Database.Port', 'Port', 0, 1, false)}
                  {renderTextField(['Database', 'Username'], 'Database.Username', 'Username')}
                  {renderPasswordField(['Database', 'Password'], 'Database.Password', 'Password')}
                  {renderTextField(['Database', 'Instance'], 'Database.Instance', 'Instance')}
                  {renderTextField(['Database', 'DatabaseName'], 'Database.DatabaseName', 'Database Name')}
                  {renderSwitchField(['Database', 'RequireEncryption'], 'Database.RequireEncryption', 'Require Encryption')}
                  {renderSwitchField(['Database', 'LogQueries'], 'Database.LogQueries', 'Log Queries')}
                </div>
              </Less3Flex>
            </Less3Card>

            <Less3Card>
              <Less3Flex vertical gap={16}>
                <Less3Text weight={600} fontSize={16}>Webserver</Less3Text>
                <div style={gridStyle}>
                  {renderTextField(['Webserver', 'Hostname'], 'Webserver.Hostname', 'Hostname', true)}
                  {renderNumberField(['Webserver', 'Port'], 'Webserver.Port', 'Port', 1)}
                  {renderSwitchField(['Webserver', 'UseMachineHostname'], 'Webserver.UseMachineHostname', 'Use Machine Hostname')}
                </div>

                <Less3Text weight={600}>IO</Less3Text>
                <div style={gridStyle}>
                  {renderNumberField(['Webserver', 'IO', 'StreamBufferSize'], 'Webserver.IO.StreamBufferSize', 'Stream Buffer Size', 1)}
                  {renderNumberField(['Webserver', 'IO', 'MaxRequests'], 'Webserver.IO.MaxRequests', 'Max Requests', 0)}
                  {renderNumberField(['Webserver', 'IO', 'ReadTimeoutMs'], 'Webserver.IO.ReadTimeoutMs', 'Read Timeout Ms', 0, 1000)}
                  {renderNumberField(['Webserver', 'IO', 'MaxIncomingHeadersSize'], 'Webserver.IO.MaxIncomingHeadersSize', 'Max Incoming Headers Size', 0)}
                  {renderNumberField(['Webserver', 'IO', 'MaxRequestBodySize'], 'Webserver.IO.MaxRequestBodySize', 'Max Request Body Size', 0)}
                  {renderNumberField(['Webserver', 'IO', 'MaxHeaderCount'], 'Webserver.IO.MaxHeaderCount', 'Max Header Count', 0)}
                  {renderSwitchField(['Webserver', 'IO', 'EnableKeepAlive'], 'Webserver.IO.EnableKeepAlive', 'Enable Keep Alive')}
                </div>

                <Less3Text weight={600}>SSL</Less3Text>
                <div style={gridStyle}>
                  {renderSwitchField(['Webserver', 'Ssl', 'Enable'], 'Webserver.Ssl.Enable', 'Enable SSL')}
                  {renderTextField(['Webserver', 'Ssl', 'PfxCertificateFile'], 'Webserver.Ssl.PfxCertificateFile', 'PFX Certificate File')}
                  {renderPasswordField(['Webserver', 'Ssl', 'PfxCertificatePassword'], 'Webserver.Ssl.PfxCertificatePassword', 'PFX Certificate Password')}
                  <Less3FormItem
                    label={labelWithRestart('Webserver.Ssl.SslCertificate', 'SSL Certificate')}
                    name={['__json', 'WebserverSslSslCertificate']}
                    style={fullWidthGridStyle}
                    rules={[{ required: true, message: 'SSL Certificate is required' }]}
                  >
                    <Input.TextArea rows={3} spellCheck={false} />
                  </Less3FormItem>
                  {renderSwitchField(['Webserver', 'Ssl', 'MutuallyAuthenticate'], 'Webserver.Ssl.MutuallyAuthenticate', 'Mutually Authenticate')}
                  {renderSwitchField(['Webserver', 'Ssl', 'AcceptInvalidAcertificates'], 'Webserver.Ssl.AcceptInvalidAcertificates', 'Accept Invalid Certificates')}
                </div>

                <Less3Text weight={600}>Headers</Less3Text>
                <div style={gridStyle}>
                  {renderSwitchField(['Webserver', 'Headers', 'IncludeContentLength'], 'Webserver.Headers.IncludeContentLength', 'Include Content Length')}
                  <Less3FormItem
                    label={labelWithRestart('Webserver.Headers.DefaultHeaders', 'Default Headers')}
                    name={['__json', 'WebserverHeadersDefaultHeaders']}
                    style={fullWidthGridStyle}
                    rules={[{ required: true, message: 'Default Headers is required' }]}
                  >
                    <Input.TextArea rows={5} spellCheck={false} />
                  </Less3FormItem>
                </div>

                <Less3Text weight={600}>Access Control</Less3Text>
                <div style={gridStyle}>
                  <Less3FormItem
                    label={labelWithRestart('Webserver.AccessControl.Mode', 'Mode')}
                    name={['Webserver', 'AccessControl', 'Mode']}
                    rules={[{ required: true, message: 'Mode is required' }]}
                  >
                    <Less3Select
                      options={[
                        { label: 'DefaultPermit', value: 'DefaultPermit' },
                        { label: 'DefaultDeny', value: 'DefaultDeny' },
                      ]}
                      style={selectStyle}
                    />
                  </Less3FormItem>
                  <Less3FormItem
                    label={labelWithRestart('Webserver.AccessControl.DenyList', 'Deny List')}
                    name={['__json', 'WebserverAccessControlDenyList']}
                    style={fullWidthGridStyle}
                    rules={[{ required: true, message: 'Deny List is required' }]}
                  >
                    <Input.TextArea rows={5} spellCheck={false} />
                  </Less3FormItem>
                  <Less3FormItem
                    label={labelWithRestart('Webserver.AccessControl.PermitList', 'Permit List')}
                    name={['__json', 'WebserverAccessControlPermitList']}
                    style={fullWidthGridStyle}
                    rules={[{ required: true, message: 'Permit List is required' }]}
                  >
                    <Input.TextArea rows={5} spellCheck={false} />
                  </Less3FormItem>
                </div>

                <Less3Text weight={600}>Webserver Debug</Less3Text>
                <div style={gridStyle}>
                  {renderSwitchField(['Webserver', 'Debug', 'AccessControl'], 'Webserver.Debug.AccessControl', 'Access Control')}
                  {renderSwitchField(['Webserver', 'Debug', 'Routing'], 'Webserver.Debug.Routing', 'Routing')}
                  {renderSwitchField(['Webserver', 'Debug', 'Requests'], 'Webserver.Debug.Requests', 'Requests')}
                  {renderSwitchField(['Webserver', 'Debug', 'Responses'], 'Webserver.Debug.Responses', 'Responses')}
                </div>
              </Less3Flex>
            </Less3Card>

            <Less3Card>
              <Less3Flex vertical gap={16}>
                <Less3Text weight={600} fontSize={16}>Storage</Less3Text>
                <div style={gridStyle}>
                  {renderTextField(['Storage', 'TempDirectory'], 'Storage.TempDirectory', 'Temp Directory', true)}
                  <Less3FormItem
                    label={labelWithRestart('Storage.StorageType', 'Storage Type')}
                    name={['Storage', 'StorageType']}
                    rules={[{ required: true, message: 'Storage Type is required' }]}
                  >
                    <Less3Select options={[{ label: 'Disk', value: 'Disk' }]} style={selectStyle} />
                  </Less3FormItem>
                  {renderTextField(['Storage', 'DiskDirectory'], 'Storage.DiskDirectory', 'Disk Directory', true)}
                </div>
              </Less3Flex>
            </Less3Card>

            <Less3Card>
              <Less3Flex vertical gap={16}>
                <Less3Text weight={600} fontSize={16}>Logging</Less3Text>
                <div style={gridStyle}>
                  {renderTextField(['Logging', 'SyslogServerIp'], 'Logging.SyslogServerIp', 'Syslog Server IP', true)}
                  {renderNumberField(['Logging', 'SyslogServerPort'], 'Logging.SyslogServerPort', 'Syslog Server Port', 0)}
                  <Less3FormItem
                    label={labelWithRestart('Logging.MinimumLevel', 'Minimum Level')}
                    name={['Logging', 'MinimumLevel']}
                    rules={[{ required: true, message: 'Minimum Level is required' }]}
                  >
                    <Less3Select
                      options={[
                        { label: 'Debug', value: 'Debug' },
                        { label: 'Info', value: 'Info' },
                        { label: 'Warn', value: 'Warn' },
                        { label: 'Error', value: 'Error' },
                        { label: 'Alert', value: 'Alert' },
                        { label: 'Critical', value: 'Critical' },
                        { label: 'Emergency', value: 'Emergency' },
                      ]}
                      style={selectStyle}
                    />
                  </Less3FormItem>
                  {renderSwitchField(['Logging', 'LogHttpRequests'], 'Logging.LogHttpRequests', 'Log HTTP Requests')}
                  {renderSwitchField(['Logging', 'LogS3Requests'], 'Logging.LogS3Requests', 'Log S3 Requests')}
                  {renderSwitchField(['Logging', 'LogExceptions'], 'Logging.LogExceptions', 'Log Exceptions')}
                  {renderSwitchField(['Logging', 'LogSignatureValidation'], 'Logging.LogSignatureValidation', 'Log Signature Validation')}
                  {renderSwitchField(['Logging', 'ConsoleLogging'], 'Logging.ConsoleLogging', 'Console Logging')}
                  {renderSwitchField(['Logging', 'DiskLogging'], 'Logging.DiskLogging', 'Disk Logging')}
                  {renderTextField(['Logging', 'DiskDirectory'], 'Logging.DiskDirectory', 'Disk Directory', true)}
                </div>
              </Less3Flex>
            </Less3Card>

            <Less3Card>
              <Less3Flex vertical gap={16}>
                <Less3Text weight={600} fontSize={16}>Debug</Less3Text>
                <div style={gridStyle}>
                  {renderSwitchField(['Debug', 'Authentication'], 'Debug.Authentication', 'Authentication')}
                  {renderSwitchField(['Debug', 'S3Requests'], 'Debug.S3Requests', 'S3 Requests')}
                  {renderSwitchField(['Debug', 'Exceptions'], 'Debug.Exceptions', 'Exceptions')}
                </div>
                <Less3Button type="primary" icon={<SaveOutlined />} loading={isSaving} onClick={saveSettings}>
                  Save Settings
                </Less3Button>
              </Less3Flex>
            </Less3Card>
          </Less3Flex>
        </Form>

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
                {restartRequiredSettings.map((item) => <Tag key={item} color="warning">{item}</Tag>)}
              </Descriptions.Item>
            </Descriptions>
          </Less3Flex>
        </Less3Card>
      </Less3Flex>
    </PageContainer>
  );
};

export default MaintenancePage;
