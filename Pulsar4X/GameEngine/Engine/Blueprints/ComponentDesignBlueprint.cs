using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.Blueprints;

public class ComponentDesignBlueprint : Blueprint
{
    public struct Property
    {
        public string Key { get; set; }

        private JToken _value;
        public JToken Value
        {
            get => _value;
            set => _value = value;
        }

        public T? GetValue<T>() => _value.ToObject<T>();
        [JsonIgnore]
        public int AsInt => _value.Value<int>();
        [JsonIgnore]
        public double AsDouble => _value.Value<double>();
        [JsonIgnore]
        public string? AsString => _value.Value<string>();
    }

    public string Name { get; set; }
    public string TemplateId { get; set; }
    public List<Property>? Properties { get; set; }

}