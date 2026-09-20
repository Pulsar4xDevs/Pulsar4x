using System;
using Newtonsoft.Json;

namespace Pulsar4X.DataStructures;

/// <summary>
/// Represents the level of access a faction has to masked data.
/// </summary>
public enum AccessLevel : byte
{
    /// <summary>Data is completely hidden.</summary>
    None,
    /// <summary>Data is obfuscated/approximate.</summary>
    Partial,
    /// <summary>Data is fully visible with exact values.</summary>
    Full
}

/// <summary>
/// A generic wrapper that controls access to data based on faction bit masks.
/// Use this to hide or partially reveal game data to different factions.
/// </summary>
/// <typeparam name="T">The value type to wrap.</typeparam>
[JsonConverter(typeof(MaskedConverter))]
public struct Masked<T> where T : struct
{
    private T _value;
    private T _obscured;
    private int _fullMask;
    private int _partialMask;
    private AccessLevel _defaultAccess;

    /// <summary>
    /// Creates a new Masked value with full and partial access masks.
    /// </summary>
    /// <param name="value">The actual value.</param>
    /// <param name="obscured">The obfuscated value shown to factions with partial access.</param>
    /// <param name="fullMask">Bit mask of factions with full access.</param>
    /// <param name="partialMask">Bit mask of factions with partial access.</param>
    /// <param name="defaultAccess">Access level for factions not in any mask.</param>
    public Masked(T value, T obscured, int fullMask, int partialMask = 0,
        AccessLevel defaultAccess = AccessLevel.None)
    {
        _value = value;
        _obscured = obscured;
        _fullMask = fullMask;
        _partialMask = partialMask;
        _defaultAccess = defaultAccess;
    }

    /// <summary>
    /// Creates a new Masked value where only the owner has full access.
    /// </summary>
    /// <param name="value">The actual value.</param>
    /// <param name="ownerMask">Bit mask of the owning faction.</param>
    public Masked(T value, int ownerMask) : this(value, default, ownerMask, 0) { }

    /// <summary>
    /// Creates a new Masked value with a default access level for all factions.
    /// </summary>
    /// <param name="value">The actual value.</param>
    /// <param name="obscured">The obfuscated value shown to factions with partial access.</param>
    /// <param name="defaultAccess">Default access level for all factions.</param>
    public Masked(T value, T obscured, AccessLevel defaultAccess)
        : this(value, obscured, 0, 0, defaultAccess) { }

    /// <summary>
    /// Creates a new Masked value visible to all factions at the specified level.
    /// </summary>
    /// <param name="value">The actual value.</param>
    /// <param name="defaultAccess">Access level for all factions.</param>
    public Masked(T value, AccessLevel defaultAccess)
        : this(value, default, 0, 0, defaultAccess) { }

    #region Access Control

    /// <summary>
    /// Gets the access level for the specified faction mask.
    /// </summary>
    /// <param name="factionMask">The faction's bit mask.</param>
    /// <returns>The access level.</returns>
    public AccessLevel GetAccess(int factionMask)
    {
        if ((_fullMask & factionMask) != 0) return AccessLevel.Full;
        if ((_partialMask & factionMask) != 0) return AccessLevel.Partial;
        return _defaultAccess;
    }

    /// <summary>
    /// Grants full access to the specified factions.
    /// If they had partial access, they are upgraded to full.
    /// </summary>
    /// <param name="factionMask">The faction bit mask to grant access to.</param>
    public void GrantFull(int factionMask)
    {
        _partialMask &= ~factionMask;
        _fullMask |= factionMask;
    }

    /// <summary>
    /// Grants partial access to the specified factions.
    /// Does not affect factions that already have full access.
    /// </summary>
    /// <param name="factionMask">The faction bit mask to grant access to.</param>
    public void GrantPartial(int factionMask)
    {
        int notFullAccess = ~_fullMask & factionMask;
        _partialMask |= notFullAccess;
    }

    /// <summary>
    /// Revokes all access from the specified factions.
    /// </summary>
    /// <param name="factionMask">The faction bit mask to revoke access from.</param>
    public void Revoke(int factionMask)
    {
        _fullMask &= ~factionMask;
        _partialMask &= ~factionMask;
    }

    /// <summary>
    /// Downgrades full access to partial access for the specified factions.
    /// Factions that only had partial access are unaffected.
    /// </summary>
    /// <param name="factionMask">The faction bit mask to downgrade.</param>
    public void Downgrade(int factionMask)
    {
        int hadFull = _fullMask & factionMask;
        _fullMask &= ~factionMask;
        _partialMask |= hadFull;
    }

    /// <summary>
    /// Explicitly sets the access level for the specified factions.
    /// </summary>
    /// <param name="factionMask">The faction bit mask.</param>
    /// <param name="level">The access level to set.</param>
    public void SetAccess(int factionMask, AccessLevel level)
    {
        Revoke(factionMask);
        switch (level)
        {
            case AccessLevel.Full:
                _fullMask |= factionMask;
                break;
            case AccessLevel.Partial:
                _partialMask |= factionMask;
                break;
        }
    }

    /// <summary>
    /// Gets the raw full access bit mask.
    /// </summary>
    public int FullMask => _fullMask;

    /// <summary>
    /// Gets the raw partial access bit mask.
    /// </summary>
    public int PartialMask => _partialMask;

    /// <summary>
    /// Gets or sets the default access level for factions not in any mask.
    /// </summary>
    public AccessLevel DefaultAccess
    {
        get => _defaultAccess;
        set => _defaultAccess = value;
    }

    #endregion

    #region Value Retrieval

    /// <summary>
    /// Gets the value for the specified faction, or null if hidden.
    /// </summary>
    /// <param name="factionMask">The faction's bit mask.</param>
    /// <returns>The value if accessible, null otherwise.</returns>
    public T? For(int factionMask)
    {
        return GetAccess(factionMask) switch
        {
            AccessLevel.Full => _value,
            AccessLevel.Partial => _obscured,
            _ => null
        };
    }

    /// <summary>
    /// Gets the value for the specified faction with dynamic obscuring, or null if hidden.
    /// </summary>
    /// <param name="factionMask">The faction's bit mask.</param>
    /// <param name="obscurer">Function to compute the obscured value from the actual value.
    /// If null, uses the stored obscured value.</param>
    /// <returns>The value if accessible, null otherwise.</returns>
    public T? For(int factionMask, Func<T, T>? obscurer)
    {
        return GetAccess(factionMask) switch
        {
            AccessLevel.Full => _value,
            AccessLevel.Partial => obscurer != null ? obscurer(_value) : _obscured,
            _ => null
        };
    }

    /// <summary>
    /// Gets the value with access level information.
    /// </summary>
    /// <param name="factionMask">The faction's bit mask.</param>
    /// <returns>A result containing the value and access level.</returns>
    public MaskedResult<T> Resolve(int factionMask)
    {
        var level = GetAccess(factionMask);
        var value = level switch
        {
            AccessLevel.Full => _value,
            AccessLevel.Partial => _obscured,
            _ => default
        };
        return new MaskedResult<T>(value, level);
    }

    /// <summary>
    /// Gets the value with access level information and dynamic obscuring.
    /// </summary>
    /// <param name="factionMask">The faction's bit mask.</param>
    /// <param name="obscurer">Function to compute the obscured value from the actual value.
    /// If null, uses the stored obscured value.</param>
    /// <returns>A result containing the value and access level.</returns>
    public MaskedResult<T> Resolve(int factionMask, Func<T, T>? obscurer)
    {
        var level = GetAccess(factionMask);
        var value = level switch
        {
            AccessLevel.Full => _value,
            AccessLevel.Partial => obscurer != null ? obscurer(_value) : _obscured,
            _ => default
        };
        return new MaskedResult<T>(value, level);
    }

    /// <summary>
    /// Checks if the data is visible (not hidden) to the specified faction.
    /// </summary>
    /// <param name="factionMask">The faction's bit mask.</param>
    /// <returns>True if the faction has at least partial access.</returns>
    public bool IsVisibleTo(int factionMask) => GetAccess(factionMask) != AccessLevel.None;

    /// <summary>
    /// Gets the actual value, bypassing all access checks.
    /// Use only for server/internal logic.
    /// </summary>
    internal T Actual { get => _value; set => _value = value; }

    /// <summary>
    /// Gets the obscured value.
    /// </summary>
    internal T Obscured { get => _obscured; set => _obscured = value; }

    #endregion
}

/// <summary>
/// The result of resolving a Masked value, containing both the value and access level.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public readonly struct MaskedResult<T> where T : struct
{
    /// <summary>
    /// The resolved value. Will be default if access is None.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// The access level used to resolve this value.
    /// </summary>
    public AccessLevel Access { get; }

    public MaskedResult(T value, AccessLevel access)
    {
        Value = value;
        Access = access;
    }

    /// <summary>
    /// True if the value is known (Full or Partial access).
    /// </summary>
    public bool IsKnown => Access != AccessLevel.None;

    /// <summary>
    /// True if the value is exact (Full access).
    /// </summary>
    public bool IsExact => Access == AccessLevel.Full;

    /// <summary>
    /// Deconstructs the result for pattern matching.
    /// </summary>
    public void Deconstruct(out T value, out AccessLevel access)
    {
        value = Value;
        access = Access;
    }
}

/// <summary>
/// Factory methods for creating Masked values with common obfuscation patterns.
/// </summary>
public static class Masked
{
    /// <summary>
    /// Creates a Masked double with a random percentage error.
    /// </summary>
    /// <param name="value">The actual value.</param>
    /// <param name="errorPercent">Maximum error as a decimal (e.g., 0.15 for +/-15%).</param>
    /// <param name="fullMask">Factions with full access.</param>
    /// <param name="partialMask">Factions with partial access.</param>
    /// <returns>A new Masked double.</returns>
    public static Masked<double> WithError(double value, double errorPercent,
        int fullMask, int partialMask)
    {
        var error = value * errorPercent * (Random.Shared.NextDouble() * 2 - 1);
        return new Masked<double>(value, value + error, fullMask, partialMask);
    }

    /// <summary>
    /// Creates a Masked int rounded to significant figures.
    /// </summary>
    /// <param name="value">The actual value.</param>
    /// <param name="significantFigures">Number of significant figures to keep.</param>
    /// <param name="fullMask">Factions with full access.</param>
    /// <param name="partialMask">Factions with partial access.</param>
    /// <returns>A new Masked int.</returns>
    public static Masked<int> Rounded(int value, int significantFigures,
        int fullMask, int partialMask)
    {
        if (value == 0) return new Masked<int>(0, 0, fullMask, partialMask);
        var magnitude = (int)Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(value))) - significantFigures + 1);
        var rounded = (value / magnitude) * magnitude;
        return new Masked<int>(value, rounded, fullMask, partialMask);
    }

    /// <summary>
    /// Creates a Masked long rounded to significant figures.
    /// </summary>
    /// <param name="value">The actual value.</param>
    /// <param name="significantFigures">Number of significant figures to keep.</param>
    /// <param name="fullMask">Factions with full access.</param>
    /// <param name="partialMask">Factions with partial access.</param>
    /// <returns>A new Masked long.</returns>
    public static Masked<long> Rounded(long value, int significantFigures,
        int fullMask, int partialMask)
    {
        if (value == 0) return new Masked<long>(0, 0, fullMask, partialMask);
        var magnitude = (long)Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(value))) - significantFigures + 1);
        var rounded = (value / magnitude) * magnitude;
        return new Masked<long>(value, rounded, fullMask, partialMask);
    }
}

/// <summary>
/// JSON converter for Masked&lt;T&gt; values.
/// Serializes all internal state for game saves.
/// </summary>
public class MaskedConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsGenericType &&
               objectType.GetGenericTypeDefinition() == typeof(Masked<>);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var valueType = objectType.GetGenericArguments()[0];
        var dataType = typeof(MaskedData<>).MakeGenericType(valueType);
        var data = serializer.Deserialize(reader, dataType);

        if (data == null)
            return Activator.CreateInstance(objectType);

        // Use reflection to extract values and construct Masked<T>
        var valueField = dataType.GetField("Value");
        var obscuredField = dataType.GetField("Obscured");
        var fullMaskField = dataType.GetField("FullMask");
        var partialMaskField = dataType.GetField("PartialMask");
        var defaultAccessField = dataType.GetField("DefaultAccess");

        var value = valueField!.GetValue(data);
        var obscured = obscuredField!.GetValue(data);
        var fullMask = (int)fullMaskField!.GetValue(data)!;
        var partialMask = (int)partialMaskField!.GetValue(data)!;
        var defaultAccess = (AccessLevel)defaultAccessField!.GetValue(data)!;

        return Activator.CreateInstance(objectType, value, obscured, fullMask, partialMask, defaultAccess);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        var objectType = value.GetType();
        var valueType = objectType.GetGenericArguments()[0];

        // Get values using reflection on the internal properties
        var actualProp = objectType.GetProperty("Actual",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var obscuredProp = objectType.GetProperty("Obscured",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var fullMaskProp = objectType.GetProperty("FullMask");
        var partialMaskProp = objectType.GetProperty("PartialMask");
        var defaultAccessProp = objectType.GetProperty("DefaultAccess");

        var dataType = typeof(MaskedData<>).MakeGenericType(valueType);
        var data = Activator.CreateInstance(dataType);

        dataType.GetField("Value")!.SetValue(data, actualProp!.GetValue(value));
        dataType.GetField("Obscured")!.SetValue(data, obscuredProp!.GetValue(value));
        dataType.GetField("FullMask")!.SetValue(data, fullMaskProp!.GetValue(value));
        dataType.GetField("PartialMask")!.SetValue(data, partialMaskProp!.GetValue(value));
        dataType.GetField("DefaultAccess")!.SetValue(data, defaultAccessProp!.GetValue(value));

        serializer.Serialize(writer, data);
    }
}

/// <summary>
/// Internal data transfer object for JSON serialization of Masked&lt;T&gt;.
/// </summary>
internal class MaskedData<T> where T : struct
{
    public T Value;
    public T Obscured;
    public int FullMask;
    public int PartialMask;
    public AccessLevel DefaultAccess;
}
