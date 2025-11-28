import { Dropdown, DropDownProps } from 'antd';

const Less3Dropdown = (props: DropDownProps) => {
  const { children, ...rest } = props;
  return <Dropdown {...rest}>{children}</Dropdown>;
};

export default Less3Dropdown;
