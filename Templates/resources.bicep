// ========== Parameters ==========
@description('Name of the backend engine function app')
param functionEngineName string = 'func-blog-engine-we-prod-001'

@description('Name of the frontend function app')
param functionFrontendName string = 'func-blog-engine-we-prod-001'

@description('Custom domain name for the frontend function (optional). Leave empty to use default azurewebsites.net domain')
param customDomainName string = ''

@description('Azure Active Directory client ID for authentication')
param aadClientId string

@description('Azure Active Directory tenant for authentication')
param aadTenant string

@description('Enable zone redundancy for Cosmos DB')
param cosmosDbisZoneRedundant bool

@description('Name of the Cosmos DB account')
param cosmosDbName string

@description('Name of the static web app for the editor')
param staticWebAppName string

@description('Azure region for resource deployment')
param location string = 'westeurope'

@description('Name of the Service Bus namespace')
param serviceBusName string = 'sb-blog-we-prod-001'

// ========== Variables ==========
var appInsightName = replace(functionEngineName, 'func', 'appi')
var appPlanName = replace(functionEngineName, 'func', 'plan')
var storageNameWeb = 'stblogstaticweprod001'
var storageFunctionName = 'stblogfuncweprod001'

// Built-in Azure RBAC role IDs
var serviceBusReceiverRoleId = '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0' // Azure Service Bus Data Receiver
var serviceBusSenderRoleId = '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39' // Azure Service Bus Data Sender
var blogDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe' // Storage Blob Data Contributor
var blogDataOwnerRoleId = 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b' // Storage Blob Data Owner

// ========== Static Web App (Editor) ==========
resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' = {
  name: staticWebAppName
  location: location
  tags: {}
  properties: {
    repositoryUrl: 'https://github.com/jhueppauff/ServerlessBlog'
    branch: 'main'
    buildProperties: {
      appLocation: 'EditorNG'
      apiLocation: ''
      appArtifactLocation: 'wwwroot'
    }
  }
  sku: {
    tier: 'Free'
    name: 'Free'
  }
}

// ========== Service Bus ==========
resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: serviceBusName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    minimumTlsVersion: '1.2'
  }
}

resource scheduledQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  name: 'scheduled'
  parent: serviceBus
}

resource renderQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  name: 'created'
  parent: serviceBus
}

// ========== RBAC Role Definitions (Existing) ==========
resource blobDataContributorRoleDefenition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup()
  name: blogDataContributorRoleId
}

resource blobDataOwnerRoleDefenition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup()
  name: blogDataOwnerRoleId
}

resource serviceBusReceiverRoleDefenition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup()
  name: serviceBusReceiverRoleId
}

resource serviceBusSenderRoleDefenition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup()
  name: serviceBusSenderRoleId
}

// ========== RBAC Role Assignments ==========
// Frontend Storage access
resource rbacFunctionServiceStorageWebEngine 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageWeb.id, functionFrontend.id, blogDataContributorRoleId)
  scope: storageWeb
  properties: {
    principalId: functionFrontend.identity.principalId
    roleDefinitionId: blobDataContributorRoleDefenition.id
    principalType: 'ServicePrincipal'
  }
}

resource rbacFunctionServiceStorageWebFrontend 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageWeb.id, functionEngine.id, blogDataContributorRoleId)
  scope: storageWeb
  properties: {
    principalId: functionEngine.identity.principalId
    roleDefinitionId: blobDataContributorRoleDefenition.id
    principalType: 'ServicePrincipal'
  }
}

// Function Storage (2x Owner, Contributor)
resource rbacFunctionServiceStorageFunctionOwnerFrontend 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageFunction.id, functionFrontend.id, blogDataOwnerRoleId)
  scope: storageFunction
  properties: {
    principalId: functionFrontend.identity.principalId
    roleDefinitionId: blobDataOwnerRoleDefenition.id
    principalType: 'ServicePrincipal'
  }
}

resource rbacFunctionServiceStorageFunctionFrontend 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageFunction.id, functionFrontend.id, serviceBusReceiverRoleId)
  scope: storageFunction
  properties: {
    principalId: functionFrontend.identity.principalId
    roleDefinitionId: blobDataContributorRoleDefenition.id
    principalType: 'ServicePrincipal'
  }
}

resource rbacFunctionServiceStorageFunctionOwnerEngine 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageFunction.id, functionEngine.id, blogDataOwnerRoleId)
  scope: storageFunction
  properties: {
    principalId: functionEngine.identity.principalId
    roleDefinitionId: blobDataOwnerRoleDefenition.id
    principalType: 'ServicePrincipal'
  }
}

resource rbacFunctionServiceStorageFunctionEngine 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageFunction.id, functionEngine.id, serviceBusReceiverRoleId)
  scope: storageFunction
  properties: {
    principalId: functionEngine.identity.principalId
    roleDefinitionId: blobDataContributorRoleDefenition.id
    principalType: 'ServicePrincipal'
  }
}

resource rbacFunctionServiceBusReceiver 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBus.id, functionEngine.id, serviceBusReceiverRoleId)
  scope: serviceBus
  properties: {
    principalId: functionEngine.identity.principalId
    roleDefinitionId: serviceBusReceiverRoleDefenition.id
    principalType: 'ServicePrincipal'
  }
}

resource rbacFunctionServiceBusSender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBus.id, functionEngine.id, serviceBusSenderRoleId)
  scope: serviceBus
  properties: {
    principalId: functionEngine.identity.principalId
    roleDefinitionId: serviceBusSenderRoleDefenition.id
    principalType: 'ServicePrincipal'
  }
}

// ========== Storage Accounts ==========
// Web content storage (for blog posts and assets)
resource storageWeb 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageNameWeb
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
  }
}

resource storageWebBlobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageWeb
  name: 'default'
  properties: {
    changeFeed: {
      enabled: true
    }
    restorePolicy: {
      enabled: true
      days: 6
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    cors: {
      corsRules: []
    }
    deleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    isVersioningEnabled: true
  }
}

// Function app storage (for function runtime)
resource storageFunction 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageFunctionName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    networkAcls: {
      bypass: 'AzureServices'
      virtualNetworkRules: []
      ipRules: []
      defaultAction: 'Allow'
    }
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        file: {
          enabled: true
        }
        blob: {
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

// ========== App Service Plan (Consumption) ==========
resource appPlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appPlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
  }
}

// ========== Function Apps ==========
// Backend engine function (authenticated API)
resource functionEngine 'Microsoft.Web/sites@2023-01-01' = {
  name: functionEngineName
  location: location
  kind: 'functionapp'
  identity: {
     type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appPlan.id
    siteConfig: {
      minTlsVersion: '1.2'
      netFrameworkVersion: 'v8.0'
      appSettings: [
        {
          name: 'OpenApi__Auth__TenantId'
          value: '72e647c0-4a7a-4959-bee5-14c8615d8ae5'
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageFunctionName};AccountKey=${storageFunction.listKeys().keys[0].value}'
        }
        {
          name: 'AzureStorageConnection'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageNameWeb};AccountKey=${storageWeb.listKeys().keys[0].value}'
        }
        {
          name: 'CosmosDBConnection'
          value: 'DefaultEndpointsProtocol=https;AccountName=${cosmosDbName};AccountKey=${listKeys(cosmosDb.id, '2020-04-01').primaryMasterKey};TableEndpoint=https://${cosmosDbName}.table.cosmos.azure.com:443/;'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageFunctionName};AccountKey=${storageFunction.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionEngineName)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'ServiceBusConnection__fullyQualifiedNamespace'
          value: '${serviceBus.name}.servicebus.windows.net'
        }
        {
          name: 'DeletionDays'
          value: '32'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsight.properties.ConnectionString
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'WEBSITE_ENABLE_SYNC_UPDATE_SITE'
          value: 'true'
        }
        {
          name: 'AzureWebJobsDisableHomepage'
          value: 'true'
        }
      ]
    }
  }
}

// Authentication settings for backend engine
resource functionEngine_authsettings 'Microsoft.Web/sites/config@2023-01-01' = {
  parent: functionEngine
  name: 'authsettingsV2'
  properties: {
    globalValidation: {
      requireAuthentication: true
      unauthenticatedClientAction: 'RedirectToLoginPage'
    }
    identityProviders: {
      azureActiveDirectory: {
        enabled: true
        registration: {
          clientId: aadClientId
          openIdIssuer: 'https://sts.windows.net/${aadTenant}/v2.0'
        }
      }
    }
  }
}

// Frontend function (public-facing blog)
resource functionFrontend 'Microsoft.Web/sites@2023-01-01' = {
  name: functionFrontendName
  location: location
  kind: 'functionapp'
  identity: {
     type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appPlan.id
    httpsOnly: true
    siteConfig: {
      minTlsVersion: '1.2'
      netFrameworkVersion: 'v8.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageFunctionName};AccountKey=${storageFunction.listKeys().keys[0].value}'
        }
        {
          name: 'AzureStorageConnection'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageNameWeb};AccountKey=${storageWeb.listKeys().keys[0].value}'
        }
        {
          name: 'CosmosDBConnection'
          value: 'DefaultEndpointsProtocol=https;AccountName=${cosmosDbName};AccountKey=${listKeys(cosmosDb.id, '2020-04-01').primaryMasterKey};TableEndpoint=https://${cosmosDbName}.table.cosmos.azure.com:443/;'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageFunctionName};AccountKey=${storageFunction.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionFrontendName)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsight.properties.ConnectionString
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'WEBSITE_ENABLE_SYNC_UPDATE_SITE'
          value: 'true'
        }
        {
          name: 'AzureWebJobsDisableHomepage'
          value: 'true'
        }
      ]
    }
  }
}

// Custom domain binding for the frontend function (optional)
resource functionFrontendCustomDomain 'Microsoft.Web/sites/hostNameBindings@2023-01-01' = if (!empty(customDomainName)) {
  parent: functionFrontend
  name: customDomainName
  properties: {
    hostNameType: 'Verified'
    sslState: 'SniEnabled'
    thumbprint: null
  }
}

// ========== Monitoring ==========
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: replace(appInsightName, 'appi', 'log')
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    } 
    retentionInDays: 30
    workspaceCapping: {
      dailyQuotaGb: 1
    }
  }
}

resource appInsight 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightName
  location: location
  kind: 'web'
  properties: {
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
    RetentionInDays: 30
    Application_Type: 'web'
  }
}

// ========== Cosmos DB (Table API) ==========
resource cosmosDb 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' = {
  kind: 'GlobalDocumentDB'
  name: cosmosDbName
  location: location
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        failoverPriority: 0
        locationName: location
        isZoneRedundant: cosmosDbisZoneRedundant
      }
    ]
    backupPolicy: {
      type: 'Periodic'
      periodicModeProperties: {
        backupIntervalInMinutes: 1440
        backupRetentionIntervalInHours: 48
        backupStorageRedundancy: 'Local'
      }
    }
    isVirtualNetworkFilterEnabled: false
    virtualNetworkRules: []
    ipRules: []
    enableMultipleWriteLocations: false
    capabilities: [
      {
        name: 'EnableTable'
      }
    ]
    enableFreeTier: true
  }
}

resource metadataTable 'Microsoft.DocumentDB/databaseAccounts/tables@2023-11-15' = {
  parent: cosmosDb
  name: 'metadata'
  properties: {
    resource: {
      id: 'metadata'
    }
  }
}

resource metricTable 'Microsoft.DocumentDB/databaseAccounts/tables@2023-11-15' = {
  parent: cosmosDb
  name: 'metrics'
  properties: {
    resource: {
      id: 'metrics'
    }
  }
}
