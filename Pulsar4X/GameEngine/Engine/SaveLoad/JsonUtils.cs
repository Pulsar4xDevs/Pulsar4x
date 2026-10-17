using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using GameEngine.Engine.Orders;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Pulsar4X.Engine;

public class NonPublicResolver : DefaultContractResolver {
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var prop = base.CreateProperty(member, memberSerialization);
        if (!prop.Writable) {
            var property = member as PropertyInfo;
            var hasNonPublicSetter = property?.GetSetMethod(true) != null;
            prop.Writable = hasNonPublicSetter;
        }
        return prop;
    }

    protected override JsonObjectContract CreateObjectContract(Type objectType)
    {
        var contract = base.CreateObjectContract(objectType);
        // Orders saved before they grew a parameterless constructor still have to come back.
        // Populate the serialized fields afterwards; do not run the start-path constructor.
        if (contract.DefaultCreator == null
            && contract.OverrideCreator == null
            && !objectType.IsAbstract
            && typeof(EntityAction).IsAssignableFrom(objectType))
        {
            contract.DefaultCreator = () => RuntimeHelpers.GetUninitializedObject(objectType);
        }
        return contract;
    }
}