'use client';

import React, { useEffect } from 'react';
import { App } from 'antd';
import { clearMessageInstance, setMessageInstance } from '#/utils/message';

interface AppMessageBridgeProps {
  children: React.ReactNode;
}

const AppMessageBridge: React.FC<AppMessageBridgeProps> = ({ children }) => {
  const { message } = App.useApp();

  useEffect(() => {
    setMessageInstance(message);

    return () => {
      clearMessageInstance();
    };
  }, [message]);

  return <>{children}</>;
};

export default AppMessageBridge;
