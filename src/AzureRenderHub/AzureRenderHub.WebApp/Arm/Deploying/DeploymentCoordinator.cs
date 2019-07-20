using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Operations;
using WebApp.Providers.Templates;

namespace AzureRenderHub.WebApp.Arm.Deploying
{
    public class DeploymentCoordinator : IDeploymentCoordinator
    {
        private readonly ITemplateProvider _templateProvider;
        private readonly IManagementClientProvider _managementClientProvider;

        public DeploymentCoordinator(
            ITemplateProvider templateProvider,
            IManagementClientProvider managementClientProvider)
        {
            _templateProvider = templateProvider;
            _managementClientProvider = managementClientProvider;
        }

        public async Task<Deployment> BeginDeploymentAsync(IDeployable deployment)
        {
            using (var client = await _managementClientProvider.CreateResourceManagementClient(deployment.SubscriptionId))
            {
                await client.ResourceGroups.CreateOrUpdateAsync(
                    deployment.ResourceGroupName,
                    new ResourceGroup {
                        Location = deployment.Location
                    });

                var templateParams = deployment.DeploymentParameters;

                var properties = new Microsoft.Azure.Management.ResourceManager.Models.Deployment
                {
                    Properties = new DeploymentProperties
                    {
                        Template = await _templateProvider.GetTemplate(deployment.TemplateName),
                        Parameters = _templateProvider.GetParameters(templateParams),
                        Mode = DeploymentMode.Incremental
                    }
                };

                // Start the ARM deployment
                var deploymentResult = await client.Deployments.BeginCreateOrUpdateAsync(
                    deployment.ResourceGroupName,
                    deployment.DeploymentName,
                    properties);

                return ToDeploymentStatus(deploymentResult);
            }
        }

        public async Task<Deployment> GetDeploymentAsync(IDeployable deployment)
        {
            using (var client = await _managementClientProvider.CreateResourceManagementClient(deployment.SubscriptionId))
            {
                return await GetDeploymentInnerAsync(deployment);
            }
        }

        public async Task<Deployment> WaitForCompletionAsync(IDeployable deployment)
        {
            using (var client = await _managementClientProvider.CreateResourceManagementClient(deployment.SubscriptionId))
            {
                var deploymentResult = await GetDeploymentInnerAsync(deployment);
                while (deploymentResult.ProvisioningState == ProvisioningState.Running)
                {
                    await Task.Delay(2000);
                    deploymentResult = await GetDeploymentInnerAsync(deployment);
                }
                return deploymentResult;
            }
        }

        private async Task<Deployment> GetDeploymentInnerAsync(IDeployable deployment)
        {
            using (var client = await _managementClientProvider.CreateResourceManagementClient(deployment.SubscriptionId))
            {
                var deploymentResult = await client.Deployments.GetAsync(
                        deployment.ResourceGroupName,
                        deployment.DeploymentName);
                return ToDeploymentStatus(deploymentResult);
            }
        }

        private Deployment ToDeploymentStatus(DeploymentExtended deployment)
        {
            Enum.TryParse<ProvisioningState>(deployment.Properties.ProvisioningState, out var deploymentState);
            return new Deployment
            {
                ProvisioningState = deploymentState,
            };
        }
    }
}
