import { Typography } from 'antd';
import { TextProps } from 'antd/es/typography/Text';

const { Text } = Typography;

export type Less3TextProps = TextProps & {
  weight?: number;
  fontSize?: number;
  color?: string;
};

const Less3Text = (props: Less3TextProps) => {
  const { children, style, weight, fontSize, color, ...rest } = props;
  return (
    <Text style={{ fontWeight: weight, fontSize: fontSize, color: color, ...style }} {...rest}>
      {children}
    </Text>
  );
};

export default Less3Text;
