import React, { useState } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { I18nextProvider } from 'react-i18next';
import i18n from '@/shared/i18n';
import { useBootstrapAuth } from '@/features/auth';
import { Toaster } from '@/shared/components/ui/Toaster';

const AuthSessionBootstrap: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  useBootstrapAuth();
  return <>{children}</>;
};

export const AppProviders: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            refetchOnWindowFocus: false,
            retry: 1,
          },
        },
      })
  );

  return (
    <QueryClientProvider client={queryClient}>
      <I18nextProvider i18n={i18n}>
        <AuthSessionBootstrap>{children}</AuthSessionBootstrap>
        <Toaster position="top-right" />
      </I18nextProvider>
    </QueryClientProvider>
  );
};
