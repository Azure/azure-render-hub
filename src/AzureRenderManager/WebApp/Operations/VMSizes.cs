// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using WebApp.Code.Contract;
using WebApp.Config;

namespace WebApp.Operations
{
    public sealed class VMSizes : IVMSizes
    {
        private readonly IManagementClientProvider _managementClientProvider;

        public VMSizes(IManagementClientProvider managementClient)
        {
            _managementClientProvider = managementClient;
        }

        public async Task<IReadOnlyList<VirtualMachineSize>> GetSizes(RenderingEnvironment environment)
        {
            bool IsSupportedSize(VirtualMachineSize vmSize)
            {
                var size = vmSize.Name.Substring(vmSize.Name.IndexOf('_') + 1);
                return
                    size.StartsWith("D", StringComparison.Ordinal) ||
                    size.StartsWith("NC", StringComparison.Ordinal) ||
                    size.StartsWith("F", StringComparison.Ordinal) ||
                    size.StartsWith("H", StringComparison.Ordinal);
            }

            var sizes = new List<VirtualMachineSize>();

            using (var computeClient = await _managementClientProvider.CreateComputeManagementClient(environment.SubscriptionId))
            {
                var vmSizes = await computeClient.VirtualMachineSizes.ListAsync(environment.BatchAccount.Location);
                sizes.AddRange(vmSizes.Where(IsSupportedSize));
            }

            return sizes;
        }

        public async Task<IReadOnlyList<SelectListItem>> GetSizesSelectList(RenderingEnvironment environment)
        {
            var sizes = await GetSizes(environment);

            string RenderMB(int? mb)
            {
                if (mb == null)
                {
                    return "???";
                }

                if (mb >= 1024 * 1024)
                {
                    return $"{mb / 1024.0 / 1024.0:N1} TB";
                }

                if (mb >= 1024)
                {
                    return $"{mb / 1024.0:N1} GB";
                }

                return $"{mb} MB";
            }

            var list = new List<SelectListItem>();
            foreach (var tier in sizes.GroupBy(v => v.Name.Substring(0, v.Name.IndexOf('_'))))
            {
                var group = new SelectListGroup {Name = tier.Key};
                foreach (var sku in tier)
                {
                    var description = $"{sku.Name.Substring(sku.Name.IndexOf('_') + 1)} ({sku.NumberOfCores} {(sku.NumberOfCores > 1 ? "vCPUs" : "vCPU")}, {RenderMB(sku.MemoryInMB)} RAM)";
                    list.Add(new SelectListItem(description, sku.Name) {Group = group});
                }
            }

            return list;
        }
    }
}