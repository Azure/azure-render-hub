// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Rest.Azure;

namespace WebApp.Code.Extensions
{
    public static class CloudExceptionExtensions
    {
        public static string ToFriendlyString(this CloudException ex)
        {
            StringBuilder sb = new StringBuilder($"Message={ex.Message}");
            if (ex.Body != null)
            {
                AppendCloudError(sb, ex.Body);
            }
            sb.AppendLine($"Stack={ex}");
            return sb.ToString();
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
