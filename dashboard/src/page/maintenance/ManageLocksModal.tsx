'use client';

import React from 'react';
import { LinkOutlined, LockOutlined } from '@ant-design/icons';
import Less3Button from '#/components/base/button/Button';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Modal from '#/components/base/modal/Modal';
import Less3Text from '#/components/base/typograpghy/Text';
import CopyToClipboard from '#/components/copy-to-clipboard/CopyToClipboard';
import { clutchApiUrl, clutchUiUrl } from '#/constants/config';

const CLUTCH_ACCESS_KEY = 'clutch-default-access-key';
const CLUTCH_ADMIN_EMAIL = 'admin@clutch.local';
const CLUTCH_ADMIN_PASSWORD = 'clutchadmin';

interface CredentialRowProps {
  label: string;
  value: string;
  href?: string;
  copyLabel: string;
}

const CredentialRow: React.FC<CredentialRowProps> = ({ label, value, href, copyLabel }) => (
  <Less3Flex align="center" justify="space-between" gap={12} wrap="wrap">
    <Less3Text type="secondary" fontSize={13}>
      {label}
    </Less3Text>
    <Less3Flex align="center" gap={8}>
      {href ? (
        <a href={href} target="_blank" rel="noopener noreferrer">
          <LinkOutlined /> {value}
        </a>
      ) : (
        <Less3Text className="code-font-style">{value}</Less3Text>
      )}
      <CopyToClipboard text={value} tooltip="Copy" copiedTooltip="Copied!" ariaLabel={copyLabel} />
    </Less3Flex>
  </Less3Flex>
);

const ManageLocksModal: React.FC = () => {
  const [open, setOpen] = React.useState(false);

  const handleOpenClutch = () => {
    window.open(clutchUiUrl, '_blank', 'noopener,noreferrer');
    setOpen(false);
  };

  return (
    <>
      <Less3Button icon={<LockOutlined />} onClick={() => setOpen(true)}>
        Manage Locks
      </Less3Button>
      <Less3Modal
        title="Manage Locks"
        open={open}
        onCancel={() => setOpen(false)}
        onOk={handleOpenClutch}
        okText="Open Clutch Dashboard"
        cancelText="Close"
        destroyOnHidden
      >
        <Less3Flex vertical gap={16}>
          <Less3Text type="secondary" fontSize={13}>
            Lock management is handled by the bundled Clutch operator dashboard. Use the URLs and
            default login credentials below to sign in, then click{' '}
            <Less3Text weight={600}>Open Clutch Dashboard</Less3Text> to launch it in a new tab.
          </Less3Text>

          <Less3Flex vertical gap={10}>
            <CredentialRow
              label="Clutch dashboard URL"
              value={clutchUiUrl}
              href={clutchUiUrl}
              copyLabel="Copy Clutch dashboard URL"
            />
            <CredentialRow
              label="Clutch server API URL"
              value={clutchApiUrl}
              href={clutchApiUrl}
              copyLabel="Copy Clutch server API URL"
            />
            <CredentialRow
              label="Access key (login)"
              value={CLUTCH_ACCESS_KEY}
              copyLabel="Copy Clutch access key"
            />
          </Less3Flex>

          <Less3Flex vertical gap={6}>
            <Less3Text weight={600} fontSize={13}>
              Advanced: system administrator
            </Less3Text>
            <CredentialRow
              label="Admin email"
              value={CLUTCH_ADMIN_EMAIL}
              copyLabel="Copy Clutch admin email"
            />
            <CredentialRow
              label="Admin password"
              value={CLUTCH_ADMIN_PASSWORD}
              copyLabel="Copy Clutch admin password"
            />
          </Less3Flex>

          <Less3Text type="secondary" fontSize={12}>
            These are the seeded defaults for the bundled Clutch server. Change them for production.
          </Less3Text>
        </Less3Flex>
      </Less3Modal>
    </>
  );
};

export default ManageLocksModal;
