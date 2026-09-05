# ServerlessBlog Editor

Admin portal for the ServerlessBlog engine, written in React and TypeScript and hosted in
Azure Static Web Apps. It replaces the former Blazor WebAssembly editor (`EditorNG`) and
uses the same Azure Functions API.

## Features

- Overview of all blog posts including page views, publishing and deletion
- Markdown editor with live preview and image picker for new and existing posts
- Image management: upload, preview and delete blob images
- Metrics: page views of the last 31 days for one or more posts
- Azure AD (MSAL) authentication using the `user_impersonation` scope of the backend

## Configuration

The app is configured through Vite environment variables (see [.env](.env)):

| Variable              | Description                                    |
| --------------------- | ---------------------------------------------- |
| `VITE_API_ENDPOINT`   | Base url of the Azure Functions backend        |
| `VITE_AAD_CLIENT_ID`  | Application (client) id of the Azure AD app     |
| `VITE_AAD_AUTHORITY`  | Azure AD authority, e.g. `https://login.microsoftonline.com/<tenant>` |

Create a `.env.local` file to override these values locally.

## Development

```bash
npm install
npm run dev      # start the dev server on http://localhost:5173
npm run lint     # lint the sources
npm test         # run the unit tests
npm run build    # type check and create a production build in dist/
```
