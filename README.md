# ServerlessBlog

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=jhueppauff_ServerlessBlog&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=jhueppauff_ServerlessBlog)

Serverless blog engine running [https://blog.hueppauff.com](https://blog.hueppauff.com). This Engine use Azure Functions and Azure Platform Services to host a blog as cost efficent as possible. All Services used are consumption-based, meaning you'll only pay for the usage of the blog.
Learn more about [serverless](https://azure.microsoft.com/en-us/solutions/serverless/)

Components used

- Azure Functions
- Azure Storage (Blob, Table and Queues)
- Application Insights
- Custom Domain Support (optional)

## How to run

### Deploy with Azure CLI

```bash
# Clone the repository
git clone https://github.com/jhueppauff/ServerlessBlog.git
cd ServerlessBlog/Templates

# Deploy using Bicep
az deployment group create \
  --resource-group <your-resource-group> \
  --template-file resources.bicep \
  --parameters resources.parameters.json
```

Or use the Deploy to Azure button (requires the resources.json to be present):

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fgithub.com%2Fjhueppauff%2FServerlessBlog%2Fblob%2Fmain%2FTemplates%2Fresources.json)

### Custom Domain Configuration

To add a custom domain to your blog:

1. Deploy the infrastructure using the Bicep template
2. Configure your DNS to point to the Azure Function Frontend:
   - Add a CNAME record pointing to `<your-function-name>.azurewebsites.net`
3. Update the `customDomainName` parameter in `resources.parameters.json` with your domain (e.g., `blog.yourdomain.com`)
4. Redeploy the template to bind the custom domain
5. Azure will automatically provision an SSL certificate for HTTPS

Note: The custom domain parameter is optional. If left empty, the blog will be accessible via the default Azure Function URL.

## How to customize

Currently the Blog has some static assets sitting in the Frontend and Engine Function. If you like to change HTML, CSS you need to update the html files in the statics Folder.
