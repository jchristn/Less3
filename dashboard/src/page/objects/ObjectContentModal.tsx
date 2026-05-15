'use client';

import React, { useEffect, useMemo, useState } from 'react';
import { Input } from 'antd';
import Less3Button from '#/components/base/button/Button';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Modal from '#/components/base/modal/Modal';
import Less3Text from '#/components/base/typograpghy/Text';
import {
  formatJsonContent,
  formatXmlContent,
  getObjectContentKind,
  isTabularCsv,
  parseCsvContent,
} from '#/utils/objectContentUtils';

type ObjectContentModalMode = 'view' | 'edit';

interface ObjectContentModalProps {
  open: boolean;
  mode: ObjectContentModalMode;
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

const preStyle: React.CSSProperties = {
  margin: 0,
  padding: 16,
  minHeight: 'calc(78vh - 170px)',
  overflow: 'auto',
  whiteSpace: 'pre-wrap',
  wordBreak: 'break-word',
  borderRadius: 8,
  border: '1px solid var(--color-separator)',
  background: 'var(--ant-color-bg-layout)',
  fontSize: 13,
  lineHeight: 1.7,
  fontFamily: "'SF Mono', 'Monaco', 'Inconsolata', 'Fira Code', 'Consolas', monospace",
};

const ObjectContentModal: React.FC<ObjectContentModalProps> = ({
  open,
  mode,
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

  const contentKind = useMemo(() => getObjectContentKind(contentType, objectKey), [contentType, objectKey]);

  const formattedContent = useMemo(() => {
    switch (contentKind) {
      case 'json':
        return formatJsonContent(content);
      case 'xml':
        return formatXmlContent(content);
      default:
        return content;
    }
  }, [content, contentKind]);

  const csvRows = useMemo(() => {
    if (contentKind !== 'csv') {
      return [];
    }

    return parseCsvContent(content);
  }, [content, contentKind]);

  const isCsvTable = contentKind === 'csv' && isTabularCsv(csvRows);

  const handleRequestSave = async () => {
    if (!onSave) {
      return;
    }

    await onSave(draft);
    setIsSaveWarningVisible(false);
  };

  const renderViewContent = () => {
    if (loading) {
      return <div style={{ textAlign: 'center', padding: '120px 0' }}>Loading contents...</div>;
    }

    if (isCsvTable) {
      const [headerRow, ...bodyRows] = csvRows;

      return (
        <div
          style={{
            minHeight: 'calc(78vh - 170px)',
            overflow: 'auto',
            borderRadius: 8,
            border: '1px solid var(--color-separator)',
            background: 'var(--ant-color-bg-layout)',
          }}
        >
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
            <thead>
              <tr>
                {headerRow.map((cell, index) => (
                  <th
                    key={`${cell}-${index}`}
                    style={{
                      textAlign: 'left',
                      padding: '10px 12px',
                      borderBottom: '1px solid var(--color-separator)',
                      background: 'var(--ant-color-bg-container)',
                      position: 'sticky',
                      top: 0,
                      zIndex: 1,
                    }}
                  >
                    {cell}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {bodyRows.map((row, rowIndex) => (
                <tr key={`row-${rowIndex}`}>
                  {row.map((cell, cellIndex) => (
                    <td
                      key={`cell-${rowIndex}-${cellIndex}`}
                      style={{
                        padding: '10px 12px',
                        borderBottom: '1px solid var(--color-separator)',
                        verticalAlign: 'top',
                        wordBreak: 'break-word',
                      }}
                    >
                      {cell}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      );
    }

    return <pre style={preStyle}>{formattedContent}</pre>;
  };

  return (
    <>
      <Less3Modal
        title={`${mode === 'edit' ? 'Edit' : 'View'} Contents - ${objectKey}`}
        open={open}
        onCancel={onClose}
        width="92vw"
        keyboard={true}
        footer={[
          <Less3Button key="close" onClick={onClose} disabled={saving}>
            Close
          </Less3Button>,
          ...(mode === 'edit'
            ? [
                <Less3Button
                  key="save"
                  type="primary"
                  onClick={() => setIsSaveWarningVisible(true)}
                  loading={saving}
                  disabled={loading}
                >
                  Save
                </Less3Button>,
              ]
            : []),
        ]}
      >
        <Less3Flex vertical gap={12} style={{ minHeight: 'calc(78vh - 120px)' }}>
          <Less3Text type="secondary" style={{ fontSize: 12 }}>
            Content-Type: {contentType || 'Unknown'}
          </Less3Text>
          {mode === 'edit' ? (
            loading ? (
              <div style={{ textAlign: 'center', padding: '120px 0' }}>Loading contents...</div>
            ) : (
              <Input.TextArea
                value={draft}
                onChange={(event) => setDraft(event.target.value)}
                style={editorStyle}
                autoSize={false}
              />
            )
          ) : (
            renderViewContent()
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
