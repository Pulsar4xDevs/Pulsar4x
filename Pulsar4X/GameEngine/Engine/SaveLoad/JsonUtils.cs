using System.Reflection;
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
}