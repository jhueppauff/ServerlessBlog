# ServerlessBlog

Serverless blog engine running [https://blog.hueppauff.com](https://blog.hueppauff.com). This Engine use Azure Functions and Azure Platform Services to host a blog as cost efficent as possible. All Services used are consumption-based, meaning you'll only pay for the usage of the blog.
Learn more about [serverless](https://azure.microsoft.com/en-us/solutions/serverless/)

Components used

- Azure Functions
- Azure Storage (Blob, Table and Queues)
- Azure CDN
- Application Insights

## How to run

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fgithub.com%2Fjhueppauff%2FServerlessBlog%2Fblob%2Fmain%2FTemplates%2Fresources.json)

## How to customize

Currently the Blog has some static assets sitting in the Frontend and Engine Function. If you like to change HTML, CSS you need to update the html files in the statics Folder.
