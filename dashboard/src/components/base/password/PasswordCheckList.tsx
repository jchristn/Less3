import { MIN_PASSWORD_LENGTH } from '#/constants/config';
import React from 'react';
import ReactPasswordChecklist from 'react-password-checklist';

interface Less3PasswordCheckListProps {
  value: string;
  valueAgain: string;
  className?: string;
  onChange: (isValid: boolean) => void;
}

const Less3PasswordCheckList = ({ value, valueAgain, className, onChange }: Less3PasswordCheckListProps) => {
  return (
    <ReactPasswordChecklist
      className={className}
      rules={['minLength', 'specialChar', 'number', 'capital', 'match']}
      minLength={MIN_PASSWORD_LENGTH}
      value={value}
      valueAgain={valueAgain}
      onChange={onChange}
    />
  );
};

export default Less3PasswordCheckList;
