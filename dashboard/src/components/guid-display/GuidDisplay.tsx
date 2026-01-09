'use client';
import { CopyOutlined, CheckOutlined } from '@ant-design/icons';
import React, { useState } from 'react';
import Less3Flex from '../base/flex/Flex';
import Less3Tooltip from '../base/tooltip/Tooltip';
import styles from './guidDisplay.module.scss';

interface GuidDisplayProps {
  guid: string;
  className?: string;
}

const GuidDisplay = ({ guid, className }: GuidDisplayProps) => {
  const [isCopied, setIsCopied] = useState(false);

  const handleCopy = (e: React.MouseEvent) => {
    e.stopPropagation();
    navigator.clipboard.writeText(guid);
    setIsCopied(true);
    setTimeout(() => {
      setIsCopied(false);
    }, 1500);
  };

  return (
    <Less3Flex align="center" gap={8} className={className}>
      <span className={styles.guidText}>{guid}</span>
      <Less3Tooltip title={isCopied ? 'Copied!' : 'Copy'} placement="top">
        <span className={styles.copyIcon} onClick={handleCopy}>
          {isCopied ? (
            <CheckOutlined className={styles.checkIcon} />
          ) : (
            <CopyOutlined className={styles.defaultIcon} />
          )}
        </span>
      </Less3Tooltip>
    </Less3Flex>
  );
};

export default GuidDisplay;
