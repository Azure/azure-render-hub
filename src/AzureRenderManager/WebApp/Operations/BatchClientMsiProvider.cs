// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Services.AppAuthentication;
using WebApp.Config;

namespace WebApp.Operations
{
    public sealed class BatchClientMsiProvider
    {
        private const string BatchResourceUri = "https://batch.core.windows.net/";

        public BatchClient CreateBatchClient(RenderingEnvironment environment)
        {
            var url = environment.BatchAccount.Url;
            Func<Task<string>> tokenProvider = GetAuthenticationTokenAsync;
            return BatchClient.Open(new BatchTokenCredentials(url, tokenProvider));
        }

        private static async Task<string> GetAuthenticationTokenAsync()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            return await azureServiceTokenProvider.GetAccessTokenAsync(BatchResourceUri);
        }
    }
}