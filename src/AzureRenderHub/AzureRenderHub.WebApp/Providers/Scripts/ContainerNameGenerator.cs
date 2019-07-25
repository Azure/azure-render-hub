using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureRenderHub.WebApp.Providers.Scripts
{
    public class ContainerNameGenerator : IContainerNameGenerator
    {
        public string GetContainerName(string poolName)
        {
            return $"pool-{CreateMD5(poolName).ToLower()}";
        }

        // From MSDN https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.md5?redirectedfrom=MSDN&view=netframework-4.8
        private static string CreateMD5(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

                StringBuilder sBuilder = new StringBuilder();
                
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                
                return sBuilder.ToString();
            }
        }
    }
}
