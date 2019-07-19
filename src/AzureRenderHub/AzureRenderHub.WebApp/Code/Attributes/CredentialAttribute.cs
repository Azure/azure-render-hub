// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace WebApp.Code.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CredentialAttribute : Attribute
    {
        private readonly string _name;

        public CredentialAttribute(string name)
        {
            _name = name;
        }

        public virtual string Name => _name;
    }
}
