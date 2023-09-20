using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Modding
{
    public class ModInstruction
    {
        public enum DataType
        {
            Armor,
            CargoType,
            ComponentTemplate,
            Gas,
            IndustryType,
            Mineral,
            ProcessedMaterial,
            SystemGenSettings,
            Tech,
            Theme,
        }
        public enum OperationType { Default, Remove }
        public enum CollectionOperationType { Add, Remove, Overwrite }
        public DataType Type { get; set; }
        public OperationType Operation { get; set; } = OperationType.Default;
        public CollectionOperationType? CollectionOperation { get; set;}

        [JsonIgnore]
        public SerializableGameData Data { get; set; }

        public JObject Payload { get; set; }
    }

    public class ModInstructionJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ModInstruction);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);
            var instruction = new ModInstruction
            {
                Type = jObject["Type"].ToObject<ModInstruction.DataType>()
            };
            if(jObject["Operation"] != null)
            {
                instruction.Operation = jObject["Operation"].ToObject<ModInstruction.OperationType>();
            }
            if(jObject["CollectionOperation"] != null)
            {
                instruction.CollectionOperation = jObject["CollectionOperation"].ToObject<ModInstruction.CollectionOperationType>();
            }

            switch (instruction.Type)
            {
                case ModInstruction.DataType.Armor:
                    instruction.Data = jObject["Payload"].ToObject<ArmorBlueprint>();
                    break;
                case ModInstruction.DataType.CargoType:
                    instruction.Data = jObject["Payload"].ToObject<CargoTypeBlueprint>();
                    break;
                case ModInstruction.DataType.ComponentTemplate:
                    instruction.Data = jObject["Payload"].ToObject<ComponentTemplateBlueprint>();
                    break;
                case ModInstruction.DataType.Gas:
                    instruction.Data = jObject["Payload"].ToObject<GasBlueprint>();
                    break;
                case ModInstruction.DataType.IndustryType:
                    instruction.Data = jObject["Payload"].ToObject<IndustryTypeBlueprint>();
                    break;
                case ModInstruction.DataType.Mineral:
                    instruction.Data = jObject["Payload"].ToObject<MineralBlueprint>();
                    break;
                case ModInstruction.DataType.ProcessedMaterial:
                    instruction.Data = jObject["Payload"].ToObject<ProcessedMaterialBlueprint>();
                    break;
                case ModInstruction.DataType.SystemGenSettings:
                    instruction.Data = jObject["Payload"].ToObject<SystemGenSettingsBlueprint>(serializer);
                    break;
                case ModInstruction.DataType.Tech:
                    instruction.Data = jObject["Payload"].ToObject<TechBlueprint>();
                    break;
                case ModInstruction.DataType.Theme:
                    instruction.Data = jObject["Payload"].ToObject<ThemeBlueprint>();
                    break;
            }

            return instruction;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

}