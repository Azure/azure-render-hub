[![Build Status](https://dev.azure.com/azure/azure-render-hub/_apis/build/status/Azure.azure-render-hub?branchName=master)](https://dev.azure.com/azure/azure-render-hub/_build/latest?definitionId=19&branchName=master)

# Azure Render Hub

The Azure Render Hub is an Azure Web App that simplifies the creation and managment of your hybrid or cloud rendering infrastructure.  
Azure Render Hub comes with native support for deploying PipelineFx Qube! (6.10 and 7.0) and Thinkbox Deadline 10. 
Support for other render farm managers, like OpenCue, will be added in the future.

The portal Web App can be easily deployed into your existing Azure subscription as per the instructions referenced below.

## What Render Hub Does

* Create Azure infrastructure to extend your existing render farm
* Configures the resources to work together
* Provides usage and costs

## What Render Hub is Not

* A Render Manager, Queue Manager or Scheduler

It is a prerequisite that you have an existing Deadline or Qube render farm.

## What's Supported

The following scenarios are currently supported.

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

## Deploying

For deployment instructions see [here](docs/00-deployment.md).

Click the following link to start a deployment into your existing Azure subscription. 
The required input fields and prerequisites are described in the detailed [deployment instructions](docs/00-deployment.md).

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Fazure-render-hub%2Fmaster%2FTemplates%2FAzureRenderHub.json" target="_blank" rel="noopener">
   <img alt="Deploy to Azure" src="http://azuredeploy.net/deploybutton.png"/>
</a>

## Documentation

For more information about Azure Render Hub see the docs [here](docs/README.md).

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
