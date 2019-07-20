using System.Threading.Tasks;

namespace AzureRenderHub.WebApp.Arm.Deploying
{
    public interface IDeploymentCoordinator
    {
        Task<Deployment> BeginDeploymentAsync(IDeployable deployment);

        Task<Deployment> GetDeploymentAsync(IDeployable deployment);

        Task<Deployment> WaitForCompletionAsync(IDeployable deployment);
    }
}
