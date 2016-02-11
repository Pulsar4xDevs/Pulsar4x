using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Pulsar4X.ECSLib
{
    public class DictionaryConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("List");
            writer.WriteStartArray();
            foreach (DictionaryEntry entry in (IDictionary)value)
            {
                serializer.Serialize(writer, entry);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var dictionary = (IDictionary)Activator.CreateInstance(objectType);
            var keyType = objectType.GenericTypeArguments[0];
            var valueType = objectType.GenericTypeArguments[1];

            reader.Read(); // PropertyName "List"
            reader.Read(); // StartArray
            reader.Read(); // EndArray OR StartObject
            while (reader.TokenType != JsonToken.EndArray)
            {
                reader.Read(); // PropertyName "Key"
                reader.Read(); // Actual Key value
                var Key = serializer.Deserialize(reader, keyType);
                reader.Read(); // PropertyName "Value"
                reader.Read(); // Actual Value value
                var Value = serializer.Deserialize(reader, valueType);
                dictionary.Add(Key, Value);
                reader.Read(); // End Object
                reader.Read(); // EndArray OR StartObject
            }
            reader.Read(); // Next

            return dictionary;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.GetInterfaces().Any(i => i == typeof(IDictionary) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)));
        }
    } 

    public static class DictionaryExtension
    {
        [PublicAPI]
        public static void SafeValueAdd<TKey>(this Dictionary<TKey, int> jDict, TKey key, int toAdd)
        {
            if(!jDict.ContainsKey(key))
                jDict.Add(key, toAdd);
            else
            {
                jDict[key] += toAdd;
            }
        }

        [PublicAPI]
        public static void SafeValueAdd<TKey>(this Dictionary<TKey, float> jDict, TKey key, float toAdd)
        {
            if (!jDict.ContainsKey(key))
                jDict.Add(key, toAdd);
            else
            {
                jDict[key] += toAdd;
            }
        }

        [PublicAPI]
        public static void SafeValueAdd<TKey>(this Dictionary<TKey, double> jDict, TKey key, double toAdd)
        {
            if (!jDict.ContainsKey(key))
                jDict.Add(key, toAdd);
            else
            {
                jDict[key] += toAdd;
            }
        }
    }

    /// <summary>
    /// This class can be used by a Json.net serializer to get around the problem of it ignoreing ISerializable in favor of IEnumarble.
    /// </summary>
    public class ForceUseISerializable : DefaultContractResolver
    {
        protected override JsonContract CreateContract(Type objectType)
        {
            // make sure we use default Json datTime converter
            // otherwise import will fail.
            if (typeof(DateTime).IsAssignableFrom(objectType))  
            {
                return base.CreateContract(objectType);
            }

            // Serialize dictionaries as arrays without subclassing.
            if (objectType.GetInterfaces().Any(i => i == typeof(IDictionary) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))))
            {
                var contract = CreateArrayContract(objectType);
                contract.Converter = new DictionaryConverter();
                return contract;
            }

            // now we can force iserialible to be used.
            if (typeof(ISerializable).IsAssignableFrom(objectType))
            {
                return CreateISerializableContract(objectType);
            }

            // if we don't match any of the above just use the default.
            return base.CreateContract(objectType);
        }
    }
}
