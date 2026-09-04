export interface AppConfig {
  apiEndpoint: string;
  clientId: string;
  authority: string;
}

const trimTrailingSlash = (value: string): string => value.replace(/\/+$/, '');

export const config: AppConfig = {
  apiEndpoint: trimTrailingSlash(
    import.meta.env.VITE_API_ENDPOINT ?? 'https://func-blog-engine-we-prod-001.azurewebsites.net',
  ),
  clientId: import.meta.env.VITE_AAD_CLIENT_ID ?? '486fb560-438a-4cd5-92f1-c3b9d93b5131',
  authority:
    import.meta.env.VITE_AAD_AUTHORITY ??
    'https://login.microsoftonline.com/72e647c0-4a7a-4959-bee5-14c8615d8ae5',
};

export const apiScopes = [`${config.apiEndpoint}/user_impersonation`];
