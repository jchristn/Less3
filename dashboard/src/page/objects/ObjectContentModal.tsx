'use client';

import React, { useEffect, useState } from 'react';
import { Input } from 'antd';
import Less3Button from '#/components/base/button/Button';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Modal from '#/components/base/modal/Modal';
import Less3Text from '#/components/base/typograpghy/Text';

interface ObjectContentModalProps {
  open: boolean;
  objectKey: string;
  contentType?: string;
  content: string;
  loading?: boolean;
  saving?: boolean;
  onClose: () => void;
  onSave?: (value: string) => Promise<void> | void;
}

const editorStyle: React.CSSProperties = {
  width: '100%',
  minHeight: 'calc(78vh - 170px)',
  resize: 'none',
  fontSize: 13,
  lineHeight: 1.7,
  padding: 16,
  fontFamily: "'SF Mono', 'Monaco', 'Inconsolata', 'Fira Code', 'Consolas', monospace",
};

const ObjectContentModal: React.FC<ObjectContentModalProps> = ({
  open,
  objectKey,
  contentType,
  content,
  loading = false,
  saving = false,
  onClose,
  onSave,
}) => {
  const [draft, setDraft] = useState(content);
  const [isSaveWarningVisible, setIsSaveWarningVisible] = useState(false);

  useEffect(() => {
    if (open) {
      setDraft(content);
    }
  }, [content, open]);

  const handleRequestSave = async () => {
    if (!onSave) {
      return;
    }

    await onSave(draft);
    setIsSaveWarningVisible(false);
  };

  return (
    <>
      <Less3Modal
        title={`Object Contents - ${objectKey}`}
        open={open}
        onCancel={onClose}
        width="92vw"
        keyboard={true}
        footer={[
          <Less3Button key="close" onClick={onClose} disabled={saving}>
            Close
          </Less3Button>,
          <Less3Button
            key="save"
            type="primary"
            onClick={() => setIsSaveWarningVisible(true)}
            loading={saving}
            disabled={loading}
          >
            Save
          </Less3Button>,
        ]}
      >
        <Less3Flex vertical gap={12} style={{ minHeight: 'calc(78vh - 120px)' }}>
          <Less3Text type="secondary" style={{ fontSize: 12 }}>
            Content-Type: {contentType || 'Unknown'}
          </Less3Text>
          {loading ? (
            <div style={{ textAlign: 'center', padding: '120px 0' }}>Loading contents...</div>
          ) : (
            <Input.TextArea
              value={draft}
              onChange={(event) => setDraft(event.target.value)}
              style={editorStyle}
              autoSize={false}
            />
          )}
        </Less3Flex>
      </Less3Modal>

      <Less3Modal
        title="Confirm Object Save"
        open={isSaveWarningVisible}
        onCancel={() => setIsSaveWarningVisible(false)}
        width={350}
        centered
        keyboard={true}
        footer={[
          <Less3Button key="cancel" onClick={() => setIsSaveWarningVisible(false)} disabled={saving}>
            Cancel
          </Less3Button>,
          <Less3Button key="confirm" type="primary" danger onClick={handleRequestSave} loading={saving}>
            Continue
          </Less3Button>,
        ]}
      >
        <Less3Flex vertical gap={12}>
          <p>
            Saving this object will delete the current object and then write the new contents.
          </p>
          <p style={{ margin: 0, color: '#8c8c8c', fontSize: 13 }}>
            If the write fails after the delete succeeds, the original object data may be lost.
          </p>
        </Less3Flex>
      </Less3Modal>
    </>
  );
};

export default ObjectContentModal;
