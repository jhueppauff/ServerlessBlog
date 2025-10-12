# Azure CDN Replacement Migration Notes

## Overview
This migration removes Azure CDN from the ServerlessBlog infrastructure and adds direct custom domain support to the Azure Function Frontend.

## Changes Made

### 1. Removed Components
- **Azure CDN Profile** (`microsoft.cdn/profiles@2019-04-15`)
- **Azure CDN Endpoint** (`microsoft.cdn/profiles/endpoints@2019-04-15`)
- Parameters: `profileProperties` and `endpointProperties`
- Variables: `cdnProfileName_var` and `cdnEndpointName`

### 2. Added Components
- **Custom Domain Support**: Optional custom domain binding for Function Frontend
  - New parameter: `customDomainName` (string, optional)
  - New resource: `functionFrontendCustomDomain` (conditional deployment based on customDomainName)
  - Automatic SSL/TLS certificate provisioning via Azure
  - SNI-based SSL enabled

### 3. Infrastructure Improvements (Azure Verified Modules Pattern)
- Added `@description` decorators to all parameters for better documentation
- Organized bicep file with section comments:
  - Parameters
  - Variables
  - Static Web App (Editor)
  - Service Bus
  - RBAC Role Definitions
  - RBAC Role Assignments
  - Storage Accounts
  - App Service Plan
  - Function Apps
  - Monitoring
  - Cosmos DB

- Updated API versions to latest stable:
  - Static Web Apps: `2023-01-01`
  - Service Bus: `2022-10-01-preview`
  - Storage Accounts: `2023-01-01`
  - App Service Plan/Functions: `2023-01-01`
  - Log Analytics: `2022-10-01`
  - Cosmos DB: `2023-11-15`

- Security enhancements:
  - Added `httpsOnly: true` to Function Frontend
  - Updated authentication to `authsettingsV2` for Function Engine
  - Added `minimumTlsVersion: '1.2'` to Service Bus
  - Explicitly set `allowBlobPublicAccess: false` on storage accounts

- Code quality improvements:
  - Removed `_var` suffix from variable names
  - Added inline comments for RBAC role IDs
  - Improved resource organization and readability

## Migration Path

### For Existing Deployments
1. **DNS Configuration** (if using custom domain):
   - Create CNAME record pointing to `<function-name>.azurewebsites.net`
   - Update parameters file with your custom domain

2. **Update Parameters**:
   ```json
   {
     "customDomainName": {
       "value": "blog.yourdomain.com"  // or "" if not using custom domain
     }
   }
   ```

3. **Deploy Updated Template**:
   ```bash
   az deployment group create \
     --resource-group <your-rg> \
     --template-file resources.bicep \
     --parameters resources.parameters.json
   ```

4. **Clean Up Old CDN Resources** (manual step):
   - The CDN resources won't be automatically deleted
   - Delete them manually via Azure Portal or CLI to avoid ongoing charges

### For New Deployments
- Simply deploy the updated template
- Optionally configure custom domain by setting the `customDomainName` parameter

## Benefits

1. **Simplified Architecture**: 
   - Fewer moving parts
   - Reduced complexity
   - Lower management overhead

2. **Cost Optimization**:
   - Eliminates CDN costs
   - Azure Functions already provide good performance with global distribution

3. **Better Security**:
   - Direct HTTPS enforcement
   - Managed SSL certificates
   - Modern authentication (authsettingsV2)

4. **Improved Maintainability**:
   - Better documented code
   - Latest API versions
   - Follows Azure best practices

## Custom Domain Configuration

### Steps to Configure Custom Domain:

1. **Deploy Infrastructure First**:
   ```bash
   az deployment group create \
     --resource-group <your-rg> \
     --template-file resources.bicep \
     --parameters resources.parameters.json
   ```

2. **Configure DNS**:
   - Add CNAME record in your DNS provider
   - Point to: `<your-function-name>.azurewebsites.net`
   - Wait for DNS propagation (usually 5-15 minutes)

3. **Update Parameters and Redeploy**:
   ```json
   {
     "customDomainName": {
       "value": "blog.yourdomain.com"
     }
   }
   ```

4. **Verify**:
   - Azure will automatically provision and bind an SSL certificate
   - Access your blog at `https://blog.yourdomain.com`

## Rollback Plan

If you need to rollback to the previous CDN-based infrastructure:

1. Revert to the previous version of the bicep template
2. Redeploy with the old parameters file
3. Update DNS to point back to the CDN endpoint

## Testing

All changes have been validated:
- ✅ Bicep compilation successful
- ✅ No CDN references remaining
- ✅ Custom domain resource correctly configured
- ✅ All API versions updated to stable releases
- ✅ Parameter validation successful

## Support

For questions or issues related to this migration, please:
1. Check the README.md for custom domain configuration
2. Review this migration document
3. Open an issue on GitHub if problems persist
