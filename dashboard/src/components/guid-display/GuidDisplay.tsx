'use client';

import React from 'react';
import CopyToClipboard from '#/components/copy-to-clipboard/CopyToClipboard';
import Less3Flex from '../base/flex/Flex';
import styles from './guidDisplay.module.scss';

interface GuidDisplayProps {
  guid: string;
  className?: string;
}

const GuidDisplay = ({ guid, className }: GuidDisplayProps) => {
  return (
    <Less3Flex align="center" gap={8} className={className}>
      <span className={styles.guidText}>{guid}</span>
      <CopyToClipboard text={guid} tooltip="Copy GUID" copiedTooltip="Copied!" ariaLabel="Copy GUID" />
    </Less3Flex>
  );
};

export default GuidDisplay;
