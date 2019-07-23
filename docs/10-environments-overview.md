# Render Hub Environments

Environments are a core concept of Azure Render Hub and encapsulate all the Azure resources required to deploy and 
maintain a render farm in Azure.

You can have a single environment, or multiple environments that could represent different
departments, cost centers or projects in your organization.  For example, you could have an Environment for your studio in LA and another for a studio in Vancouver.

<img src="images/environment.png" width="400" alt="Environment Diagram">

An Environment consists of the following Azure resources.

**Azure VNet and Subnet**

This is the Virtual Network with connectivity to your Qube Supervisor or Deadline Repository.  It is also where all your 
virtual machines will be deployed to ensure they can connect to the Supervisor or Repository.

See [here](https://docs.microsoft.com/en-us/azure/virtual-network/virtual-networks-overview) for more information.

**Azure Batch Account**

Azure Batch deploys Virtual Machines at scale.  It allows you to create a single virtual machine, or 10s of thousands of virtual machines.
Azure Batch abstracts the complexities of managing many virtual machine deployments and images to acheive this scale.

For more information on Azure Batch see [here](https://azure.microsoft.com/en-au/services/batch/).

Azure Batch also enables Pay-Per-Use (PPU) licensing for your rendering applications, if required.

PPU Licensing currently supports:

Autodesk 3ds Max
Autodesk Maya
Chaos Group VRay
Autodesk Arnold

For more information on Azure Batch Rendering see [here](https://azure.microsoft.com/en-au/services/batch/rendering/).

**Azure Storage**

By default an Azure Files share is created and can be used for input and output data.  Qube and Deadline each have methods to automatically mount a share on the render nodes.  See the Environment -> Storage tab for details.

**Key Vault**

A Key Vault service is created for each environment to store credentials such as domain credentials, database certificates (Deadline) and other sensitive information.

See [here](https://azure.microsoft.com/en-au/services/key-vault/) for more information.

**Application Insights**

Render Hub automatically installs the Application Insights agent on the render nodes to capture CPU, GPU and Rendering process metrics. 
This information is used to automatically scale down virtual machine pools as nodes become idle.

For more information see [here](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview).


# Create an Environment

For instructions on creating a new Environment see [here](11-environments-creation.md)

Return to the Render Hub [docs](README.md).
