// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebApp.Code.Validators
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ContainsAsciiOnlyAttribute : ValidationAttribute, IClientModelValidator
    {
        private readonly Regex _regex = new Regex(Validation.RegularExpressions.AsciiOnly);
 
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var currentValue = value as string;
            return !_regex.IsMatch(currentValue ?? "")
                ? new ValidationResult(ErrorMessage)
                : ValidationResult.Success;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            var error = FormatErrorMessage(context.ModelMetadata.GetDisplayName());
            context.Attributes.TryAdd("data-val", "true");
            context.Attributes.Add("data-val-ascii-only", error);
        }

        public override object TypeId { get; } = new object();
    }
}
