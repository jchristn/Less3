'use client';
import React from 'react';
import {
  AreaChartOutlined,
  ClusterOutlined,
  DashboardOutlined,
  FundOutlined,
  LineChartOutlined,
  LinkOutlined,
  LockOutlined,
} from '@ant-design/icons';
import Less3Card from '#/components/base/card/Card';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Text from '#/components/base/typograpghy/Text';
import Less3Tag from '#/components/base/tag/Tag';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import CopyToClipboard from '#/components/copy-to-clipboard/CopyToClipboard';
import { grafanaUrl, prometheusUrl } from '#/constants/config';
import styles from '../cluster/ClusterPage.module.scss';

interface ToolLinkCardProps {
  title: string;
  description: string;
  url: string;
  icon: React.ReactNode;
  color: string;
}

const ToolLinkCard: React.FC<ToolLinkCardProps> = ({ title, description, url, icon, color }) => (
  <Less3Card hoverable style={{ height: '100%' }}>
    <Less3Flex vertical gap={12}>
      <Less3Flex align="center" gap={12}>
        <div
          style={{
            flex: '0 0 44px',
            width: 44,
            height: 44,
            borderRadius: 10,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            background: color + '14',
            color,
            fontSize: 22,
          }}
        >
          {icon}
        </div>
        <Less3Flex vertical gap={2}>
          <Less3Text weight={600} fontSize={16}>
            {title}
          </Less3Text>
          <Less3Text type="secondary" fontSize={13}>
            {description}
          </Less3Text>
        </Less3Flex>
      </Less3Flex>
      <Less3Flex align="center" gap={8} wrap="wrap">
        <a href={url} target="_blank" rel="noreferrer">
          <LinkOutlined /> {url}
        </a>
        <CopyToClipboard text={url} tooltip="Copy URL" copiedTooltip="Copied!" ariaLabel={`Copy ${title} URL`} />
      </Less3Flex>
    </Less3Flex>
  </Less3Card>
);

interface DashboardInfo {
  name: string;
  description: string;
  icon: React.ReactNode;
  color: string;
}

const GRAFANA_DASHBOARDS: DashboardInfo[] = [
  {
    name: 'Overview',
    description: 'Request throughput, latency, storage, and error rates across the deployment.',
    icon: <DashboardOutlined />,
    color: '#22AF79',
  },
  {
    name: 'Locks & Data Integrity',
    description: 'Distributed lock activity and fencing conflicts. The fencing-conflict count should stay at zero.',
    icon: <LockOutlined />,
    color: '#722ed1',
  },
  {
    name: 'Cluster',
    description: 'Per-node health, leadership, and peer coordination across the cluster.',
    icon: <ClusterOutlined />,
    color: '#1890ff',
  },
];

const ObservabilityPage: React.FC = () => {
  return (
    <PageContainer pageTitle="Observability">
      <Less3Flex vertical gap={24}>
        <div>
          <Less3Text weight={600} fontSize={16} style={{ marginBottom: 12, display: 'block' }}>
            Monitoring Tools
          </Less3Text>
          <div className={styles.linkGrid}>
            <ToolLinkCard
              title="Grafana"
              description="Bundled dashboards for metrics, cluster health, and data integrity."
              url={grafanaUrl}
              icon={<AreaChartOutlined />}
              color="#fa8c16"
            />
            <ToolLinkCard
              title="Prometheus"
              description="Raw metric storage and ad-hoc PromQL querying for Less3."
              url={prometheusUrl}
              icon={<FundOutlined />}
              color="#e6522c"
            />
          </div>
        </div>

        <div>
          <Less3Flex align="center" gap={8} style={{ marginBottom: 12 }}>
            <Less3Text weight={600} fontSize={16}>
              Grafana Dashboards
            </Less3Text>
            <Less3Tag icon={<LineChartOutlined />}>3 bundled</Less3Tag>
          </Less3Flex>
          <div className={styles.linkGrid}>
            {GRAFANA_DASHBOARDS.map((dashboard) => (
              <Less3Card key={dashboard.name} style={{ height: '100%' }}>
                <Less3Flex align="flex-start" gap={12}>
                  <div
                    style={{
                      flex: '0 0 40px',
                      width: 40,
                      height: 40,
                      borderRadius: 8,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      background: dashboard.color + '14',
                      color: dashboard.color,
                      fontSize: 20,
                    }}
                  >
                    {dashboard.icon}
                  </div>
                  <Less3Flex vertical gap={2}>
                    <Less3Text weight={600} fontSize={15}>
                      {dashboard.name}
                    </Less3Text>
                    <Less3Text type="secondary" fontSize={13}>
                      {dashboard.description}
                    </Less3Text>
                  </Less3Flex>
                </Less3Flex>
              </Less3Card>
            ))}
          </div>
        </div>

        <Less3Text type="secondary" fontSize={13}>
          Endpoints are configurable via the <span className="code-font-style">NEXT_PUBLIC_LESS3_GRAFANA_URL</span> and{' '}
          <span className="code-font-style">NEXT_PUBLIC_LESS3_PROMETHEUS_URL</span> environment variables.
        </Less3Text>
      </Less3Flex>
    </PageContainer>
  );
};

export default ObservabilityPage;
