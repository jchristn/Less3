import React from 'react';
import { Upload, UploadProps } from 'antd';
import styles from './upload.module.scss';
import classNames from 'classnames';

type Less3UploadProps = UploadProps;

const Less3Upload = (props: Less3UploadProps) => {
  const { className, ...rest } = props;
  return <Upload {...rest} className={classNames(styles.uploadContainer, className)} />;
};

export default Less3Upload;
