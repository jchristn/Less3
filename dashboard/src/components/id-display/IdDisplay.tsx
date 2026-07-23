'use client';

import React from 'react';
import classNames from 'classnames';
import CopyToClipboard from '#/components/copy-to-clipboard/CopyToClipboard';
import Less3Flex from '../base/flex/Flex';
import styles from './idDisplay.module.scss';

interface IdDisplayProps {
  id: string;
  className?: string;
}

const IdDisplay = ({ id, className }: IdDisplayProps) => {
  return (
    <Less3Flex align="center" gap={8} className={classNames(styles.idDisplay, className)}>
      <span className={styles.idText}>{id}</span>
      <CopyToClipboard text={id} tooltip="Copy ID" copiedTooltip="Copied!" ariaLabel="Copy ID" />
    </Less3Flex>
  );
};

export default IdDisplay;
