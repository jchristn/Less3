import { Typography } from 'antd';
import { TitleProps } from 'antd/es/typography/Title';
import classNames from 'classnames';

const { Title } = Typography;

export type Less3TitleProps = TitleProps & {
  weight?: number;
  fontSize?: number;
  center?: boolean;
};

const Less3Title = (props: Less3TitleProps) => {
  const { children, className, style, color, weight, fontSize, center, ...rest } = props;
  return (
    <Title
      className={classNames(className)}
      style={{
        color: color,
        fontWeight: weight,
        fontSize: fontSize,
        ...style,
        textAlign: center ? 'center' : 'left',
      }}
      {...rest}
    >
      {children}
    </Title>
  );
};

export default Less3Title;
