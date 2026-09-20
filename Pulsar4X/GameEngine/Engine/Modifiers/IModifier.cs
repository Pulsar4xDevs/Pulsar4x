namespace Pulsar4X.Engine;

public interface IModifier<T>
{
    string Id { get; }
    string Name { get; }
    float Priority { get; }
    T Before { get; set; }
    T After { get; set; }
    T Apply(T baseValue, T currentValue);
    bool IsExpired { get; }
}