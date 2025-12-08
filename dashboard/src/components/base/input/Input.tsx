import React, { LegacyRef } from 'react';
import { Input } from 'antd';
import { InputProps, InputRef } from 'antd/es/input';

type Less3InputProps = InputProps;

const Less3Input = React.forwardRef((props: Less3InputProps, ref?: LegacyRef<InputRef>) => {
  const { ...rest } = props;
  return <Input ref={ref} {...rest} />;
});

Less3Input.displayName = 'Less3Input';
export default Less3Input;
