'use client';
import React from 'react';
import { Layout, Menu, Button } from 'antd';
import {
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  HomeOutlined,
  DatabaseOutlined,
  FolderOutlined,
  UserOutlined,
  KeyOutlined,
} from '@ant-design/icons';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import classNames from 'classnames';
import Less3Flex from '../flex/Flex';
import Less3Logo from '#/components/logo/Logo';
import styles from './sidebar.module.scss';

const { Sider } = Layout;

interface SidebarProps {
  collapsed?: boolean;
  onCollapse?: (collapsed: boolean) => void;
  showLogo?: boolean;
}

const Sidebar: React.FC<SidebarProps> = ({ collapsed = false, onCollapse, showLogo = true }: SidebarProps) => {
  const pathname = usePathname();

  const getSelectedKey = () => {
    if (pathname === '/dashboard') return ['dashboard'];
    if (pathname.startsWith('/admin/buckets')) return ['buckets'];
    if (pathname.startsWith('/admin/objects')) return ['objects'];
    if (pathname.startsWith('/admin/users')) return ['users'];
    if (pathname.startsWith('/admin/credentials')) return ['credentials'];
    return [];
  };

  const menuItems = [
    {
      key: 'dashboard',
      icon: <HomeOutlined />,
      label: <Link href="/dashboard">Home</Link>,
    },
    {
      key: 'buckets',
      icon: <DatabaseOutlined />,
      label: <Link href="/admin/buckets">Buckets</Link>,
    },
    {
      key: 'objects',
      icon: <FolderOutlined />,
      label: <Link href="/admin/objects">Objects</Link>,
    },
    {
      key: 'users',
      icon: <UserOutlined />,
      label: <Link href="/admin/users">Users</Link>,
    },
    {
      key: 'credentials',
      icon: <KeyOutlined />,
      label: <Link href="/admin/credentials">Credentials</Link>,
    },
  ];

  return (
    <Sider
      theme="light"
      width={200}
      collapsed={collapsed}
      collapsedWidth={60}
      className={classNames(styles.sidebarContainer, !showLogo && styles.sidebarWithTopHeader)}
      trigger={null}
      collapsible
    >
      {showLogo && (
        <Less3Flex justify="center" align="center" className={styles.logoContainer}>
          <Less3Logo showOnlyIcon={collapsed} size={16} imageSize={35} />
        </Less3Flex>
      )}
      <Less3Flex justify="flex-end" className={styles.collapseButtonContainer}>
        <Button
          type="text"
          icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
          onClick={() => onCollapse?.(!collapsed)}
          className={styles.collapseButton}
        />
      </Less3Flex>
      <Menu mode="inline" selectedKeys={getSelectedKey()} items={menuItems} className={styles.menu} />
    </Sider>
  );
};

export default Sidebar;
