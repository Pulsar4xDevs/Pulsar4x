using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.Engine;

public class ModifiableValue<T>
{
    private readonly List<IModifier<T>> _modifiers = new ();

    public T BaseValue { get; private set; }

    public ModifiableValue(T baseValue)
    {
        BaseValue = baseValue;
    }

    public void SetBaseValue(T newBaseValue)
    {
        BaseValue = newBaseValue;
    }

    public T GetValue()
    {
        _modifiers.RemoveAll(m => m.IsExpired);

        return Calculate();
    }

    public void AddModifier(IModifier<T> modifier)
    {
        _modifiers.Add(modifier);
        _modifiers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    public void RemoveModifier(string id)
    {
        var modifier = _modifiers.FirstOrDefault(m => m.Id.Equals(id));
        if(modifier != null)
        {
            _modifiers.Remove(modifier);
        }
    }

    public void ClearAllModifiers()
    {
        _modifiers.Clear();
    }

    public IReadOnlyList<IModifier<T>> GetModifiers()
    {
        return _modifiers.AsReadOnly();
    }

    public T Calculate()
    {
        T result = BaseValue;

        foreach(var modifier in _modifiers)
        {
            modifier.Before = result;
            result = modifier.Apply(BaseValue, result);
            modifier.After = result;
        }

        return result;
    }
}