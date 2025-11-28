'use client';
import { CopyOutlined } from '@ant-design/icons';
import React, { useState } from 'react';
import Less3Flex from '../base/flex/Flex';
import Less3Text from '../base/typograpghy/Text';
import Less3Tooltip from '../base/tooltip/Tooltip';
import classNames from 'classnames';
import Less3Button from '../base/button/Button';

interface TextWithCopyProps {
  text: string;
  className?: string;
}

const TextWithCopy = ({ text, className }: TextWithCopyProps) => {
  const [isCopied, setIsCopied] = useState(false);

  const handleCopy = () => {
    navigator.clipboard.writeText(text);
    setIsCopied(true);
    setTimeout(() => {
      setIsCopied(false);
    }, 2000);
  };

  return (
    <Less3Flex
      //   style={{ display: "inline-flex" }}
      align="center"
      gap={10}
      className={classNames(className, 'mb-0')}
    >
      <Less3Text>{text}</Less3Text>
      <Less3Tooltip title={isCopied ? 'Copied' : 'Copy'} placement="top" color={isCopied ? 'success' : 'default'}>
        <Less3Button type="link" icon={<CopyOutlined />} onClick={handleCopy} />
      </Less3Tooltip>
    </Less3Flex>
  );
};

export default TextWithCopy;
