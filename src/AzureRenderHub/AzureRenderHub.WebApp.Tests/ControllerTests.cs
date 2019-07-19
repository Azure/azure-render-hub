using System.Linq;

using Microsoft.AspNetCore.Mvc;
using WebApp.Code.Attributes;
using WebApp.Controllers;
using Xunit;

namespace WebApp.Tests
{
    public class ControllerTests
    {
        [Fact]
        public void AllControllersAreSubclassesOfBaseController()
        {
            // this makes sure that the controllers have appropriate security settings

            var controllers =
                typeof(BaseController).Assembly.GetExportedTypes()
                    .Where(c => c.IsSubclassOf(typeof(Controller)));

            Assert.All(controllers, c =>
                Assert.True(c.IsSubclassOf(typeof(BaseController)) ||
                            c.IsDefined(typeof(AuthorizeEnvironmentEndpointAttribute), false) ||
                            c == typeof(AccountController) ||
                            c == typeof(BaseController)));
        }
    }
}
