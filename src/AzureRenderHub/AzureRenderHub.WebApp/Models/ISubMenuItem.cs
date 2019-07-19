// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WebApp.Models
{
    public interface ISubMenuItem
    {
        string Id { get; }

        string DisplayName { get; }

        bool Enabled { get; }
    }
}
