'use client';

import React, { useEffect, useState } from 'react';
import LoginLayout from '#/components/layout/LoginLayout';
import styles from './login-page.module.scss';
import Less3Logo from '#/components/logo/Logo';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Text from '#/components/base/typograpghy/Text';
import Less3Divider from '#/components/base/divider/Divider';
import PageLoading from '#/components/base/loading/PageLoading';
import { Form, Input, message } from 'antd';
import { ArrowRightOutlined, CheckCircleOutlined, CloseCircleOutlined } from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import { apiEndpointURL } from '#/constants/config';
import { useValidateConnectivityMutation } from '#/store/slice/sdkSlice';
import { updateSdkEndPoint } from '#/services/sdk.service';
import { localStorageKeys, paths } from '#/constants/constant';

//eslint-disable-next-line max-lines-per-function
const LoginPage = () => {
  const [loading, setLoading] = useState(false);
  const [documentAtomAPIUrl, setLess3APIUrl] = useState(apiEndpointURL);
  const [form] = Form.useForm();
  const router = useRouter();
  const [isFormSubmitted, setIsFormSubmitted] = useState(false);
  const [hasValidated, setHasValidated] = useState(false);
  const [isSuccess, setIsSuccess] = useState(false);
  const [isError, setIsError] = useState(false);
  const [validateConnectivityMutation] = useValidateConnectivityMutation();

  useEffect(() => {
    // Check if there's a saved URL in localStorage
    const savedUrl = localStorage.getItem(localStorageKeys.documentAtomAPIUrl);
    if (savedUrl) {
      setLess3APIUrl(savedUrl);
      form.setFieldsValue({ documentAtomAPIUrl: savedUrl });
      // Optionally validate connectivity on mount
      validateConnectivity(savedUrl, false);
    }
  }, [form]);

  const validateConnectivity = async (newURL: string, navigate: boolean = false) => {
    updateSdkEndPoint(newURL);
    setLoading(true);
    setHasValidated(true);
    try {
      const response = await validateConnectivityMutation().unwrap();
      if (response) {
        setIsSuccess(true);
        setIsError(false);
        message.success('Connected successfully!');
        localStorage.setItem(localStorageKeys.documentAtomAPIUrl, newURL);
        if (navigate) {
          router.push(paths.dashboard);
        }
      } else {
        setIsSuccess(false);
        setIsError(true);
        message.error('Unable to connect to Less3 services');
      }
    } catch (err) {
      console.log(err);
      setIsSuccess(false);
      setIsError(true);
      message.error('Something went wrong.');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async () => {
    const values = await form.validateFields();
    setLoading(true);
    setIsFormSubmitted(true);
    const newURL = values.documentAtomAPIUrl;
    if (newURL) {
      await validateConnectivity(newURL, true);
    } else {
      message.error('Something went wrong.');
      setHasValidated(false);
    }
  };

  return (
    <LoginLayout>
      <Less3Flex justify="center" align="center" vertical className={styles.loginBox}>
        <Less3Logo imageSize={50} showOnlyIcon />
        <Less3Divider />
        <Form
          initialValues={{ documentAtomAPIUrl }}
          layout="vertical"
          form={form}
          onFinish={handleSubmit}
          requiredMark={false}
          style={{ width: 'fit-content' }}
        >
          <Less3Flex align="center" gap={0}>
            <Form.Item
              label="Less3 Server URL"
              name="documentAtomAPIUrl"
              rules={[
                {
                  required: true,
                  message: 'Please enter a valid Less3 URL',
                },
              ]}
              required
              style={{ width: '400px' }}
            >
              <Input.Search
                size="large"
                loading={loading}
                autoFocus
                disabled={loading}
                value={documentAtomAPIUrl}
                onChange={(e: any) => setLess3APIUrl(e.target.value)}
                enterButton={<ArrowRightOutlined />}
                onSearch={handleSubmit}
                placeholder="https://your-documentatom-server.com"
              />
            </Form.Item>
          </Less3Flex>
        </Form>

        {loading && <PageLoading message="Connecting..." />}

        {isSuccess && hasValidated && !loading && !isFormSubmitted && (
          <Less3Text className="text-color-success mt">
            <CheckCircleOutlined /> Your Less3 node is operational.
          </Less3Text>
        )}

        {isError && hasValidated && !loading && (
          <Less3Text className="text-color-error mt">
            <CloseCircleOutlined /> Unable to connect to Less3 services
          </Less3Text>
        )}
      </Less3Flex>
    </LoginLayout>
  );
};

export default LoginPage;
