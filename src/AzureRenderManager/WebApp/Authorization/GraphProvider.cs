using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Web.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApp.Code.Extensions;
using WebApp.Operations;

namespace WebApp.Authorization
{
    public class GraphProvider : NeedsAccessToken, IGraphProvider
    {
        private static string[] DirectoryObjectTypes = new[] { "user" };

        private readonly IConfiguration _configuration;
        private readonly string[] _scopes;

        public GraphProvider(
            IConfiguration configuration,
            ITokenAcquisition tokenAcquisition,
            IHttpContextAccessor contextAccessor) : base(contextAccessor, tokenAcquisition)
        {
            _configuration = configuration;
            var adOptions = new AzureAdOptions();
            configuration.Bind("AzureAd", adOptions);
            _scopes = adOptions.GraphScopes.Split(new[] { ' ' });
        }

        public async Task<Dictionary<string, User>> LookupObjectIdsAsync(ClaimsPrincipal claim, IList<string> userIds)
        {
            var graphServiceClient = new GraphServiceClient(new DelegateAuthenticationProvider(async requestMessage =>
            {
                var graphAccessToken = await GetAccessTokenWithGraphScope();
                requestMessage
                    .Headers
                    .Authorization = new AuthenticationHeaderValue("Bearer", graphAccessToken);
            }));

            // The max Graph objects you can fetch in a single request.
            const int batchSize = 1000;

            var userDict = new Dictionary<string, User>();

            try
            {
                for (int i = 0; i < userIds.Count; i = i + batchSize)
                {
                    await GetBatchOfUsers(graphServiceClient, userIds, i, batchSize, userDict);
                }
            }
            catch (ServiceException se) when (se.Error?.Code == "Authorization_RequestDenied")
            {
                // No Graph API Permissions, let's ignore this for tenants that don't support this.
            }

            return userDict;
        }

        public async Task<User> GetUser(ClaimsPrincipal claim, string userEmailAddress)
        {
            var graphServiceClient = new GraphServiceClient(new DelegateAuthenticationProvider(async requestMessage =>
            {
                var graphAccessToken = await GetAccessTokenWithGraphScope();
                requestMessage
                    .Headers
                    .Authorization = new AuthenticationHeaderValue("Bearer", graphAccessToken);
            }));

            var externalEmail = $"{userEmailAddress.Replace('@', '_')}#EXT#@{_configuration["AzureAd:Domain"]}";
            var user = await graphServiceClient.Users.Request().Filter(
                $"mail eq '{userEmailAddress}' or " +
                $"userPrincipalName eq '{userEmailAddress}' or " +
                $"userPrincipalName eq '{externalEmail}'").GetAsync();

            if (user == null || user.Count == 0)
            {
                throw new Exception($"User {userEmailAddress} not found");
            }

            if (user.Count > 1)
            {
                throw new Exception($"Multiple users found for email {userEmailAddress}");
            }

            return user.First();
        }

        private async Task GetBatchOfUsers(GraphServiceClient graphServiceClient, IList<string> userIds, int batch, int batchSize, Dictionary<string, User> targetUserDictionary)
        {
            var items = userIds.Skip(batch).Take(batchSize);
            var dirObjects = await graphServiceClient.DirectoryObjects.GetByIds(userIds, DirectoryObjectTypes).Request().PostAsync();
            var dirUsers = dirObjects.Cast<User>().ToDictionary(k => k.Id);
            foreach (var userObject in dirObjects.Cast<User>())
            {
                userObject.UserPrincipalName = SanitizeUserPrincipalName(userObject.UserPrincipalName);
                targetUserDictionary[userObject.Id] = userObject;
            }
        }

        private string SanitizeUserPrincipalName(string userPrincipalName)
        {
            if (userPrincipalName != null && userPrincipalName.Contains("#EXT#@"))
            {
                // Guest accounts have an encoded User Principal Name in the format:
                // john.doe_contoso.com#EXT#@contoso.onmicrosoft.com
                // where '@' in the email is replaced with '_'.
                var encodedEmail = userPrincipalName.Split("#EXT#@")[0];
                return encodedEmail.Replace("_", "@");
            }
            return userPrincipalName;
        }

        private async Task<string> GetAccessTokenWithGraphScope()
        {
            return await GetAccessToken(_scopes.FirstOrDefault());
        }
    }
}
