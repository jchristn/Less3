import React, { useEffect, useState } from 'react';
import { Layout } from 'antd';
import { LogoutOutlined, GithubOutlined } from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import CopyToClipboard from '#/components/copy-to-clipboard/CopyToClipboard';
import Less3Logo from '#/components/logo/Logo';
import { apiEndpointURL } from '#/constants/config';
import { localStorageKeys, paths } from '#/constants/constant';
import ErrorBoundary from '#/hoc/ErrorBoundary';
import { getApiEndpoint, updateSdkEndPoint } from '#/services/sdk.service';
import Less3Button from '../base/button/Button';
import Less3Flex from '../base/flex/Flex';
import Sidebar from '../base/sidebar';
import Less3Tooltip from '../base/tooltip/Tooltip';
import ThemeModeSwitch from '../theme-mode-switch/ThemeModeSwitch';
import styles from './dashboard.module.scss';

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
    updateSdkEndPoint(apiEndpointURL);
    router.push(paths.login);
  };

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header className={styles.topHeader}>
        <Less3Flex align="center" gap={16}>
          <Less3Logo showOnlyIcon={false} size={16} imageSize={32} />
        </Less3Flex>
        <div style={{ flex: 1, display: 'flex', justifyContent: 'center', alignItems: 'center', alignSelf: 'center' }}>
          <span className={styles.serverUrlBadge}>
            <span className={styles.serverUrlValue}>{serverUrl}</span>
            <CopyToClipboard
              text={serverUrl}
              tooltip="Copy URL"
              copiedTooltip="Copied!"
              ariaLabel="Copy server URL"
              placement="bottom"
              iconSize={12}
              buttonSize={20}
            />
          </span>
        </div>
        <Less3Flex gap={16} align="center">
          <Less3Tooltip title="GitHub" placement="bottom">
            <Less3Button
              type="text"
              icon={<GithubOutlined />}
              onClick={() => window.open('https://github.com/jchristn/less3', '_blank')}
              className={styles.logoutButton}
            />
          </Less3Tooltip>
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
          }}
        >
          <Content
            style={{
              minHeight: 280,
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
