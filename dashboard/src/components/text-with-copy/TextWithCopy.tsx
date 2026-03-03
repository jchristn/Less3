'use client';
import { CheckOutlined } from '@ant-design/icons';
import React, { useState } from 'react';
import Less3Flex from '../base/flex/Flex';
import Less3Text from '../base/typograpghy/Text';
import Less3Tooltip from '../base/tooltip/Tooltip';
import classNames from 'classnames';
import { copyToClipboard } from '#/utils/clipboardUtils';

interface TextWithCopyProps {
  text: string;
  className?: string;
}

const CopyIcon = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <rect x="9" y="9" width="13" height="13" rx="2" ry="2" />
    <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
  </svg>
);

const TextWithCopy = ({ text, className }: TextWithCopyProps) => {
  const [isCopied, setIsCopied] = useState(false);

  const handleCopy = () => {
    copyToClipboard(text);
    setIsCopied(true);
    setTimeout(() => {
      setIsCopied(false);
    }, 2000);
  };

  return (
    <Less3Flex
      align="center"
      gap={10}
      className={classNames(className, 'mb-0')}
    >
      <Less3Text>{text}</Less3Text>
      <Less3Tooltip title={isCopied ? 'Copied' : 'Copy'} placement="top" color={isCopied ? 'success' : 'default'}>
        <span
          onClick={handleCopy}
          style={{
            cursor: 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: isCopied ? 'var(--ant-color-success)' : 'var(--ant-color-text-quaternary)',
            transition: 'color 0.2s ease',
            padding: 4,
            borderRadius: 4,
          }}
        >
          {isCopied ? <CheckOutlined style={{ fontSize: 14 }} /> : <CopyIcon />}
        </span>
      </Less3Tooltip>
    </Less3Flex>
  );
};

export default TextWithCopy;
