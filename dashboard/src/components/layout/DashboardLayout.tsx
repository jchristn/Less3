import React, { useState, useEffect } from 'react';
import { Layout } from 'antd';
import { LogoutOutlined, CheckOutlined, GithubOutlined } from '@ant-design/icons';
import styles from './dashboard.module.scss';
import Less3Flex from '../base/flex/Flex';
import Less3Button from '../base/button/Button';
import Less3Tooltip from '../base/tooltip/Tooltip';
import ErrorBoundary from '#/hoc/ErrorBoundary';
import ThemeModeSwitch from '../theme-mode-switch/ThemeModeSwitch';
import { useRouter } from 'next/navigation';
import { paths, localStorageKeys } from '#/constants/constant';
import Sidebar from '../base/sidebar';
import Less3Logo from '#/components/logo/Logo';
import { getApiEndpoint } from '#/services/sdk.service';
import { copyToClipboard } from '#/utils/clipboardUtils';

const { Header, Content } = Layout;

interface LayoutWrapperProps {
  children: React.ReactNode;
}

const CopyIcon = () => (
  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <rect x="9" y="9" width="13" height="13" rx="2" ry="2" />
    <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
  </svg>
);

const DashboardLayout = ({ children }: LayoutWrapperProps) => {
  const [collapsed, setCollapsed] = useState(false);
  const [serverUrl, setServerUrl] = useState<string>('');
  const [isCopied, setIsCopied] = useState(false);
  const router = useRouter();

  useEffect(() => {
    setServerUrl(getApiEndpoint());
  }, []);

  const handleLogout = () => {
    localStorage.removeItem(localStorageKeys.less3APIUrl);
    router.push(paths.login);
  };

  const handleCopyUrl = () => {
    copyToClipboard(serverUrl);
    setIsCopied(true);
    setTimeout(() => {
      setIsCopied(false);
    }, 2000);
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
            <Less3Tooltip title={isCopied ? 'Copied!' : 'Copy URL'} placement="bottom">
              <span className={styles.serverUrlCopy} onClick={handleCopyUrl}>
                {isCopied ? <CheckOutlined style={{ fontSize: 12 }} /> : <CopyIcon />}
              </span>
            </Less3Tooltip>
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
