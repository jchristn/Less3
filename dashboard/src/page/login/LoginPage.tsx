'use client';

import React, { useEffect, useState } from 'react';
import LoginLayout from '#/components/layout/LoginLayout';
import styles from './login-page.module.scss';
import Less3Logo from '#/components/logo/Logo';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Button from '#/components/base/button/Button';
import Less3Text from '#/components/base/typograpghy/Text';
import Less3Title from '#/components/base/typograpghy/Title';
import { Alert, Checkbox, Form, Input } from 'antd';
import { ArrowRightOutlined, KeyOutlined, LinkOutlined } from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import { useValidateConnectivityMutation } from '#/store/slice/sdkSlice';
import {
  getInitialAdminApiKey,
  getInitialApiEndpoint,
  getInitialRememberAdminApiKey,
  persistDashboardSession,
} from '#/services/sdk.service';
import { paths } from '#/constants/constant';
import { message } from '#/utils/message';

const SERVER_CONNECTION_ERROR_MESSAGE = 'Unable to connect to specified server.';

const extractErrorMessage = (value: unknown): string | null => {
  if (typeof value === 'string' && value.trim()) {
    return value;
  }

  if (!value || typeof value !== 'object') {
    return null;
  }

  const record = value as Record<string, unknown>;

  return (
    extractErrorMessage(record.Message) ||
    extractErrorMessage(record.Description) ||
    extractErrorMessage(record.message) ||
    extractErrorMessage(record.error) ||
    extractErrorMessage(record.data)
  );
};

const getErrorMessage = (error: unknown): string => {
  const errorMessage = extractErrorMessage(error);

  if (!errorMessage) {
    return 'Something went wrong.';
  }

  if (errorMessage === 'Failed to fetch') {
    return SERVER_CONNECTION_ERROR_MESSAGE;
  }

  return errorMessage;
};

const validateUrl = (_rule: unknown, value: string) => {
  if (!value?.trim()) {
    return Promise.reject(new Error('Please enter your Less3 server URL.'));
  }

  try {
    new URL(value);
    return Promise.resolve();
  } catch {
    return Promise.reject(new Error('Enter a full URL including http:// or https://.'));
  }
};

type ValidationState =
  | {
      type: 'info' | 'success' | 'error';
      message: string;
    }
  | null;

//eslint-disable-next-line max-lines-per-function
const LoginPage = () => {
  const [loading, setLoading] = useState(false);
  const [form] = Form.useForm();
  const router = useRouter();
  const [validationState, setValidationState] = useState<ValidationState>(null);
  const [validateConnectivityMutation] = useValidateConnectivityMutation();

  const validateConnectivity = async (
    newURL: string,
    newApiKey: string,
    options?: {
      navigate?: boolean;
      restoring?: boolean;
    }
  ) => {
    const navigate = options?.navigate ?? false;
    const restoring = options?.restoring ?? false;

    setLoading(true);
    setValidationState({
      type: 'info',
      message: restoring ? 'Restoring your previous dashboard session...' : 'Validating admin API access...',
    });

    try {
      const response = await validateConnectivityMutation({
        endpoint: newURL,
        apiKey: newApiKey,
      }).unwrap();

      if (response) {
        persistDashboardSession(newURL, newApiKey, {
          rememberAdminKey: form.getFieldValue('rememberAdminKey') ?? true,
        });
        setValidationState({
          type: 'success',
          message: 'Authenticated against the Less3 admin API.',
        });

        if (!restoring) {
          message.success('Connected successfully!');
        }

        if (navigate) {
          router.push(paths.dashboard);
        }
      } else {
        setValidationState({
          type: 'error',
          message: 'Unable to connect to Less3 services.',
        });
        if (!restoring) {
          message.error('Unable to connect to Less3 services');
        }
      }
    } catch (err) {
      const errorMessage = getErrorMessage(err);
      setValidationState({
        type: 'error',
        message: errorMessage,
      });

      if (!restoring) {
        message.error(errorMessage);
      } else {
        persistDashboardSession(newURL, '');
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const initialUrl = getInitialApiEndpoint();
    const initialApiKey = getInitialAdminApiKey();

    form.setFieldsValue({
      less3APIUrl: initialUrl,
      adminApiKey: initialApiKey,
      rememberAdminKey: getInitialRememberAdminApiKey(),
    });

    if (initialUrl && initialApiKey) {
      void validateConnectivity(initialUrl, initialApiKey, {
        navigate: true,
        restoring: true,
      });
    }
  }, [form]);

  const handleSubmit = async () => {
    const values = await form.validateFields();
    await validateConnectivity(values.less3APIUrl, values.adminApiKey, { navigate: true });
  };

  return (
    <LoginLayout>
      <div className={styles.loginShell}>
        <section className={styles.formPanel}>
          <div className={styles.formHeader}>
            <div className={styles.brandBadge}>
              <Less3Logo imageSize={32} showOnlyIcon />
              <span>Less3 Dashboard</span>
            </div>
            <Less3Title level={3} className={styles.formTitle}>
              Admin Sign In
            </Less3Title>
            <Less3Text className={styles.formDescription}>
              Use the same API key configured on the server to unlock dashboard access.
            </Less3Text>
          </div>

          <Form
            layout="vertical"
            form={form}
            onFinish={handleSubmit}
            requiredMark={false}
            className={styles.form}
          >
            <Form.Item
              label="Less3 Server URL"
              name="less3APIUrl"
              rules={[
                {
                  validator: validateUrl,
                },
              ]}
            >
              <Input
                size="large"
                autoFocus
                disabled={loading}
                prefix={<LinkOutlined className={styles.fieldIcon} />}
                placeholder="https://your-less3-server.com"
              />
            </Form.Item>

            <Form.Item
              label="Admin API Key"
              name="adminApiKey"
              extra="Use the AdminApiKey value from the Less3 server's system.json file."
              rules={[
                {
                  required: true,
                  message: 'Please enter the Less3 admin API key.',
                },
              ]}
            >
              <Input.Password
                size="large"
                disabled={loading}
                prefix={<KeyOutlined className={styles.fieldIcon} />}
                placeholder="Paste the server admin API key"
                autoComplete="current-password"
              />
            </Form.Item>

            <Form.Item name="rememberAdminKey" valuePropName="checked">
              <Checkbox disabled={loading}>Remember key on this device</Checkbox>
            </Form.Item>

            {validationState && (
              <Alert
                type={validationState.type}
                showIcon
                message={validationState.message}
                className={styles.validationAlert}
              />
            )}

            <Less3Button
              htmlType="submit"
              type="primary"
              size="large"
              block
              loading={loading}
              icon={<ArrowRightOutlined />}
              className={styles.submitButton}
            >
              Sign In to Dashboard
            </Less3Button>
          </Form>

          <Less3Flex className={styles.formFooter} justify="space-between" align="center" gap={12}>
            <Less3Text className={styles.footerNote}>
              Use the exact URL and key served by the target Less3 instance.
            </Less3Text>
          </Less3Flex>
        </section>
      </div>
    </LoginLayout>
  );
};

export default LoginPage;
