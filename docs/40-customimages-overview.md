# Custom Images

Custom images are useful when you require plugins or rendering applications that are not available in the Azure Batch Rendering
image.  Custom images are also useful for pre-installing and configuring your render manager agents or slaves.

## General Setup

### Windows

The Windows setup script utilises the AzureRM powershell commandlets for Azure calls.  To speed up virtual machine creation it's 
recommended that you pre-install the powershell commandlets.

```
Install-Module -Name AzureRm -Repository PSGallery -Force -Scope AllUsers
```

### Linux

Always update you Linux packages to ensure you have the latest patches and security updates.

## Qube!

The Qube! worker should be downloaded and installed as per the PipelineFx Qube! working installation instructions.

## Deadline

Download and install the Deadline Client Slave as per the Thinkbox Deadline instructions.

It's recommended that the Deadline Launcher and Slave run in an interactive session, this ensures all features, including GPU support,
work as expected, for this reason it's not recommended to run as a service.
