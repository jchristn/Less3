import { Modal, ModalProps } from 'antd';
import React from 'react';

export type Less3ModalProps = ModalProps & {};
const Less3Modal = ({ centered = true, ...props }: Less3ModalProps) => {
  return <Modal centered={centered} {...props} />;
};

export default Less3Modal;
