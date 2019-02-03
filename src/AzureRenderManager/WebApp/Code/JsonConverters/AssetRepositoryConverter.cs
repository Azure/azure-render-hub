// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.ComponentModel;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebApp.Config.Storage;

namespace WebApp.Code.JsonConverters
{
    /// <summary>
    /// Custom converter to convert serialized abstract AssetRepository entities
    /// into the correct implementation instances.
    /// </summary>
    public class AssetRepositoryConverter : JsonConverter
    {
        /**
         * NOTE: need to use this at the moment
         * If you use the passed in JsonSerializer you get a stack overflow exception as the
         * JsonConverter repeatedly calls itself
         *
         * TODO: Figure out how to get around this.
         */
        private static readonly JsonSerializer StaticSerializer = new JsonSerializer();

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Unused as CanWrite returns false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);

            // RepositoryType is a known property in the AssetRepository json
            var typeString = jObject.Value<string>("RepositoryType");
            if (!Enum.TryParse(typeString, true, out AssetRepositoryType repoType))
            {
                throw new InvalidEnumArgumentException($"RepositoryType value '{typeString}' cannot be converted into a AssetRepositoryType");
            }

            var requiredType = GetType(repoType);
            return StaticSerializer.Deserialize(jObject.CreateReader(), requiredType);
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(AssetRepository).IsAssignableFrom(objectType) || typeof(AssetRepository) == objectType;
        }

        private Type GetType(AssetRepositoryType repoType)
        {
            switch (repoType)
            {
                case AssetRepositoryType.AvereCluster:
                    return typeof(AvereCluster);
//                case AssetRepositoryType.AzureFilesShare:
//                    return typeof(AzureFilesShare);
                case AssetRepositoryType.NfsFileServer:
                    return typeof(NfsFileServer);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
