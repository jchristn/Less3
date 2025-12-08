import React from 'react';
import Less3Modal from '../base/modal/Modal';
import Less3Button from '../base/button/Button';
import Less3Paragraph from '../base/typograpghy/Paragraph';
import Less3Flex from '../base/flex/Flex';

interface ConfirmationModalProps {
  title: string;
  isModelVisible: boolean;
  setIsModelVisible: (value: boolean) => void;
  handleConfirm: () => void;
  paragraphText: string;
  isLoading?: boolean;
}

const ConfirmationModal = ({
  isLoading,
  title,
  isModelVisible,
  setIsModelVisible,
  handleConfirm,
  paragraphText,
}: ConfirmationModalProps) => {
  return (
    <Less3Modal
      title={title}
      centered
      open={isModelVisible}
      onCancel={() => setIsModelVisible(false)}
      footer={
        <Less3Flex justify="end" gap={10}>
          <Less3Button
            data-testid="confirmation-modal-cancel-button"
            type="default"
            onClick={() => setIsModelVisible(false)}
          >
            Cancel
          </Less3Button>
          <Less3Button
            data-testid="confirmation-modal-confirm-button"
            type="primary"
            onClick={handleConfirm}
            loading={isLoading}
          >
            Confirm
          </Less3Button>
        </Less3Flex>
      }
    >
      <Less3Paragraph>{paragraphText}</Less3Paragraph>
    </Less3Modal>
  );
};

export default ConfirmationModal;
