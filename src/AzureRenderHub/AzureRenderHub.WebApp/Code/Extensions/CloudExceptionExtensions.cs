// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Rest.Azure;

namespace WebApp.Code.Extensions
{
    public static class CloudExceptionExtensions
    {
        public static bool ResourceNotFound(this CloudException ce)
        {
            return ce.Response.StatusCode == HttpStatusCode.NotFound
                || ce.Body?.Code != "ResourceGroupNotFound"
                || ce.Body?.Code != "ResourceNotFound"
                || ce.Body?.Code != "NotFound";
        }

        public static string ToFriendlyString(this CloudException ex, bool includeStack = true)
        {
            StringBuilder sb = new StringBuilder($"Message={ex.Message}");
            if (ex.Body != null)
            {
                var additionalInfo = ex.Body.AdditionalInfo == null
                    ? "None"
                    : string.Join(", ", ex.Body.AdditionalInfo.Select(ai => $"{ai.Type}={ai.Info}"));

                sb.Append(additionalInfo);

                AppendCloudError(sb, ex.Body);
            }
            if (includeStack)
            {
                sb.AppendLine($"Stack={ex}");
            }
            return sb.ToString();
        }

        public static void AddModelErrors(this CloudException ex, ModelStateDictionary modelState)
        {
            var errors = ex.ToFriendlyString(false);
            var lines = errors.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                modelState.AddModelError("", line);
            }
        }

        private static void AppendCloudError(StringBuilder sb, CloudError error)
        {
            sb.AppendLine($"Code={error.Code}");
            sb.AppendLine($"Message={error.Message}");
            sb.AppendLine($"Target={error.Target}");
            if (error.Details != null)
            {
                foreach (var bodyDetail in error.Details)
                {
                    AppendCloudError(sb, bodyDetail);
                }
            }
        }
    }
}
