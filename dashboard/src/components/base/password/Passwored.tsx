import { Input } from 'antd';
import { PasswordProps } from 'antd/es/input';
import React from 'react';

const Less3Password = ({ ...props }: PasswordProps) => {
  return <Input.Password {...props} />;
};

export default Less3Password;
