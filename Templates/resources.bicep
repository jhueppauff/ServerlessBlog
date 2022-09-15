param functionEngineName string = 'func-blog-engine-we-prod-001'
param functionFrontendName string = 'func-blog-engine-we-prod-001'
param profileProperties object
param endpointProperties object
param aadClientId string
param aadTenant string
param cosmosDbisZoneRedundant bool
param cosmosDbName string
param staticWebAppName string
param location string = 'westeurope'

var appInsightName_var = replace(functionEngineName, 'func', 'appi')
var appPlanName_var = replace(functionEngineName, 'func', 'plan')
var storageNameWeb_var = 'stblogstaticweprod001'
var storageFunction_var = 'stblogfuncweprod001'
var cdnProfileName_var = replace(functionEngineName, 'func', 'cdn')
var cdnEndpointName = replace(functionFrontendName, 'func', 'cdnedp')

resource staticWebApp 'Microsoft.Web/staticSites@2022-03-01' = {
  name: staticWebAppName
  location: location
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

resource storageWeb 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: storageNameWeb_var
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource storageNameWeb_default 'Microsoft.Storage/storageAccounts/blobServices@2022-05-01' = {
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

resource storageFunction 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: storageFunction_var
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

resource appPlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appPlanName_var
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
  }
}

resource managedIdentityEngine 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: 'msi${functionEngineName}'
  location: location
}

resource managedIdentityFrontend 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: 'msi${functionFrontendName}'
  location: location
}

resource functionEngine 'Microsoft.Web/sites@2022-03-01' = {
  name: functionEngineName
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
       '${managedIdentityEngine.id}': {}
    }
  }
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: appPlan.id
    siteConfig: {
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageFunction_var};AccountKey=${storageFunction.listKeys().keys[0].value}'
        }
        {
          name: 'AzureStorageConnection'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageNameWeb_var};AccountKey=${storageWeb.listKeys().keys[0].value}'
        }
        {
          name: 'CosmosDBConnection'
          value: 'DefaultEndpointsProtocol=https;AccountName=${cosmosDbName};AccountKey=${listKeys(cosmosDb.id, '2020-04-01').primaryMasterKey};TableEndpoint=https://${cosmosDbName}.table.cosmos.azure.com:443/;'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageFunction_var};AccountKey=${storageFunction.listKeys().keys[0].value}'
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
          value: reference(appInsight.id, '2015-05-01').InstrumentationKey
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
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

resource functionEngine_authsettings 'Microsoft.Web/sites/config@2016-08-01' = {
  parent: functionEngine
  name: 'authsettings'
  properties: {
    enabled: true
    unauthenticatedClientAction: 'RedirectToLoginPage'
    tokenStoreEnabled: true
    defaultProvider: 'AzureActiveDirectory'
    clientId: aadClientId
    issuer: 'https://sts.windows.net/${aadTenant}/'
  }
}

resource functionFrontend 'Microsoft.Web/sites@2022-03-01' = {
  name: functionFrontendName
  location: location
  identity: {
     type: 'UserAssigned'
     userAssignedIdentities: {
        '${managedIdentityFrontend.id}': {}
     }
  }
  kind: 'functionapp'
  properties: {
    serverFarmId: appPlan.id
    siteConfig: {
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageFunction_var};AccountKey=${storageFunction.listKeys().keys[0].value}'
        }
        {
          name: 'AzureStorageConnection'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageNameWeb_var};AccountKey=${storageWeb.listKeys().keys[0].value}'
        }
        {
          name: 'CosmosDBConnection'
          value: 'DefaultEndpointsProtocol=https;AccountName=${cosmosDbName};AccountKey=${listKeys(cosmosDb.id, '2020-04-01').primaryMasterKey};TableEndpoint=https://${cosmosDbName}.table.cosmos.azure.com:443/;'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageFunction_var};AccountKey=${storageFunction.listKeys().keys[0].value}'
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
          value: reference(appInsight.id, '2015-05-01').InstrumentationKey
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
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

resource cdnProfileName 'microsoft.cdn/profiles@2019-04-15' = {
  name: cdnProfileName_var
  location: location
  sku: {
    name: 'Standard_Microsoft'
  }
  properties: profileProperties
}

resource cdnProfileName_cdnEndpointName 'microsoft.cdn/profiles/endpoints@2019-04-15' = {
  parent: cdnProfileName
  name: cdnEndpointName
  location: location
  properties: endpointProperties
}

resource appInsight 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightName_var
  location: location
  kind: 'web'
  properties: {
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
    RetentionInDays: 30
    Application_Type: 'web'
    Flow_Type: 'Redfield'
    Request_Source: 'IbizaAIExtension'
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2021-12-01-preview' = {
  name: replace(appInsightName_var, 'appi', 'log')
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

resource cosmosDb 'Microsoft.DocumentDB/databaseAccounts@2022-05-15' = {
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

resource metadataTable 'Microsoft.DocumentDB/databaseAccounts/tables@2021-04-15' = {
  parent: cosmosDb
  name: 'metadata'
  properties: {
    resource: {
      id: 'metadata'
    }
  }
}

resource metricTable 'Microsoft.DocumentDB/databaseAccounts/tables@2021-04-15' = {
  parent: cosmosDb
  name: 'metrics'
  properties: {
    resource: {
      id: 'metrics'
    }
  }
}
