'use client';
import React from 'react';
import { LoadingOutlined } from '@ant-design/icons';
import styles from './pageLoding.module.scss';
import Less3Text from '../typograpghy/Text';
import Less3Flex from '../flex/Flex';
import classNames from 'classnames';

const PageLoading = ({
  message = 'Loading...',
  dataTestId,
}: {
  message?: string | JSX.Element;
  dataTestId?: string;
}) => {
  return (
    <Less3Flex
      data-testid={dataTestId}
      className={classNames(styles.pageLoading, 'mt')}
      justify="center"
      align="center"
      vertical
    >
      <Less3Text>{message}</Less3Text>
      <LoadingOutlined className={styles.pageLoader} />
    </Less3Flex>
  );
};

export default PageLoading;
