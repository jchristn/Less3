'use client';

import classNames from 'classnames';
import React from 'react';
import CopyToClipboard from '#/components/copy-to-clipboard/CopyToClipboard';
import Less3Flex from '../base/flex/Flex';
import Less3Text from '../base/typograpghy/Text';

interface TextWithCopyProps {
  text: string;
  className?: string;
}

const TextWithCopy = ({ text, className }: TextWithCopyProps) => {
  return (
    <Less3Flex align="center" gap={10} className={classNames(className, 'mb-0')}>
      <Less3Text className={className}>{text}</Less3Text>
      <CopyToClipboard text={text} ariaLabel="Copy text" />
    </Less3Flex>
  );
};

export default TextWithCopy;
