import { createContext, useContext, useMemo, type ReactNode } from 'react';
import { useMsal } from '@azure/msal-react';
import { InteractionRequiredAuthError } from '@azure/msal-browser';
import { BlogClient } from './blogClient';
import { loginRequest } from '../auth/msalConfig';
import { config } from '../config';

const BlogClientContext = createContext<BlogClient | undefined>(undefined);

export const BlogClientProvider = ({
  client,
  children,
}: {
  client: BlogClient;
  children: ReactNode;
}) => <BlogClientContext.Provider value={client}>{children}</BlogClientContext.Provider>;

export const useBlogClient = (): BlogClient => {
  const client = useContext(BlogClientContext);

  if (!client) {
    throw new Error('useBlogClient must be used within a BlogClientProvider');
  }

  return client;
};

/** Creates a BlogClient which acquires access tokens from the signed in MSAL account. */
export const useAuthenticatedBlogClient = (): BlogClient => {
  const { instance, accounts } = useMsal();

  return useMemo(
    () =>
      new BlogClient(config.apiEndpoint, async () => {
        const account = accounts[0] ?? instance.getActiveAccount() ?? undefined;

        if (!account) {
          return undefined;
        }

        try {
          const result = await instance.acquireTokenSilent({ ...loginRequest, account });
          return result.accessToken;
        } catch (error) {
          if (error instanceof InteractionRequiredAuthError) {
            await instance.acquireTokenRedirect({ ...loginRequest, account });
          }

          throw error;
        }
      }),
    [instance, accounts],
  );
};
