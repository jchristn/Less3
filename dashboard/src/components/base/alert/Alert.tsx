import { Alert, AlertProps } from "antd";
import React from "react";

export type WeAlertProps = AlertProps;

const WeAlert = ({ ...props }: WeAlertProps) => {
  return <Alert {...props} />;
};

export default WeAlert;
