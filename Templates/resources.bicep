param functionEngineName string = 'func-blog-engine-we-prod-001'
param functionFrontendName string = 'func-blog-engine-we-prod-001'
param profileProperties object
param endpointProperties object
param aadClientId string
param aadTenant string
param cosmosDbisZoneRedundant string
param cosmosDbName string
param staticWebAppName string

var appInsightName_var = replace(functionEngineName, 'func', 'appi')
var appPlanName_var = replace(functionEngineName, 'func', 'plan')
var storageNameWeb_var = 'stblogstaticweprod001'
var storageFunction_var = 'stblogfuncweprod001'
var cdnProfileName_var = replace(functionEngineName, 'func', 'cdn')
var cdnEndpointName = replace(functionFrontendName, 'func', 'cdnedp')

resource staticWebAppName_resource 'Microsoft.Web/staticSites@2019-12-01-preview' = {
  name: staticWebAppName
  location: resourceGroup().location
  tags: {
  }
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

resource storageNameWeb 'Microsoft.Storage/storageAccounts@2019-06-01' = {
  name: storageNameWeb_var
  location: resourceGroup().location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
    tier: 'Standard'
  }
  properties: {
    supportsHttpsTrafficOnly: false
  }
}

resource storageNameWeb_default 'Microsoft.Storage/storageAccounts/blobServices@2021-04-01' = {
  parent: storageNameWeb
  name: 'default'
  sku: {
    name: 'Standard_LRS'
    tier: 'Standard'
  }
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

resource storageFunction 'Microsoft.Storage/storageAccounts@2018-07-01' = {
  name: storageFunction_var
  location: resourceGroup().location
  sku: {
    name: 'Standard_LRS'
  }
  tags: {
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
    accountType: 'Standard_LRS'
    tier: 'Standard'
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
  }
}

resource appPlanName 'Microsoft.Web/serverfarms@2018-02-01' = {
  name: appPlanName_var
  location: resourceGroup().location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    name: appPlanName_var
    computeMode: 'Dynamic'
  }
}

resource functionEngineName_resource 'Microsoft.Web/sites@2015-08-01' = {
  name: functionEngineName
  location: resourceGroup().location
  kind: 'functionapp'
  properties: {
    serverFarmId: appPlanName.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageFunction_var};AccountKey=${listKeys(storageFunction.id, providers('Microsoft.Storage', 'storageAccounts').apiVersions[0]).keys[0].value}'
        }
        {
          name: 'AzureStorageConnection'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageNameWeb_var};AccountKey=${listKeys(storageNameWeb.id, providers('Microsoft.Storage', 'storageAccounts').apiVersions[0]).keys[0].value}'
        }
        {
          name: 'CosmosDBConnection'
          value: 'DefaultEndpointsProtocol=https;AccountName=${cosmosDbName};AccountKey=${listKeys(cosmosDbName_resource.id, '2020-04-01').primaryMasterKey};TableEndpoint=${cosmosDbName_resource.properties.tableEndpoint};'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageFunction_var};AccountKey=${listKeys(storageFunction.id, providers('Microsoft.Storage', 'storageAccounts').apiVersions[0]).keys[0].value}'
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
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: reference(appInsightName.id, '2015-05-01').InstrumentationKey
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: 1
        }
        {
          name: 'WEBSITE_ENABLE_SYNC_UPDATE_SITE'
          value: true
        }
        {
          name: 'AzureWebJobsDisableHomepage'
          value: 'true'
        }
      ]
    }
  }
}

resource functionEngineName_authsettings 'Microsoft.Web/sites/config@2016-08-01' = {
  parent: functionEngineName_resource
  name: 'authsettings'
  location: resourceGroup().location
  properties: {
    enabled: true
    unauthenticatedClientAction: 'RedirectToLoginPage'
    tokenStoreEnabled: true
    defaultProvider: 'AzureActiveDirectory'
    clientId: aadClientId
    issuer: 'https://sts.windows.net/${aadTenant}/'
  }
}

resource functionFrontendName_resource 'Microsoft.Web/sites@2015-08-01' = {
  name: functionFrontendName
  location: resourceGroup().location
  kind: 'functionapp'
  properties: {
    serverFarmId: appPlanName.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageFunction_var};AccountKey=${listKeys(storageFunction.id, providers('Microsoft.Storage', 'storageAccounts').apiVersions[0]).keys[0].value}'
        }
        {
          name: 'AzureStorageConnection'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageNameWeb_var};AccountKey=${listKeys(storageNameWeb.id, providers('Microsoft.Storage', 'storageAccounts').apiVersions[0]).keys[0].value}'
        }
        {
          name: 'CosmosDBConnection'
          value: 'DefaultEndpointsProtocol=https;AccountName=${cosmosDbName};AccountKey=${listKeys(cosmosDbName_resource.id, '2020-04-01').primaryMasterKey};TableEndpoint=${cosmosDbName_resource.properties.tableEndpoint};'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageFunction_var};AccountKey=${listKeys(storageFunction.id, providers('Microsoft.Storage', 'storageAccounts').apiVersions[0]).keys[0].value}'
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
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: reference(appInsightName.id, '2015-05-01').InstrumentationKey
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: 1
        }
        {
          name: 'WEBSITE_ENABLE_SYNC_UPDATE_SITE'
          value: true
        }
        {
          name: 'AzureWebJobsDisableHomepage'
          value: 'true'
        }
      ]
    }
  }
}

resource cdnProfileName 'microsoft.cdn/profiles@2019-04-15' = {
  name: cdnProfileName_var
  location: resourceGroup().location
  sku: {
    name: 'Standard_Microsoft'
  }
  properties: profileProperties
}

resource cdnProfileName_cdnEndpointName 'microsoft.cdn/profiles/endpoints@2019-04-15' = {
  parent: cdnProfileName
  name: cdnEndpointName
  location: resourceGroup().location
  properties: endpointProperties
}

resource appInsightName 'microsoft.insights/components@2014-08-01' = {
  name: appInsightName_var
  location: resourceGroup().location
  properties: {
    ApplicationId: appInsightName_var
    Application_Type: 'web'
    Flow_Type: 'Redfield'
    Request_Source: 'IbizaAIExtension'
  }
}

resource cosmosDbName_resource 'Microsoft.DocumentDb/databaseAccounts@2020-04-01' = {
  kind: 'GlobalDocumentDB'
  name: cosmosDbName
  location: resourceGroup().location
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        id: '${cosmosDbName}-${resourceGroup().location}'
        failoverPriority: 0
        locationName: resourceGroup().location
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
    dependsOn: []
    enableMultipleWriteLocations: false
    capabilities: [
      {
        name: 'EnableTable'
      }
    ]
    enableFreeTier: true
  }
}

resource metadataTable 'Microsoft.DocumentDB/databaseAccounts/tables@2021-04-15' = {
  parent: cosmosDbName_resource
  name: 'metadata'
  properties: {
    resource: {
      id: 'metadata'
    }
  }
}

resource metricTable 'Microsoft.DocumentDB/databaseAccounts/tables@2021-04-15' = {
  parent: cosmosDbName_resource
  name: 'metrics'
  properties: {
    resource: {
      id: 'metrics'
    }
  }
}
