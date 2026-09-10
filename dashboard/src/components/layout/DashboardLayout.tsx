import React, { useEffect, useState } from 'react';
import { Layout } from 'antd';
import { LogoutOutlined, GithubOutlined } from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import CopyToClipboard from '#/components/copy-to-clipboard/CopyToClipboard';
import Less3Logo from '#/components/logo/Logo';
import { paths } from '#/constants/constant';
import ErrorBoundary from '#/hoc/ErrorBoundary';
import { clearDashboardSession, getApiEndpoint } from '#/services/sdk.service';
import Less3Button from '../base/button/Button';
import Less3Flex from '../base/flex/Flex';
import Sidebar from '../base/sidebar';
import Less3Tooltip from '../base/tooltip/Tooltip';
import ThemeModeSwitch from '../theme-mode-switch/ThemeModeSwitch';
import styles from './dashboard.module.scss';

const { Header, Content } = Layout;
const dashboardVersion = process.env.NEXT_PUBLIC_LESS3_VERSION || '4.0.0';

const DiscordIcon = () => (
  <svg
    width="1em"
    height="1em"
    viewBox="0 0 24 24"
    fill="currentColor"
    aria-hidden="true"
    focusable="false"
  >
    <path d="M19.54 5.34A17.6 17.6 0 0 0 15.19 4a12.4 12.4 0 0 0-.56 1.15 16.3 16.3 0 0 0-4.87 0A11.8 11.8 0 0 0 9.2 4a17.6 17.6 0 0 0-4.35 1.34C2.08 9.46 1.33 13.47 1.7 17.42a17.7 17.7 0 0 0 5.36 2.71c.43-.59.82-1.22 1.15-1.88a11.4 11.4 0 0 1-1.81-.87c.15-.11.3-.23.44-.35a12.6 12.6 0 0 0 10.72 0c.15.12.29.24.44.35-.58.34-1.19.63-1.82.87.33.66.72 1.29 1.15 1.88a17.6 17.6 0 0 0 5.37-2.71c.43-4.58-.73-8.56-3.16-12.08ZM8.52 15c-1.05 0-1.92-.96-1.92-2.14 0-1.18.85-2.15 1.92-2.15s1.94.97 1.92 2.15c0 1.18-.85 2.14-1.92 2.14Zm6.96 0c-1.05 0-1.92-.96-1.92-2.14 0-1.18.85-2.15 1.92-2.15s1.94.97 1.92 2.15c0 1.18-.84 2.14-1.92 2.14Z" />
  </svg>
);

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
    clearDashboardSession();
    router.push(paths.login);
  };

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header className={styles.topHeader}>
        <Less3Flex align="center" gap={16}>
          <Less3Logo showOnlyIcon={false} size={16} imageSize={32} />
        </Less3Flex>
        <div className={styles.contextBar}>
          <span className={styles.contextChip}>
            <span className={styles.contextLabel}>Tenant</span>
            <span className={styles.contextValue}>default</span>
          </span>
          <span className={styles.contextChip}>
            <span className={styles.contextLabel}>User</span>
            <span className={styles.contextValue}>admin@less3</span>
          </span>
          <span className={styles.contextChip}>
            <span className={styles.contextLabel}>Role</span>
            <span className={styles.contextValue}>System Admin</span>
          </span>
          <span className={styles.serverUrlBadge}>
            <span className={styles.contextLabel}>Endpoint</span>
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
          <span className={styles.contextChip}>
            <span className={styles.contextLabel}>Version</span>
            <span className={styles.contextValue}>{dashboardVersion}</span>
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
          <Less3Tooltip title="Discord" placement="bottom">
            <Less3Button
              type="text"
              icon={<DiscordIcon />}
              onClick={() => window.open('https://discord.gg/tRAN8HgvK5', '_blank', 'noopener,noreferrer')}
              className={styles.logoutButton}
              aria-label="Discord"
            />
          </Less3Tooltip>
          <ThemeModeSwitch />
          <Less3Tooltip title="Logout" placement="bottom">
            <Less3Button
              type="text"
              icon={<LogoutOutlined />}
              onClick={handleLogout}
              className={styles.logoutButton}
              aria-label="Logout"
            />
          </Less3Tooltip>
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
