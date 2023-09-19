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
            Mineral,
            ProcessedMaterial,
            SystemGenSettings,
            Tech,
            Theme,
        }
        public enum OperationType { Default, Remove }
        public DataType Type { get; set; }
        public OperationType Operation { get; set; } = OperationType.Default;

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

            switch (instruction.Type)
            {
                case ModInstruction.DataType.Armor:
                    instruction.Data = jObject["Payload"].ToObject<ArmorSD>();
                    break;
                case ModInstruction.DataType.CargoType:
                    instruction.Data = jObject["Payload"].ToObject<CargoTypeSD>();
                    break;
                //... Handle other types accordingly
            }

            return instruction;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

}