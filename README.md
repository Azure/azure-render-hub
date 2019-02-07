[![Build Status](https://dev.azure.com/azure/azure-render-farm-manager/_apis/build/status/Azure.azure-render-farm-manager?branchName=master)](https://dev.azure.com/azure/azure-render-farm-manager/_build/latest?definitionId=19&branchName=master)

# Azure Render Farm Manager

The Azure Render Farm Manager is an Azure Web App to create and manage your cloud or hybrid render farm with native support for PipelineFx Qube! and Thinkbox Deadline 10.
The portal web app can be easily deployed into your existing Azure subscription.

# Deploying the Portal

Before you deploy the portal you'll need to create a Azure AD application to login.  You can create the application via the Azure cloud shell [here](https://shell.azure.com/powershell).

## Create an Azure AD Application

The AAD application is used by the portal to authenticate the user and request consent for the portal to access Azure resources as the user.  Creating the AAD application via the Azure portal will enable the application as an 'Enterprise Application' which means you can restrict portal access to specific users.  This is recommended, otherwise all users in your AAD organization will have access to the portal.  It should be noted that the portal uses delegated permissions to access Azure resources so even if a user can log into the portal, it does not guarantee they have access to create or read resources within the subscription.

### Using the Azure Portal

Login to the Azure portal and navigate to the Azure Active Directory application registration blade, or click [here](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps).

#### Create the AAD Application

 1. Click New application registration
 2. Enter an application name, e.g. RenderFarmManager
 3. For application type select Web app/ API
 4. Enter a sign-on URL - this is the URL of the Web App that you will deploy next.  The URL will be in the format, https://[MyWebAppName].azurewebsites.net.  You must ensure the name is globally unique and has not been used by anyone else.
 5. Click Create
 6. Note the Application ID as you'll need it later

#### Assign API Access Permissions

 1. Click Settings on the application
 2. Click Required permissions
 3. Click Add -> Select an API -> Select the Microsoft Graph API and 'Sign in and read user profile' under Delegated Permissions (near the bottom)
 4. Click Done to Save
 5. Click Add -> Select an API -> Select the Windows Azure Service Management API and 'Access Azure Service Management as organization users (preview)' under Delegated Permissions (near the bottom)
 6. Click Done to Save

#### Create a Client Key (Secret)

 1. Under the application Settings blade click Keys
 2. Under Passwords enter a description in the blank box, select Never expires and click Save
 3. Save the displayed Key somewhere safe, you'll need it later.  Note, the key cannot be accessed again.

#### Update the Reply URL

 1. On the application Settings blade click Reply URLs
 2. Edit the existing reply URL to append '/signin-oidc', the new URL should look like: https://[MyWebAppName].azurewebsites.net/signin-oidc
 3. Click Save

#### Set Application Access Permissions

The following instructions allow you to restrict access to specific users in your organisation.

 1. In the Azure portal navigate to Azure Active Directory -> Enterprise Applications
 2. Search for the AAD application you created above using the Application ID
 3. Click on Properties and click Yes for User Assignment Required
 4. Click Save
 5. Click Users and Groups
 6. Add each user that requires access to the portal

#### Get the AAD Tenant Id

In the Azure Portal navigate to Azure Active Directory -> Properties.  Note down the Directory ID, this is your Tenant ID that is required when you deploy the portal.

### Using Cloud Shell

Simply copy the script snippet below, update the $webAppName variable and paste the script below into the cloud shell to create a new AAD application.  Keep in mind the Web App name must be globally unique and be a valid DNS name as it becomes the host in your web sites URL, e.g. https://< webAppName >.azurewebsites.net.

```
$webAppName = "contosorenderfarmmanager"

# Create the application
$app = az ad app create --display-name $webAppName --identifier-uris http://$webAppName --end-date 2040-12-31 --homepage "https://$webAppName.azurewebsites.net" --reply-urls "https://$webAppName.azurewebsites.net/signin-oidc"

# Assign the required API permissions

# Windows Azure Active Directory - Sign in and read user profile
az ad app permission add --id ($app | ConvertFrom-Json).appId --api 00000002-0000-0000-c000-000000000000 --api-permissions 311a71cc-e848-46a1-bdf8-97ff7156d8e6=Scope

# Graph API - Sign in and read user profile
az ad app permission add --id ($app | ConvertFrom-Json).appId --api 00000003-0000-0000-c000-000000000000 --api-permissions e1fe6dd8-ba31-4d61-89e7-88639da4683d=Scope

# Windows Azure Service Management API - Access Azure Service Management as organization users (preview)
az ad app permission add --id ($app | ConvertFrom-Json).appId --api 797f4846-ba00-4fd7-ba43-dac1f8f63013 --api-permissions 41094075-9dad-400e-a0bd-54e686782033=Scope

# Print the app details to the shell
$app

```

## Deploying the Portal

Click the following link to start a deployment into your existing Azure subscription.  The required input fields are described in detail below.

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Fazure-render-farm-manager%2Fmaster%2FTemplates%2FAzureRenderFarmManager.json" target="_blank">
   <img alt="Deploy to Azure" src="http://azuredeploy.net/deploybutton.png"/>
</a>

 - webSiteName - The Azure Web App name.  This must be globally unique and is also part of the websites DNS name.
 - hostingPlanName - The name of the hosting plan service, you can leave the default.
 - skuTier* - The Hosting Plan tier that determines the performance and cost for the Web App.  Plans and prices are available [here](https://azure.microsoft.com/en-au/pricing/details/app-service/plans/).
 - skuSize* - The instance size in the hosting plan tier, choose F1 for the free tier, SX for shared, BX for Basic, PX for premium.
 - aadTenantId - the AAD application tenant Id from the application you created above.
 - aadDomain - the AAD tenant domain, i.e. constos.microsoft.com.
 - aadClientId - the AAD application (or client) Id from above.
 - aadClientSecret - the AAD application/client secret from above.

\* Note that auto scale functionality requires the web site to always be running which requires Basic/B1 SKU or higher.
 
After submitting the deployment your instance of the Portal will be deployed into your subscription.  You'll see a link to the deployment to monitor it's progress.

## Accessing the Portal

Once the deployment is complete you can access the portal at: https://[webSiteName].azurewebsites.net

## Updating the Portal

If updates or fixes have been pushed to the Azure Render Farm Manager portal repository you can deploy the latest changes with the click of a button.  Login to the Azure [portal](https://portal.azure.com) and navigate to the portals Resource Group -> Web App.  Click on Deployment Center and then click Sync to pull the latest changes.

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
