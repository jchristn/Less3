import { Form, FormItemProps } from 'antd';
import React from 'react';
const Less3FormItem = (props: FormItemProps) => {
  const { className, ...rest } = props;
  return <Form.Item className={className} {...rest}></Form.Item>;
};

export default Less3FormItem;
