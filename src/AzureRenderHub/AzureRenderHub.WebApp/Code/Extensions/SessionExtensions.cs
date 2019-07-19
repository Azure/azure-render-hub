// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using WebApp.Code.JsonConverters;

namespace WebApp.Code.Extensions
{
    public static class SessionExtensions
    {
        private static readonly JsonSerializerSettings _jsonSerializerSettings;

        static SessionExtensions()
        {
            _jsonSerializerSettings = new JsonSerializerSettings();
            _jsonSerializerSettings.ContractResolver = new JsonIgnoreAttributeIgnorerContractResolver();
            _jsonSerializerSettings.Converters.Add(new StringEnumConverter());
            _jsonSerializerSettings.Converters.Add(new AssetRepositoryConverter());
        }

        public static T Set<T>(this ISession session, string key, T value)
        {
            // TODO: probably need some error handling in here
            session.SetString(key, JsonConvert.SerializeObject(value, _jsonSerializerSettings));
            return value;
        }

        public static T Get<T>(this ISession session, string key)
        {
            // TODO: probably need some error handling in here
            var value = session.GetString(key);
            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value, _jsonSerializerSettings);
        }
    }

    // Our environment models use the JsonIgnore attribute to prevent serializing credentials, however
    // in the session we want them serialized so below 'ignores the ignore'.
    public class JsonIgnoreAttributeIgnorerContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            property.Ignored = false;
            return property;
        }
    }
}
