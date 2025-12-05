'use client';
import React from 'react';
import { Form, Input, message } from 'antd';
import Less3Modal from '#/components/base/modal/Modal';
import Less3FormItem from '#/components/base/form/FormItem';
import Less3Input from '#/components/base/input/Input';
import { useWriteBucketObjectMutation } from '#/store/slice/bucketsSlice';
import type { Bucket } from '#/store/slice/bucketsSlice';

interface WriteObjectFormValues {
  filename: string;
  content: string;
}

interface WriteObjectModalProps {
  bucket: Bucket | null;
  open: boolean;
  onCancel: () => void;
  onSuccess?: () => void;
}

const WriteObjectModal: React.FC<WriteObjectModalProps> = ({ bucket, open, onCancel, onSuccess }) => {
  const [form] = Form.useForm<WriteObjectFormValues>();
  const [writeBucketObject, { isLoading: isWritingObject }] = useWriteBucketObjectMutation();

  const handleOk = async () => {
    if (!bucket?.Name) {
      message.error('Bucket information not available');
      return;
    }

    try {
      const values = await form.validateFields();
      await writeBucketObject({
        bucketGUID: bucket.Name,
        objectKey: values.filename,
        content: values.content,
      }).unwrap();

      message.success(`Object "${values.filename}" written successfully`);
      form.resetFields();
      onSuccess?.();
      onCancel();
    } catch (error: any) {
      message.error(error?.data?.data || error?.message || 'Failed to write object');
    }
  };

  const handleCancel = () => {
    form.resetFields();
    onCancel();
  };

  return (
    <Less3Modal
      title={`Write Object to Bucket: ${bucket?.Name || ''}`}
      open={open}
      onOk={handleOk}
      onCancel={handleCancel}
      confirmLoading={isWritingObject}
      width={700}
      centered
    >
      <Form form={form} layout="vertical" autoComplete="off">
        <Less3FormItem
          label="Filename"
          name="filename"
          rules={[
            { required: true, message: 'Please enter filename' },
            { min: 1, message: 'Filename must be at least 1 character' },
          ]}
        >
          <Less3Input placeholder="hello.txt" />
        </Less3FormItem>
        <Less3FormItem label="Content" name="content" rules={[{ required: true, message: 'Please enter content' }]}>
          <Input.TextArea rows={8} placeholder="Enter content here..." style={{ resize: 'vertical' }} />
        </Less3FormItem>
      </Form>
    </Less3Modal>
  );
};

export default WriteObjectModal;

