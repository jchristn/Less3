import React from 'react';
import { DownCircleOutlined, AccountBookOutlined, LogoutOutlined, UserOutlined } from '@ant-design/icons';
import Less3Space from '#/components/base/space/Space';
import Less3Text from '#/components/base/typograpghy/Text';
import { getFirstLetterOfTheWord } from '#/utils/stringUtils';

import Less3Dropdown from '#/components/base/dropdown/Dropdown';
import Less3Avatar from '#/components/base/avatar/Avatar';
import { Less3Theme } from '#/theme/theme';
import { MenuProps } from 'antd';
import { usePathname } from 'next/navigation';
import { getDashboardPathKey } from '#/utils/appUtils';
import styles from './styles.module.scss';

const items: MenuProps['items'] = [
  {
    label: 'Profile',
    key: 'profile',
    icon: <UserOutlined />,
  },
  {
    label: 'Account',
    key: 'account',
    icon: <AccountBookOutlined />,
  },
  {
    label: 'Logout',
    key: 'logout',
    icon: <LogoutOutlined />,
  },
];

const LoggedUserInfo = () => {
  const pathname = usePathname();

  const userName = 'User';

  const onClick: MenuProps['onClick'] = ({ key }: { key: string }) => {};

  return (
    <Less3Dropdown menu={{ items, onClick }} trigger={['click']}>
      <Less3Space className={styles.container}>
        <Less3Text className="ant-color-white" strong weight={400}>
          {userName}
        </Less3Text>
        <Less3Avatar
          alt="User Profile"
          src={!userName && '/profile-pic.png'}
          size={'small'}
          style={{ background: Less3Theme.primary }}
        >
          {getFirstLetterOfTheWord(userName)}
        </Less3Avatar>
        <DownCircleOutlined className="ant-color-white" />
      </Less3Space>
    </Less3Dropdown>
  );
};

export default LoggedUserInfo;
