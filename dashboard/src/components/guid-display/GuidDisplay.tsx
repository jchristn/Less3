'use client';
import { CheckOutlined } from '@ant-design/icons';
import React, { useState } from 'react';
import Less3Flex from '../base/flex/Flex';
import Less3Tooltip from '../base/tooltip/Tooltip';
import styles from './guidDisplay.module.scss';
import { copyToClipboard } from '#/utils/clipboardUtils';

interface GuidDisplayProps {
  guid: string;
  className?: string;
}

const CopyIcon = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <rect x="9" y="9" width="13" height="13" rx="2" ry="2" />
    <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
  </svg>
);

const GuidDisplay = ({ guid, className }: GuidDisplayProps) => {
  const [isCopied, setIsCopied] = useState(false);

  const handleCopy = (e: React.MouseEvent) => {
    e.stopPropagation();
    copyToClipboard(guid);
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
            <span className={styles.defaultIcon}><CopyIcon /></span>
          )}
        </span>
      </Less3Tooltip>
    </Less3Flex>
  );
};

export default GuidDisplay;
