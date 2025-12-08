import { Button, ButtonProps } from 'antd';

interface Less3ButtonProps extends ButtonProps {
  weight?: number;
}

const Less3Button = (props: Less3ButtonProps) => {
  const { weight, icon, ...rest } = props;
  return <Button {...rest} icon={icon} style={{ fontWeight: weight }} />;
};

export default Less3Button;
