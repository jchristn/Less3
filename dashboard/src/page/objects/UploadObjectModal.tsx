'use client';

import React, { useState } from 'react';
import { InboxOutlined } from '@ant-design/icons';
import { Upload, UploadFile } from 'antd';
import Less3Button from '#/components/base/button/Button';
import Less3Modal from '#/components/base/modal/Modal';
import Less3Text from '#/components/base/typograpghy/Text';
import { useUploadBucketObjectMutation } from '#/store/slice/bucketsSlice';
import { message } from '#/utils/message';

interface UploadObjectModalProps {
  bucketName: string | null;
  open: boolean;
  onCancel: () => void;
  onSuccess?: () => void;
  currentPrefix?: string;
}

const UploadObjectModal: React.FC<UploadObjectModalProps> = ({
  bucketName,
  open,
  onCancel,
  onSuccess,
  currentPrefix = '',
}) => {
  const [fileList, setFileList] = useState<UploadFile[]>([]);
  const [uploadBucketObject, { isLoading: isUploading }] = useUploadBucketObjectMutation();

  const resetState = () => {
    setFileList([]);
  };

  const handleClose = () => {
    resetState();
    onCancel();
  };

  const handleUpload = async () => {
    if (!bucketName) {
      message.error('Bucket information not available');
      return;
    }

    if (fileList.length === 0) {
      message.warning('Please select at least one file to upload');
      return;
    }

    let successCount = 0;
    let failCount = 0;

    for (const file of fileList) {
      const originFile = file.originFileObj;
      if (!originFile) {
        failCount += 1;
        continue;
      }

      try {
        await uploadBucketObject({
          bucketId: bucketName,
          objectKey: `${currentPrefix}${originFile.name}`,
          file: originFile,
        }).unwrap();
        successCount += 1;
      } catch {
        failCount += 1;
      }
    }

    if (failCount === 0) {
      message.success(`Uploaded ${successCount} file(s) successfully`);
      resetState();
      onSuccess?.();
      onCancel();
      return;
    }

    message.warning(`Uploaded ${successCount} file(s), ${failCount} failed`);
    onSuccess?.();
  };

  return (
    <Less3Modal
      title={`Upload Object${bucketName ? ` to Bucket: ${bucketName}` : ''}`}
      open={open}
      onCancel={handleClose}
      width={720}
      centered
      keyboard={true}
      confirmLoading={isUploading}
      footer={[
        <Less3Button key="cancel" onClick={handleClose} disabled={isUploading}>
          Cancel
        </Less3Button>,
        <Less3Button key="upload" type="primary" onClick={handleUpload} loading={isUploading}>
          Upload
        </Less3Button>,
      ]}
    >
      <Upload.Dragger
        multiple
        fileList={fileList}
        beforeUpload={(file) => {
          setFileList((prev) => {
            const exists = prev.some((item) => item.uid === file.uid);
            if (exists) {
              return prev;
            }

            return [...prev, file];
          });
          return false;
        }}
        onRemove={(file) => {
          setFileList((prev) => prev.filter((item) => item.uid !== file.uid));
          return true;
        }}
        style={{ padding: '12px 0' }}
      >
        <p className="ant-upload-drag-icon">
          <InboxOutlined />
        </p>
        <p className="ant-upload-text">Click or drag files into this area to upload</p>
        <p className="ant-upload-hint">
          {currentPrefix ? (
            <>
              Files will be uploaded under <strong>{currentPrefix}</strong>
            </>
          ) : (
            'Files will be uploaded to the root of the selected bucket'
          )}
        </p>
      </Upload.Dragger>

      <Less3Text type="secondary" style={{ display: 'block', marginTop: 12, fontSize: 12 }}>
        Selected files: {fileList.length}
      </Less3Text>
    </Less3Modal>
  );
};

export default UploadObjectModal;
