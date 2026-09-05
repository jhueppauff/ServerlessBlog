import { Route, Routes } from 'react-router-dom';
import { InteractionType } from '@azure/msal-browser';
import { MsalAuthenticationTemplate } from '@azure/msal-react';
import { Typography } from '@mui/material';
import { BlogClientProvider, useAuthenticatedBlogClient } from './api/BlogClientProvider';
import { loginRequest } from './auth/msalConfig';
import { Layout } from './components/Layout';
import { Loading } from './components/Loading';
import { NotificationProvider } from './components/NotificationProvider';
import { EditPost } from './pages/EditPost';
import { Images } from './pages/Images';
import { Metrics } from './pages/Metrics';
import { Posts } from './pages/Posts';

export const AppRoutes = () => (
  <Routes>
    <Route element={<Layout />}>
      <Route path="/" element={<Posts />} />
      <Route path="/add" element={<EditPost />} />
      <Route path="/edit/:slug" element={<EditPost />} />
      <Route path="/images" element={<Images />} />
      <Route path="/metrics" element={<Metrics />} />
      <Route path="*" element={<Typography>Sorry, there's nothing at this address.</Typography>} />
    </Route>
  </Routes>
);

const AuthenticatedApp = () => {
  const client = useAuthenticatedBlogClient();

  return (
    <BlogClientProvider client={client}>
      <NotificationProvider>
        <AppRoutes />
      </NotificationProvider>
    </BlogClientProvider>
  );
};

export const App = () => (
  <MsalAuthenticationTemplate
    interactionType={InteractionType.Redirect}
    authenticationRequest={loginRequest}
    loadingComponent={Loading}
    errorComponent={({ error }) => (
      <Typography color="error">Authentication failed: {error?.errorMessage}</Typography>
    )}
  >
    <AuthenticatedApp />
  </MsalAuthenticationTemplate>
);
