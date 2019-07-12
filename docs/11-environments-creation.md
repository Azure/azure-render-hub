# Creating an Environment

# Pre-requisites

You'll need the required Azure permissions to create new resources and assign roles.  The wizard breaks up the resource creation and 
role assignment steps, allowing someone else with role assignment permissions to complete the step, if needed.

# Step 1 - Environment Name and Location

In step 1 you'll specify a name for your environment which will define a prefix for the Azure resources.  The environment name should
identify the department, location or some other identifier for the render farm.

The following information must be specified.

1. Render farm name
2. Render farm manager - Qube 6.10, 7.0 or Deadline 10 (more coming soon)
3. Subscription ID - The Azure Subscription the resources should be deployed into.
4. Location - The Azure location or region the resources will be deployed.  
If you have an existing Azure ExpressRoute or VPN VNet you should use its location.

# Step 2 - Resources

On step 2 you'll define the resource names for each component in your new environment.  If you have existing resources they can be selected 
by clicking the 'Select existing xxx' link and selecting the resource from the drop down.  Only resources from the previously selected Subscription will be shown.