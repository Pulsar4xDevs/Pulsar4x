using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;

namespace Pulsar4X.Tests;

/// <summary>
/// Provides methods to check the "equivalence" of data blobs.
/// Note: This is not a true equality checker, but a utility to check
/// if two data blobs are effectively equivalent for testing purposes.
/// </summary>
public static class DataBlobEqualityChecker
{
    /// <summary>
    /// Determines if two data blobs are effectively equivalent.
    /// Uses reflection and recursion to do as deep of a check as possible.
    /// </summary>
    /// <typeparam name="T">The type of the data blobs.</typeparam>
    /// <param name="first">The first data blob.</param>
    /// <param name="second">The second data blob.</param>
    /// <returns>true if the two data blobs are effectively equivalent; otherwise, false.</returns>
    public static bool AreEqual<T>(T first, T second)
        where T : BaseDataBlob
    {
        // Use a HashSet to track compared dataBlobs.
        var visitedPairs = new HashSet<(object?, object?)>();
        return AreEqualInternal(first, second, visitedPairs);
    }

    /// <summary>
    /// Recursive method that assists in determining if two data blobs are equivalent.
    /// </summary>
    /// <typeparam name="T">The type of the data blobs.</typeparam>
    /// <param name="first">The first data blob.</param>
    /// <param name="second">The second data blob.</param>
    /// <param name="visitedPairs">A collection used to track already compared pairs of objects, preventing infinite recursion when circular references are present.</param>
    /// <returns>true if the two data blobs are effectively equivalent; otherwise, false.</returns>
    private static bool AreEqualInternal<T>(T first, T second, HashSet<(object?, object?)> visitedPairs)
        where T : BaseDataBlob
    {
        // Avoid infinite recursion
        if (visitedPairs.Contains((first, second)) || visitedPairs.Contains((second, first)))
            return true;
        visitedPairs.Add((first, second));

        // Get the actual implementation type, not a base type.
        Type firstType = first.GetType();
        Type secondType = second.GetType();

        if (firstType != secondType) return false;

        // Reflect equality checks across the properties.
        foreach (PropertyInfo property in firstType.GetProperties())
        {
            object? firstValue = property.GetValue(first);
            object? secondValue = property.GetValue(second);
            if (!CompareProperty(visitedPairs, firstValue, secondValue))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Compares properties of two objects to determine if they are equivalent.
    /// </summary>
    /// <param name="visitedPairs">A collection used to track already compared pairs of objects, preventing infinite recursion when circular references are present.</param>
    /// <param name="firstValue">The first object to compare.</param>
    /// <param name="secondValue">The second object to compare.</param>
    /// <returns>true if the objects are effectively equivalent; otherwise, false.</returns>
    private static bool CompareProperty(HashSet<(object?, object?)> visitedPairs, object? firstValue, object? secondValue)
    {
        if (visitedPairs.Contains((firstValue, secondValue)) || visitedPairs.Contains((secondValue, firstValue)))
            return true;

        visitedPairs.Add((firstValue, secondValue));

        if (firstValue is null && secondValue is null)
            return true;

        if (firstValue is null || secondValue is null)
            return false;

        Type propertyType = firstValue.GetType();
        if (propertyType != secondValue.GetType())
            return false;

        // Special handling for Entities so we don't check references
        if (propertyType == typeof(Entity))
        {
            if (!EntityPropertiesMatch((Entity)firstValue, (Entity)secondValue, visitedPairs))
                return false;
        }
        // Direct compare value types and strings.
        else if (firstValue.GetType().IsValueType || firstValue is string)
        {
            if (!firstValue.Equals(secondValue))
                return false;
        }
        // Special handling for DataBlobs so we don't check references
        else if (propertyType.IsSubclassOf(typeof(BaseDataBlob)))
        {
            if (!AreEqualInternal((BaseDataBlob)firstValue, (BaseDataBlob)secondValue, visitedPairs))
                return false;
        }
        // Special handling for Lists so we don't check references
        else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
        {
            if (!CompareLists(visitedPairs, firstValue, secondValue))
                return false;
        }
        // Special handling for Dictionaries so we don't check references
        else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            if (!CompareDictionaries(visitedPairs, firstValue, secondValue))
                return false;
        }
        else
        {
            // Last Ditch Effort: Reflect the raw class and try to recurse in these
            // functions until we hit value types.
            if (propertyType.IsClass)
                return CompareClassObjects(visitedPairs, firstValue, secondValue);
            // Last last ditch effort, compare references (default equality operator).
            if (!firstValue.Equals(secondValue))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Compares dictionaries to determine if they are equivalent.
    /// </summary>
    /// <param name="visitedPairs">A collection used to track already compared pairs of objects, preventing infinite recursion when circular references are present.</param>
    /// <param name="firstValue">The first dictionary to compare.</param>
    /// <param name="secondValue">The second dictionary to compare.</param>
    /// <returns>true if the dictionaries are effectively equivalent; otherwise, false.</returns>
    private static bool CompareDictionaries(HashSet<(object?, object?)> visitedPairs, object firstValue, object secondValue)
    {
        var firstDict = (IDictionary)firstValue;
        var secondDict = (IDictionary)secondValue;

        // If the dictionaries have different counts, they're not equivalent.
        if (firstDict.Count != secondDict.Count) return false;

        // For each key in the first dictionary...
        foreach (object? firstKey in firstDict.Keys)
        {
            bool keyMatchFound = false;

            // ...we loop through all keys in the second dictionary to find an equivalent.
            foreach (object? secondKey in secondDict.Keys)
            {
                // Use CompareProperty to check if keys from both dictionaries are effectively equivalent.
                if (!CompareProperty(visitedPairs, firstKey, secondKey))
                    continue;

                keyMatchFound = true;

                // If keys are equivalent, retrieve the values for both keys.
                object? firstDictValue = firstDict[firstKey];
                object? secondDictValue = secondDict[secondKey];

                // Now compare the values for the matched keys. If values aren't equivalent, entire dictionaries aren't equivalent.
                if (!CompareProperty(visitedPairs, firstDictValue, secondDictValue))
                    return false;

                break; // Break out of inner loop as a matching key was found.
            }

            // If no equivalent key was found in the second dictionary for a key in the first dictionary, they're not equivalent.
            if (!keyMatchFound) return false;
        }

        return true;
    }

    /// <summary>
    /// Compares lists to determine if they are equivalent.
    /// </summary>
    /// <param name="visitedPairs">A collection used to track already compared pairs of data blobs, preventing infinite recursion when circular references are present.</param>
    /// <param name="firstValue">The first list to compare.</param>
    /// <param name="secondValue">The second list to compare.</param>
    /// <returns>true if the lists are effectively equivalent; otherwise, false.</returns>
    private static bool CompareLists(HashSet<(object?, object?)> visitedPairs, object firstValue, object secondValue)
    {
        var firstList = (IList)firstValue;
        var secondList = (IList)secondValue;

        if (firstList.Count != secondList.Count) return false;

        for (int i = 0; i < firstList.Count; i++)
        {
            object? firstListItem = firstList[i];
            object? secondListItem = secondList[i];
            if (!CompareProperty(visitedPairs, firstListItem, secondListItem))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Compares and reflects through the fields and properties of two class objects to determine if they are equivalent.
    /// </summary>
    /// <param name="visitedPairs">A collection used to track already compared pairs of objects, preventing infinite recursion when circular references are present.</param>
    /// <param name="firstValue">The first class object to compare.</param>
    /// <param name="secondValue">The second class object to compare.</param>
    /// <returns>true if the class objects are effectively equivalent; otherwise, false.</returns>
    private static bool CompareClassObjects(HashSet<(object?, object?)> visitedPairs, object firstObject, object secondObject)
    {
        Type objectType = firstObject.GetType();

        // Compare fields.
        foreach (FieldInfo field in objectType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            object? firstFieldValue = field.GetValue(firstObject);
            object? secondFieldValue = field.GetValue(secondObject);
            if (!CompareProperty(visitedPairs, firstFieldValue, secondFieldValue))
                return false;
        }

        // Compare properties.
        foreach (PropertyInfo property in objectType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            object? firstPropertyValue = property.GetValue(firstObject);
            object? secondPropertyValue = property.GetValue(secondObject);
            if (!CompareProperty(visitedPairs, firstPropertyValue, secondPropertyValue))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the properties of two entities make them effectively equivalent.
    /// </summary>
    /// <param name="entityA">The first entity.</param>
    /// <param name="entityB">The second entity.</param>
    /// <returns>true if the two entities are effectively equivalent; otherwise, false.</returns>
    public static bool AreEqual(Entity entityA, Entity entityB)
    {
        HashSet<(object?, object?)> visitedPairs = new();
        return EntityPropertiesMatch(entityA, entityB, visitedPairs);
    }

    /// <summary>
    /// Checks if the properties of two entities make them effectively equivalent.
    /// </summary>
    /// <param name="entityA">The first entity.</param>
    /// <param name="entityB">The second entity.</param>
    /// ///
    /// <param name="visitedPairs">A collection used to track already compared pairs of data blobs, preventing infinite recursion when circular references are present.</param>
    /// <returns>true if the two entities are effectively equivalent; otherwise, false.</returns>
    private static bool EntityPropertiesMatch(Entity entityA, Entity entityB, HashSet<(object?, object?)> visitedPairs)
    {
        // Check the entity's DataBlobs for exact information.
        List<BaseDataBlob> entityADataBlobs = entityA.GetAllDataBlobs();
        List<BaseDataBlob> entityBDataBlobs = entityB.GetAllDataBlobs();

        if (entityADataBlobs.Count != entityBDataBlobs.Count) return false;

        for (int index = 0; index < entityADataBlobs.Count; index++)
        {
            // Compare each DataBlob to ensure is has exact data.
            BaseDataBlob entityADataBlob = entityADataBlobs[index];
            BaseDataBlob entityBDataBlob = entityBDataBlobs[index];
            if (AreEqualInternal(entityADataBlob, entityBDataBlob, visitedPairs))
                continue;
            return false;
        }

        return true;
    }
}