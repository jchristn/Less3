import React from 'react';
import Less3Flex from '../base/flex/Flex';
import Image from 'next/image';
import Less3Text from '../base/typograpghy/Text';

const Less3Logo = ({
  showOnlyIcon,
  size = 20,
  imageSize = 45,
}: {
  showOnlyIcon?: boolean;
  size?: number;
  imageSize?: number;
}) => {
  return (
    <Less3Flex align="center" gap={10}>
      <Image src={'/assets/logo.png'} alt="Less3" height={imageSize} width={imageSize} />
      {!showOnlyIcon && <Less3Text fontSize={size}></Less3Text>}
    </Less3Flex>
  );
};

export default Less3Logo;
