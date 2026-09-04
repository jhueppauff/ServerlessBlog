import { MemoryRouter } from 'react-router-dom';
import { PublicClientApplication } from '@azure/msal-browser';
import { MsalProvider } from '@azure/msal-react';
import { ThemeProvider } from '@mui/material';
import { render, screen } from '@testing-library/react';
import { beforeAll, describe, expect, it, vi } from 'vitest';
import { AppRoutes } from './App';
import type { BlogClient } from './api/blogClient';
import { BlogClientProvider } from './api/BlogClientProvider';
import { msalConfig } from './auth/msalConfig';
import { NotificationProvider } from './components/NotificationProvider';
import { theme } from './theme';

const msalInstance = new PublicClientApplication(msalConfig);

const client = {
  getBlogPosts: vi.fn().mockResolvedValue([]),
  getPageViews: vi.fn().mockResolvedValue([]),
  getBlobs: vi.fn().mockResolvedValue([]),
} as Partial<BlogClient> as BlogClient;

const renderApp = (route: string) =>
  render(
    <MsalProvider instance={msalInstance}>
      <ThemeProvider theme={theme}>
        <MemoryRouter initialEntries={[route]}>
          <BlogClientProvider client={client}>
            <NotificationProvider>
              <AppRoutes />
            </NotificationProvider>
          </BlogClientProvider>
        </MemoryRouter>
      </ThemeProvider>
    </MsalProvider>,
  );

describe('AppRoutes', () => {
  beforeAll(async () => {
    await msalInstance.initialize();
  });

  it('renders the navigation and the post overview on the start page', async () => {
    renderApp('/');

    expect(screen.getByText('Blog Editor')).toBeInTheDocument();
    expect(await screen.findByText('Blog Posts')).toBeInTheDocument();
  });

  it('renders the images page', async () => {
    renderApp('/images');

    expect(await screen.findByRole('button', { name: 'Upload new image' })).toBeInTheDocument();
  });

  it('renders a message for unknown routes', () => {
    renderApp('/does-not-exist');

    expect(screen.getByText("Sorry, there's nothing at this address.")).toBeInTheDocument();
  });
});
