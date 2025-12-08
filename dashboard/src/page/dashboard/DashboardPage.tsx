'use client';
import React from 'react';
import { InfoCircleOutlined } from '@ant-design/icons';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import Less3Card from '#/components/base/card/Card';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Title from '#/components/base/typograpghy/Title';
import Less3Paragraph from '#/components/base/typograpghy/Paragraph';

const DashboardPage: React.FC = () => {
  return (
    <PageContainer pageTitle="Home">
      <Less3Flex vertical gap={24}>
        {/* Welcome Section */}
        <Less3Card>
          <Less3Flex vertical gap={16}>
            <Less3Flex align="center" gap={12}>
              <InfoCircleOutlined style={{ fontSize: 24, color: '#1890ff' }} />
              <Less3Title fontSize={24} weight={600} style={{ margin: 0 }}>
                Welcome to Less3
              </Less3Title>
            </Less3Flex>
            <Less3Paragraph fontSize={16} style={{ margin: 0, lineHeight: 1.6 }}>
              Manage your storage buckets and configure your storage infrastructure from this centralized home. Use the
              navigation menu to access different sections and manage your resources.
            </Less3Paragraph>
          </Less3Flex>
        </Less3Card>
      </Less3Flex>
    </PageContainer>
  );
};

export default DashboardPage;
