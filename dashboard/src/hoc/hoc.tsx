'use client';

import React, { useEffect, useState, ComponentType } from 'react';
import { useValidateConnectivityMutation } from '#/store/slice/sdkSlice';
import PageLoading from '../components/base/loading/PageLoading';
import FallBack from '../components/base/fallback/FallBack';
import SharpButton from '#/components/base/button/Button';
import SharpText from '#/components/base/typograpghy/Text';
import SharpFlex from '#/components/base/flex/Flex';
import SharpTitle from '#/components/base/typograpghy/Title';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import { localStorageKeys } from '#/constants/constant';
import { apiEndpointURL } from '#/constants/config';
import { updateSdkEndPoint } from '#/services/sdk.service';

/**
 * Higher-Order Component that validates connectivity before rendering the wrapped component
 * Shows loading state while validation is in progress
 * Shows error state if validation fails
 * Renders the wrapped component on successful validation
 */

const getInitialLess3APIUrl = () => {
  if (typeof localStorage !== 'undefined') {
    const initialSharpAPIUrl = localStorage.getItem(localStorageKeys.documentAtomAPIUrl);
    return initialSharpAPIUrl || apiEndpointURL;
  }
  return apiEndpointURL;
};

// Simple cache to store validation result per URL
let validationCache: { url: string; isValid: boolean } | null = null;

export function withConnectivityValidation<P extends object>(WrappedComponent: ComponentType<P>) {
  const loadingMessage = 'Validating connectivity...';
  const errorMessage = 'Failed to validate connectivity. Please check your connection.';
  const retryEnabled = true;

  const ConnectivityValidatedComponent: React.FC<P> = (props: P) => {
    const [validateConnectivity, { isLoading, isSuccess, isError, error }] = useValidateConnectivityMutation();

    // Initialize cachedValid from cache synchronously to avoid "Initializing..." flash
    const [cachedValid, setCachedValid] = useState(() => {
      const url = getInitialLess3APIUrl();
      return !!(validationCache && validationCache.url === url && validationCache.isValid);
    });

    useEffect(() => {
      const url = getInitialLess3APIUrl();
      updateSdkEndPoint(url);

      // Check cache first
      if (validationCache && validationCache.url === url && validationCache.isValid) {
        setCachedValid(true);
        return;
      }

      // If URL changed, clear cache
      if (validationCache && validationCache.url !== url) {
        validationCache = null;
        setCachedValid(false);
      }

      // Only validate if not cached
      if (!validationCache || validationCache.url !== url) {
        validateConnectivity()
          .unwrap()
          .then(() => {
            validationCache = { url, isValid: true };
            setCachedValid(true);
          })
          .catch(() => {
            validationCache = null;
            setCachedValid(false);
          });
      }
    }, [validateConnectivity]);

    const handleRetry = () => {
      const url = getInitialLess3APIUrl();
      validationCache = null;
      setCachedValid(false);
      validateConnectivity()
        .unwrap()
        .then(() => {
          validationCache = { url, isValid: true };
          setCachedValid(true);
        })
        .catch(() => {
          validationCache = null;
          setCachedValid(false);
        });
    };                                 

    // Show loading state while validation is in progress
    if (isLoading && !cachedValid) {
      return (
        <PageContainer
          style={{
            backgroundColor: 'var(--ant-color-bg-base)',
            height: '100vh',
          }}
        >
          <PageLoading message={loadingMessage} />
        </PageContainer>
      );
    }

    // Show error state if validation failed
    if (isError) {
      return (
        <FallBack
          style={{
            backgroundColor: 'var(--ant-color-bg-base)',
            height: '100vh',
          }}
        >
          <SharpFlex className="p" justify="center" align="center" vertical gap={8}>
            <SharpTitle level={5} weight={600}>
              {errorMessage}
            </SharpTitle>
            {isError && <SharpText>{JSON.stringify((error as any)?.message || error)}</SharpText>}
            {retryEnabled && (
              <SharpButton type="primary" onClick={handleRetry}>
                Retry
              </SharpButton>
            )}
          </SharpFlex>
        </FallBack>
      );
    }

    // Render the wrapped component only after successful validation (from cache or mutation)
    if (cachedValid || isSuccess) {
      return <WrappedComponent {...props} />;
    }

    // Fallback state (shouldn't normally reach here)
    return <PageLoading message="Initializing..." />;
  };

  // Set display name for better debugging
  ConnectivityValidatedComponent.displayName = `withConnectivityValidation(${
    WrappedComponent.displayName || WrappedComponent.name || 'Component'
  })`;

  return ConnectivityValidatedComponent;
}

export default withConnectivityValidation;
