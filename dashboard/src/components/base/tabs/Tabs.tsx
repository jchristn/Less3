import { Tabs, TabsProps } from 'antd';
import React from 'react';
import classNames from 'classnames';

const Less3Tabs = ({ custom, className, ...props }: TabsProps & { custom?: boolean; className?: string }) => {
  return <Tabs {...props} className={classNames(custom && 'custom-tabs', className)} />;
};

export default Less3Tabs;
