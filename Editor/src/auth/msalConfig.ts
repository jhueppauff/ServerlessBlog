import { LogLevel, PublicClientApplication, type Configuration } from '@azure/msal-browser';
import { apiScopes, config } from '../config';

export const msalConfig: Configuration = {
  auth: {
    clientId: config.clientId,
    authority: config.authority,
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: 'sessionStorage',
  },
  system: {
    loggerOptions: {
      loggerCallback: () => {},
      logLevel: LogLevel.Error,
    },
  },
};

export const loginRequest = { scopes: apiScopes };

export const msalInstance = new PublicClientApplication(msalConfig);
