using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Pulsar4X.Engine;

namespace Pulsar4X.Tests;

public class SerializationEquivalenceTester
{
    public class ComparisonResult
    {
        public bool AreEqual { get; set; }
        public List<string> Differences { get; set; } = new List<string>();
        public string SerializedJson { get; set; } = "";
    }

    public static ComparisonResult TestSerializationEquivalence<T>(T originalObject)
    {
        var result = new ComparisonResult();

        try
        {
            var customSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                Error = (sender, args) =>
                {
                    result.Differences.Add($"Serialization Error: {args.ErrorContext.Error.Message} at {args.ErrorContext.Path}");
                    args.ErrorContext.Handled = true;
                },
                Formatting = Formatting.Indented,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ContractResolver = new NonPublicResolver()
            };

            // Serialize with detailed error information
            try
            {
                result.SerializedJson = JsonConvert.SerializeObject(originalObject, customSettings);
            }
            catch (Exception ex)
            {
                throw new Exception($"Serialization failed: {ex.Message}\nObject type: {typeof(T).FullName}", ex);
            }

            // Deserialize with detailed error information
            T? deserializedObject;
            try
            {
                deserializedObject = JsonConvert.DeserializeObject<T>(result.SerializedJson, customSettings);
            }
            catch (Exception ex)
            {
                throw new Exception($"Deserialization failed: {ex.Message}\nJSON: {result.SerializedJson}", ex);
            }

            // Compare the objects
            result.AreEqual = CompareObjects(originalObject, deserializedObject, "", result.Differences);

        }
        catch(Exception ex)
        {
            result.AreEqual = false;
            result.Differences.Add($"Serialization/Deserializtion failed: {ex.Message}");
        }

        return result;
    }

    private static bool CompareObjects(object? original, object? deserialized, string path, List<string> differences)
    {
        if(original == null && deserialized == null)
            return true;

        if(original == null || deserialized == null)
        {
            differences.Add($"{path}: One object is null while the other is not");
            return false;
        }

        Type type = original.GetType();

        // Handle primitive types and strings
        if(type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
        {
            if(!object.Equals(original, deserialized))
            {
                differences.Add($"{path}: Values differ - Original: {original}, Deserialized: {deserialized}");
                return false;
            }
            return true;
        }

        // Handle DateTime
        if(type == typeof(DateTime))
        {
            var originalDate = (DateTime)original;
            var deserializedDate = (DateTime)deserialized;

            if(originalDate != deserializedDate)
            {
                differences.Add($"{path}: DateTime values differ - Original: {originalDate}, Deserialized: {deserializedDate}");
                return false;
            }
            return true;
        }

        // Handle IEnumerable (except strings)
        if(typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            return CompareEnumerables((IEnumerable)original, (IEnumerable)deserialized, path, differences);
        }

        // Handle complex objects

        // Get all the properties with a [JsonProperty] attribute or public access
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Where(p => p.GetCustomAttribute<JsonPropertyAttribute>() != null || p.GetGetMethod()?.IsPublic == true);

        foreach(var prop in properties)
        {
            string propertyPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
            var originalValue = prop.GetValue(original);
            var deserializedValue = prop.GetValue(deserialized);

            if(!CompareObjects(originalValue, deserializedValue, propertyPath, differences))
                return false;
        }

        // Handle fields with [JsonProperty] attribute or are public
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(f => f.GetCustomAttribute<JsonPropertyAttribute>() != null || f.IsPublic);

        foreach(var field in fields)
        {
            string fieldPath = string.IsNullOrEmpty(path) ? field.Name : $"{path}.{field.Name}";
            var originalValue = field.GetValue(original);
            var deserializedValue = field.GetValue(deserialized);

            if(!CompareObjects(originalValue, deserializedValue, fieldPath, differences))
                return false;
        }

        return true;
    }

    private static bool CompareEnumerables(IEnumerable original, IEnumerable deserialized, string path, List<string> differences)
    {
        var originalList = original.Cast<object>().ToList();
        var deserializedList = deserialized.Cast<object>().ToList();

        if(originalList.Count != deserializedList.Count)
        {
            differences.Add($"{path}: Collections have different lengths - Original: {originalList.Count}, Deserialized: {deserializedList.Count}");
            return false;
        }

        for(int i = 0; i < originalList.Count; i++)
        {
            if(!CompareObjects(originalList[i], deserializedList[i], $"{path}[{i}]", differences))
                return false;
        }

        return true;
    }
}