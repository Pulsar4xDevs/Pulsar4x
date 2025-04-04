using System;

namespace Pulsar4X.Engine;

public class Modifier<T> : IModifier<T>
{
    private readonly Func<T, T, T> _function;
    public string Id { get; }
    public string Name { get; }
    public float Priority { get; }
    public bool IsExpired { get; protected set; }
    public T Before { get; set; }
    public T After { get; set; }
    public T ModifyAmount { get; internal set; }

    public Modifier(string id, string name, T modifier, Func<T, T, T> function, float priority = 1.0f)
    {
        Id = id;
        Name = name;
        ModifyAmount = modifier;
        _function = function;
        Priority = priority;
        IsExpired = false;
    }

    public T Apply(T baseValue, T currentValue)
    {
        return _function(currentValue, ModifyAmount);
    }

    // Method to mark the modifier as expired
    public void Expire()
    {
        IsExpired = true;
    }
}