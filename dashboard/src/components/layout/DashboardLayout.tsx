import React, { useState } from 'react';
import { Layout } from 'antd';
import styles from './dashboard.module.scss';
import Less3Flex from '../base/flex/Flex';
import ErrorBoundary from '#/hoc/ErrorBoundary';
import ThemeModeSwitch from '../theme-mode-switch/ThemeModeSwitch';
import Link from 'next/link';
import { paths } from '#/constants/constant';
import Sidebar from '../base/sidebar';

const { Header, Content } = Layout;

interface LayoutWrapperProps {
  children: React.ReactNode;
}

const DashboardLayout = ({ children }: LayoutWrapperProps) => {
  const [collapsed, setCollapsed] = useState(false);

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sidebar collapsed={collapsed} onCollapse={setCollapsed} />
      <Layout
        style={{
          marginLeft: collapsed ? 60 : 200,
          transition: 'margin-left 0.2s',
          minHeight: '100vh',
          width: '100%',
        }}
      >
        <Header className={styles.header}>
          <Less3Flex align="center" gap={10}>
            <ThemeModeSwitch />
          </Less3Flex>

          <Less3Flex gap={10} align="center">
            <Link href={paths.login}>
              <b>Change Server URL</b>
            </Link>
          </Less3Flex>
        </Header>
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
  );
};

export default DashboardLayout;
