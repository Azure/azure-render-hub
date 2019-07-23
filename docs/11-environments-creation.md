# Creating an Environment

# Pre-requisites

You'll need the required Azure permissions to create new resources and assign roles.  The wizard breaks up the resource creation and 
role assignment steps, allowing someone else with role assignment permissions to complete the step, if needed.

# Step 1 - Environment Name and Location

In step 1 you'll specify a name for your environment which will define a prefix for the Azure resources.  The environment name should
identify the department, location or some other identifier for the Render Hub environment.

The following information needs to be specified.

1. Environment name
2. Render manager - Qube 6.10, 7.0 or Deadline 10 (more coming soon)
3. Subscription ID - The Azure Subscription the resources should be deployed into.
4. Location - The Azure location or region the resources will be deployed.  
If you have an existing Azure ExpressRoute or VPN VNet you should use its location.

# Step 2 - Resources

On step 2 you'll define the resource names for each component in your new environment.  If you have existing resources they can be selected 
by clicking the 'Select existing xxx' link and selecting the resource from the drop down.  Only resources from the previously selected Subscription will be shown.

# Step 3 - Identity

Step three creates a Service Principal Identity for Key Vault access.  The identity creates a certificate for authentication and is used by the virtual machines
to access any required secrets in Key Vault.  The certificate is automatically uploaded to the Azure Batch account and can be referenced by Pool virtual machines.

# Step 4 - Render Manager Configuration

On step four you specify any render manager specific information.

For PipelineFx Qube configuration instructions see [here](12-environments-qube.md).

For Thinkbox Deadline 10 configuration instructions see [here](13-environments-deadline.md).

Return to the Render Hub [docs](README.md).
