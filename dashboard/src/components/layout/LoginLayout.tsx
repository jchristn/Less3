import React from 'react';
import styles from './loginLayout.module.scss';
import Less3Flex from '../base/flex/Flex';
import Less3Logo from '../logo/Logo';
import Less3Text from '../base/typograpghy/Text';
import { Less3Theme } from '#/theme/theme';
import ThemeModeSwitch from '../theme-mode-switch/ThemeModeSwitch';

const LoginLayout = ({ children }: { children: React.ReactNode }) => {
  return (
    <Less3Flex className={styles.loginLayout} vertical justify="space-between">
      <Less3Flex className={styles.header} justify="space-between" align="center">
        <Less3Logo />
        <ThemeModeSwitch />
      </Less3Flex>
      <div className={styles.content}>{children}</div>
      <Less3Flex className={styles.footer} justify="center" align="center">
        <Less3Text color={Less3Theme.borderGray}></Less3Text>
      </Less3Flex>
    </Less3Flex>
  );
};

export default LoginLayout;
