# Render Farm Environments

Environments are a core concept of the Render Farm Manager and encapsulate all the Azure resources required to deploy and 
maintain a render farm in Azure.

An Environment consists of the following Azure resources.

**Azure VNet and Subnet**

This is the Virtual Network with connectivity to your Qube Supervisor or Deadline Repository.  It is also where all your 
virtual machines will be deployed to ensure they can connect to the Supervisor or Repository.

See [here]() for more information.

**Azure Batch Account**

Azure Batch deploys Virtual Machines at scale.  It allows you to create a single virtual machine, or 10s of thousands of virtual machines.
Azure Batch abstracts the complexities of managing many virtual machine deployments and images to acheive this scale.

For more information on Azure Batch see [here](https://azure.microsoft.com/en-au/services/batch/).

Azure Batch also enables Pay-Per-Use (PPU) licensing for your rendering applications, if requires.

PPU Licensing currently supports:

Autodesk 3ds Max
Autodesk Maya
Chaos Group VRay
Autodesk Arnold

For more information on Azure Batch Rendering see [here](https://azure.microsoft.com/en-au/services/batch/rendering/).

