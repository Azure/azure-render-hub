[![Build Status](https://dev.azure.com/azure/azure-render-farm-manager/_apis/build/status/Azure.azure-render-farm-manager?branchName=master)](https://dev.azure.com/azure/azure-render-farm-manager/_build/latest?definitionId=19&branchName=master)

# Azure Render Farm Manager (Preview)

The Azure Render Farm Manager is an Azure Web App to create and manage your cloud or hybrid render farm infrastructure.  The Render Farm Manager comes with native support for PipelineFx Qube! (6.10 and 7.0) and Thinkbox Deadline 10. Support for other render farm managers will be added in the future.

The portal Web App can be easily deployed into your existing Azure subscription as per the instructions referenced below.

## What the Render Farm Manager Does

* Create Azure infrastructure to extend your existing render farm
* Configures the resources to work together
* Provides usage and costs

## What the Render Farm Manager is Not

* A Render Manager, Queue Manager or Scheduler

It is a prerequisite that you have an existing Deadline or Qube render farm.

## What's Supported

The Render Farm Manager is currently in Public Preview and therefore a 'work in progress'.  The following scenarios
are currently supported.

**Deadline 10**
* Windows with Package Installation
* Windows with Custom Image
* Linux with Package Installation - **Not Supported**
* Linux with Custom Image

**Qube**
* Windows with Package Installation
* Windows with Custom Image
* Linux with Package Installation - **Not Supported**
* Linux with Custom Image - **Not Supported**

The unsupported items are in progress and will be available shortly.

## Deploying the Portal

For detailed deployment instructions see [here](docs/00-deployment.md).

Click the following link to start a deployment into your existing Azure subscription. The required input fields and prerequisites are described in the detailed [deployment instructions](docs/00-deployment.md).

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Fazure-render-farm-manager%2Fmaster%2FTemplates%2FAzureRenderFarmManager.json" target="_blank">
   <img alt="Deploy to Azure" src="http://azuredeploy.net/deploybutton.png"/>
</a>

## Documentation

For more information about the render farm manager see the docs [here](docs/README.md).

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
