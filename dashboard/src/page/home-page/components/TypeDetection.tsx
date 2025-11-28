'use client';

import React, { useRef, useState } from 'react';
import { message } from 'antd';
import { UploadOutlined } from '@ant-design/icons';
import Less3Button from '#/components/base/button/Button';
import Less3Upload from '#/components/base/upload/Upload';
import { useTypeDetectionMutation } from '#/store/slice/sdkSlice';
import Less3Title from '#/components/base/typograpghy/Title';
import Less3Divider from '#/components/base/divider/Divider';
import JSONEditor from '#/components/base/json-editor/JSONEditor';
import { uuid } from '#/utils/stringUtils';
import Less3Flex from '#/components/base/flex/Flex';

const TypeDetection = () => {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [responseData, setResponseData] = useState<any>(null);
  const [detectType, { isLoading }] = useTypeDetectionMutation();
  const uniqueKey = useRef<string>(uuid());

  const handleFileChange = (file: File) => {
    setSelectedFile(file);
    return false; // Prevent auto upload
  };

  const handleSubmit = async () => {
    if (!selectedFile) {
      message.warning('Please select a file first');
      return;
    }

    try {
      // Call the mutation
      console.log('selectedFile', selectedFile);
      const response = await detectType(selectedFile).unwrap();

      // Store the response
      setResponseData(response);
      uniqueKey.current = uuid();
      // Log the response
      message.success('Type detection completed! Check console for results.');
    } catch (error) {
      console.error('Type Detection Error:', error);
      message.error('Failed to detect file type');
      setResponseData(null);
    }
  };

  return (
    <>
      <Less3Flex gap="20px" align="center">
        <Less3Upload beforeUpload={handleFileChange} maxCount={1} onRemove={() => setSelectedFile(null)}>
          <Less3Button icon={<UploadOutlined />}>Select File</Less3Button>
        </Less3Upload>

        <Less3Button type="primary" onClick={handleSubmit} loading={isLoading} disabled={!selectedFile}>
          Submit
        </Less3Button>
      </Less3Flex>

      {responseData && (
        <>
          <Less3Divider />
          <Less3Title level={5}>Response</Less3Title>
          <JSONEditor
            value={responseData}
            onChange={() => {}}
            mode="tree"
            uniqueKey={uniqueKey.current}
            expandOnStart={true}
          />
        </>
      )}
    </>
  );
};

export default TypeDetection;
