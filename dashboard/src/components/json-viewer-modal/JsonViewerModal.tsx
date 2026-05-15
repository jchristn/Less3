'use client';

import React from 'react';
import Less3Button from '#/components/base/button/Button';
import Less3Modal from '#/components/base/modal/Modal';
import CopyToClipboard from '#/components/copy-to-clipboard/CopyToClipboard';

interface JsonViewerModalProps {
  open: boolean;
  title: string;
  data: unknown;
  onClose: () => void;
  width?: number | string;
}

const JsonViewerModal: React.FC<JsonViewerModalProps> = ({ title, data, open, onClose, width = '80vw' }) => {
  const formattedJson = React.useMemo(() => {
    try {
      return JSON.stringify(data, null, 2);
    } catch {
      return String(data);
    }
  }, [data]);

  return (
    <Less3Modal
      title={
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 8,
            minWidth: 0,
            paddingRight: 56,
          }}
        >
          <span
            style={{
              flex: 1,
              minWidth: 0,
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}
          >
            {title}
          </span>
          <CopyToClipboard
            text={formattedJson}
            tooltip="Copy JSON"
            ariaLabel="Copy JSON"
            failureMessage="Failed to copy JSON"
          />
        </div>
      }
      open={open}
      onCancel={onClose}
      footer={[
        <Less3Button key="close" onClick={onClose}>
          Close
        </Less3Button>,
      ]}
      width={width}
      centered
      keyboard={true}
    >
      <div style={{ minHeight: 320 }}>
        <pre
          style={{
            margin: 0,
            padding: 16,
            minHeight: 320,
            maxHeight: '60vh',
            overflow: 'auto',
            whiteSpace: 'pre-wrap',
            wordBreak: 'break-word',
            borderRadius: 8,
            border: '1px solid var(--color-separator)',
            background: 'var(--ant-color-bg-layout)',
            fontSize: 12,
            lineHeight: 1.6,
            fontFamily: "'SF Mono', 'Monaco', 'Inconsolata', 'Fira Code', 'Consolas', monospace",
          }}
        >
          {formattedJson}
        </pre>
      </div>
    </Less3Modal>
  );
};

export default JsonViewerModal;
