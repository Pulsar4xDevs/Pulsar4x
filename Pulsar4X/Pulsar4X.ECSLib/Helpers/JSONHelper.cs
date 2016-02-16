using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// JsonConverter for dictionaries. Allows serialization/deserialization of dictionaries as class members with custom class keys.
    /// Replaces JDictionary
    /// </summary>
    public class DictionaryConverter : JsonConverter
    {
        /// <summary>
        /// Writes the dictionary to the JsonWriter. Writes it in a custom format that's compact, and typeSafe for custom classes.
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject(); // Start of the Dictionary
            writer.WritePropertyName("Entries"); // First Property "Entries"
            writer.WriteStartArray(); // Beginning of "List" array
            foreach (DictionaryEntry entry in (IDictionary)value)
            {
                // Serialize each entry as an object in the "Entries" array
                serializer.Serialize(writer, entry);
            }
            writer.WriteEndArray(); // End of the "List" array
            writer.WriteEndObject(); // End of the Dictionary
        }

        /// <summary>
        /// Reads the dictionary from the JsonReader. Reads the custom format in a way that is typeSafe for custom classes.
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var dictionary = (IDictionary)Activator.CreateInstance(objectType);
            var keyType = objectType.GenericTypeArguments[0];
            var valueType = objectType.GenericTypeArguments[1];

            reader.Read(); // PropertyName "Entries"
            reader.Read(); // StartArray
            reader.Read(); // EndArray OR StartObject
            while (reader.TokenType != JsonToken.EndArray)
            {
                reader.Read(); // PropertyName "Key"
                reader.Read(); // Actual Key value
                var key = serializer.Deserialize(reader, keyType);
                reader.Read(); // PropertyName "Value"
                reader.Read(); // Actual Value value
                var value = serializer.Deserialize(reader, valueType);
                dictionary.Add(key, value);
                reader.Read(); // End Object
                reader.Read(); // EndArray OR StartObject OR EndObject (Dictionary)
            }
            reader.Read(); // Set the reader to the next object

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

    public static class ListExtension
    {
        [PublicAPI]
        public static List<Entity> GetEntititiesWithDataBlob<TDataBlob>(this List<Entity> list) where TDataBlob : BaseDataBlob
        {
            var retVal = new List<Entity>();
            foreach (Entity entity in list)
            {
                if (entity.HasDataBlob<TDataBlob>())
                {
                    retVal.Add(entity);
                }
            }

            return retVal;
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

            // Custom serialization of dictionaries without subclassing.
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
