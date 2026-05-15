'use client';

import { CheckOutlined } from '@ant-design/icons';
import type { TooltipProps } from 'antd';
import classNames from 'classnames';
import React from 'react';
import Less3Tooltip from '#/components/base/tooltip/Tooltip';
import { copyToClipboard } from '#/utils/clipboardUtils';
import { message } from '#/utils/message';
import styles from './copyToClipboard.module.scss';

interface CopyToClipboardProps {
  text: string;
  tooltip?: string;
  copiedTooltip?: string;
  placement?: TooltipProps['placement'];
  ariaLabel?: string;
  className?: string;
  style?: React.CSSProperties;
  iconSize?: number;
  buttonSize?: number;
  stopPropagation?: boolean;
  failureMessage?: string;
}

const CopyGlyph: React.FC<{ size: number }> = ({ size }) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    aria-hidden="true"
  >
    <rect x="9" y="9" width="13" height="13" rx="2" ry="2" />
    <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
  </svg>
);

const CopyToClipboard: React.FC<CopyToClipboardProps> = ({
  text,
  tooltip = 'Copy',
  copiedTooltip = 'Copied',
  placement = 'top',
  ariaLabel,
  className,
  style,
  iconSize = 14,
  buttonSize = 22,
  stopPropagation = true,
  failureMessage = 'Failed to copy to clipboard',
}) => {
  const [isCopied, setIsCopied] = React.useState(false);
  const resetTimeoutRef = React.useRef<number | null>(null);

  const clearResetTimeout = React.useCallback(() => {
    if (resetTimeoutRef.current !== null) {
      window.clearTimeout(resetTimeoutRef.current);
      resetTimeoutRef.current = null;
    }
  }, []);

  React.useEffect(() => clearResetTimeout, [clearResetTimeout]);

  const handleCopy = React.useCallback(async (event: React.MouseEvent<HTMLButtonElement>) => {
    if (stopPropagation) {
      event.stopPropagation();
    }

    const copied = await copyToClipboard(text);

    if (!copied) {
      message.error(failureMessage);
      return;
    }

    setIsCopied(true);
    clearResetTimeout();
    resetTimeoutRef.current = window.setTimeout(() => {
      setIsCopied(false);
      resetTimeoutRef.current = null;
    }, 2000);
  }, [clearResetTimeout, failureMessage, stopPropagation, text]);

  const buttonStyle = {
    '--copy-button-size': `${buttonSize}px`,
    ...style,
  } as React.CSSProperties;

  return (
    <Less3Tooltip title={isCopied ? copiedTooltip : tooltip} placement={placement}>
      <button
        type="button"
        aria-label={ariaLabel || tooltip}
        data-row-click-ignore="true"
        className={classNames(styles.copyButton, className, { [styles.copied]: isCopied })}
        style={buttonStyle}
        onClick={handleCopy}
      >
        {isCopied ? <CheckOutlined style={{ fontSize: iconSize }} /> : <CopyGlyph size={iconSize} />}
      </button>
    </Less3Tooltip>
  );
};

export default CopyToClipboard;
