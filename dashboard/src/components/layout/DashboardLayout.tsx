import React, { useState, useEffect } from 'react';
import { Layout } from 'antd';
import { LogoutOutlined } from '@ant-design/icons';
import styles from './dashboard.module.scss';
import Less3Flex from '../base/flex/Flex';
import Less3Button from '../base/button/Button';
import Less3Text from '../base/typograpghy/Text';
import ErrorBoundary from '#/hoc/ErrorBoundary';
import ThemeModeSwitch from '../theme-mode-switch/ThemeModeSwitch';
import { useRouter } from 'next/navigation';
import { paths, localStorageKeys } from '#/constants/constant';
import Sidebar from '../base/sidebar';
import Less3Logo from '#/components/logo/Logo';
import { getApiEndpoint } from '#/services/sdk.service';

const { Header, Content } = Layout;

interface LayoutWrapperProps {
  children: React.ReactNode;
}

const DashboardLayout = ({ children }: LayoutWrapperProps) => {
  const [collapsed, setCollapsed] = useState(false);
  const [serverUrl, setServerUrl] = useState<string>('');
  const router = useRouter();

  useEffect(() => {
    setServerUrl(getApiEndpoint());
  }, []);

  const handleLogout = () => {
    localStorage.removeItem(localStorageKeys.less3APIUrl);
    router.push(paths.login);
  };

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header className={styles.topHeader}>
        <Less3Flex align="center" gap={16}>
          <Less3Logo showOnlyIcon={false} size={16} imageSize={32} />
        </Less3Flex>
        <Less3Flex align="center" gap={16} style={{ flex: 1, justifyContent: 'center' }}>
          <Less3Text className={styles.serverUrl}>
            Server: <span className={styles.serverUrlValue}>{serverUrl}</span>
          </Less3Text>
        </Less3Flex>
        <Less3Flex gap={16} align="center">
          <ThemeModeSwitch />
          <Less3Button
            type="text"
            icon={<LogoutOutlined />}
            onClick={handleLogout}
            className={styles.logoutButton}
          >
            Logout
          </Less3Button>
        </Less3Flex>
      </Header>
      <Layout style={{ marginTop: 65 }}>
        <Sidebar collapsed={collapsed} onCollapse={setCollapsed} showLogo={false} />
        <Layout
          style={{
            marginLeft: collapsed ? 60 : 200,
            transition: 'margin-left 0.2s',
            minHeight: 'calc(100vh - 65px)',
            width: '100%',
          }}
        >
          <Content
            style={{
              minHeight: 280,
              background: '#fff',
            }}
          >
            <ErrorBoundary>{children}</ErrorBoundary>
          </Content>
        </Layout>
      </Layout>
    </Layout>
  );
};

export default DashboardLayout;
