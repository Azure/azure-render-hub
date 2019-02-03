// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WebApp.Providers.Resize
{
    public interface IScaleUpRequestStore
    {
        Task Add(string envName, string poolName, int requested);

        Task<ScaleUpRequestEntity> Get(string envName, string poolName, CancellationToken ct);

        Task<IReadOnlyList<ScaleUpRequestEntity>> List(CancellationToken ct);

        Task Delete(ScaleUpRequestEntity request);
    }
}
