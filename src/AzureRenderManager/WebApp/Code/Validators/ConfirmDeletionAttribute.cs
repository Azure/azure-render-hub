// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebApp.Code.Validators
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ConfirmDeletionAttribute : ValidationAttribute, IClientModelValidator
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // NOTE: Configuring this for client-side validation only at the moment;
            return ValidationResult.Success;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            var error = FormatErrorMessage(context.ModelMetadata.GetDisplayName());
            context.Attributes.TryAdd("data-val", "true");
            context.Attributes.Add("data-val-confirm-delete", error);
        }

        public override object TypeId { get; } = new object();
    }
}
