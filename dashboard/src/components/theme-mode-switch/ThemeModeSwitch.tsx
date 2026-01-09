import React from 'react';
import { MoonOutlined, SunOutlined } from '@ant-design/icons';
import { useAppContext } from '#/hooks/appHooks';
import { ThemeEnum } from '#/types/types';
import styles from './ThemeModeSwitch.module.scss';

const ThemeModeSwitch = () => {
  const { theme, setTheme } = useAppContext();
  const isDark = theme === ThemeEnum.DARK;

  const handleToggle = () => {
    setTheme(isDark ? ThemeEnum.LIGHT : ThemeEnum.DARK);
  };

  return (
    <button
      type="button"
      onClick={handleToggle}
      className={styles.themeModeSwitch}
      aria-label={isDark ? 'Switch to light mode' : 'Switch to dark mode'}
    >
      {isDark ? <SunOutlined /> : <MoonOutlined />}
    </button>
  );
};

export default ThemeModeSwitch;
