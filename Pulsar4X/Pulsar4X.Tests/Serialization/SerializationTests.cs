using NUnit.Framework;
using Pulsar4X.Colonies;
using Pulsar4X.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.Tests;

[TestFixture]
public class SerializationTests
{
    private void AssertSerialization<T>(T value, string typeName)
    {
        var result = SerializationEquivalenceTester.TestSerializationEquivalence(value);

        if (result.Differences.Any())
        {
            Console.WriteLine($"Differences found for {typeName}:");
            foreach (var difference in result.Differences)
            {
                Console.WriteLine(difference);
            }
            Console.WriteLine("\nSerialized JSON:");
            Console.WriteLine(result.SerializedJson);
        }

        Assert.That(result.AreEqual, Is.True, $"Serialization failed for {typeName}");
    }

    [Test]
    public void VerifyPrimitiveTypeSerialization()
    {
        // Test each primitive type with a non-default value
        AssertSerialization(42, "int");
        AssertSerialization(42L, "long");
        AssertSerialization(42.42f, "float");
        AssertSerialization(42.42d, "double");
        AssertSerialization(42.42m, "decimal");
        AssertSerialization((short)42, "short");
        AssertSerialization((byte)42, "byte");
        AssertSerialization(true, "bool");
        AssertSerialization('X', "char");
        AssertSerialization((sbyte)42, "sbyte");
        AssertSerialization((ushort)42, "ushort");
        AssertSerialization(42u, "uint");
        AssertSerialization(42ul, "ulong");
    }

    [Test]
    public void VerifyCommonTypesSerialization()
    {
        // Test common reference and value types
        AssertSerialization("Test String", "string");
        AssertSerialization(DateTime.Parse("2024-03-19T15:30:00"), "DateTime");
        AssertSerialization(TimeSpan.FromHours(1.5), "TimeSpan");
        AssertSerialization(Guid.Parse("12345678-1234-1234-1234-123456789012"), "Guid");
        AssertSerialization(DateOnly.Parse("2024-03-19"), "DateOnly");
        AssertSerialization(TimeOnly.Parse("15:30:00"), "TimeOnly");
    }

    [Test]
    public void VerifyNullableTypeSerialization()
    {
        // Test nullable primitive types with values
        AssertSerialization<int?>(42, "nullable int");
        AssertSerialization<double?>(42.42, "nullable double");
        AssertSerialization<bool?>(true, "nullable bool");
        AssertSerialization<DateTime?>(DateTime.Parse("2024-03-19T15:30:00"), "nullable DateTime");

        // Test nullable types with null
        AssertSerialization<int?>(null, "null nullable int");
        AssertSerialization<double?>(null, "null nullable double");
    }

    [Test]
    public void VerifyArraySerialization()
    {
        // Test arrays of primitive types
        AssertSerialization(new int[] { 1, 2, 3 }, "int array");
        AssertSerialization(new string[] { "one", "two", "three" }, "string array");
        AssertSerialization(new double[] { 1.1, 2.2, 3.3 }, "double array");
    }

    [Test]
    public void VerifyCollectionSerialization()
    {
        // Test various collection types
        AssertSerialization(new List<int> { 1, 2, 3 }, "List<int>");
        AssertSerialization(new Dictionary<string, int> { { "one", 1 }, { "two", 2 } }, "Dictionary");
        AssertSerialization(new HashSet<int> { 1, 2, 3 }, "HashSet");
    }

    [Test]
    public void VerifyEdgeCaseValues()
    {
        // Test edge case values
        AssertSerialization(int.MaxValue, "int.MaxValue");
        AssertSerialization(int.MinValue, "int.MinValue");
        AssertSerialization(double.MaxValue, "double.MaxValue");
        AssertSerialization(double.MinValue, "double.MinValue");
        AssertSerialization(double.PositiveInfinity, "double.PositiveInfinity");
        AssertSerialization(double.NegativeInfinity, "double.NegativeInfinity");
        AssertSerialization(double.NaN, "double.NaN");
        AssertSerialization(decimal.MaxValue, "decimal.MaxValue");
        AssertSerialization(decimal.MinValue, "decimal.MinValue");
    }

    [Test]
    public void VerifySpecialCharacterStrings()
    {
        // Test strings with special characters
        AssertSerialization("Special \"quotes\" and \\ backslashes", "string with quotes");
        AssertSerialization("Unicode: 你好世界", "string with Unicode");
        AssertSerialization("Multi\nLine\r\nString", "multiline string");
        AssertSerialization("Tab\tand\tspaces", "string with tabs");
        AssertSerialization("🎉 Emoji test 🚀", "string with emoji");
    }

    [Test]
    public void VerifyEngineDataStructures()
    {
        // Test engine data structures
        AssertSerialization(new SafeList<int>() { 1, 2, 3, 4, 5 }, "SafeList<int>");
        AssertSerialization(new SafeDictionary<int, string>() { {1, "one"}, {2, "two"}, {3, "three"} }, "SafeDictionary<int, string>");
        AssertSerialization(new PercentValue(0.42f), "PercentValue");
        AssertSerialization(new WeightedValue<string>() { Value = "one", Weight = 0.1 }, "WeightedValue<string>");
        AssertSerialization(new WeightedList<string>() { { 0.1, "one"}, {0.2, "two"}, {0.3, "three"}, {0.4, "four"} }, "WeightedList<string>");
        AssertSerialization(new ManuverState() { At = DateTime.Now, Mass = 12345.67, Position = new Orbital.Vector3(1.1, 2.2, 3.3), Velocity = new Orbital.Vector3(4.4, 5.5, 6.6) }, "ManuverState");
        AssertSerialization(new ValueTypeStruct() { ValueSize = ValueTypeStruct.ValueSizes.Centi, ValueType = ValueTypeStruct.ValueTypes.Volume }, "ValueTypeStruct");
    }
}