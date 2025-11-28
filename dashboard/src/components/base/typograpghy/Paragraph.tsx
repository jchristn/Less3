import { Typography } from 'antd';
import { ParagraphProps } from 'antd/es/typography/Paragraph';
import classNames from 'classnames';

const { Paragraph } = Typography;

export type Less3ParagraphProps = ParagraphProps & {
  weight?: number;
  fontSize?: number;
  color?: string;
};

const Less3Paragraph = (props: Less3ParagraphProps) => {
  const { children, className, style, color, weight, fontSize, ...rest } = props;
  return (
    <Paragraph
      className={classNames(className)}
      style={{ color: color, fontWeight: weight, fontSize: fontSize, ...style }}
      {...rest}
    >
      {children}
    </Paragraph>
  );
};

export default Less3Paragraph;
