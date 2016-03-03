using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// A ProtoEntity is an entity that is not stored in an EntityManager.
    /// It holds its own datablobs and provides all other functionality (but not performance) of the EntityManager.
    /// </summary>
    [PublicAPI]
    [JsonConverter(typeof(ProtoEntityConverter))]
    public class ProtoEntity
    {
        [PublicAPI]
        public List<BaseDataBlob> DataBlobs { get; set; } = EntityManager.BlankDataBlobList();

        [PublicAPI]
        public Guid Guid { get; protected internal set; }

        [NotNull]
        [PublicAPI]
        public ComparableBitArray DataBlobMask => _protectedDataBlobMask_;

        protected ComparableBitArray _protectedDataBlobMask_ = EntityManager.BlankDataBlobMask();

        [PublicAPI]
        public static ProtoEntity Create(Guid guid, IEnumerable<BaseDataBlob> dataBlobs = null)
        {
            var protoEntity = new ProtoEntity
            {
                DataBlobs = EntityManager.BlankDataBlobList(),
                Guid = guid
            };

            if (dataBlobs == null)
            {
                return protoEntity;
            }
            foreach (BaseDataBlob dataBlob in dataBlobs)
            {
                protoEntity.SetDataBlob(dataBlob);
            }

            return protoEntity;
        }

        [PublicAPI]
        public static ProtoEntity Create(IEnumerable<BaseDataBlob> dataBlobs = null)
        {
            return Create(Guid.Empty, dataBlobs);
        }

        [PublicAPI]
        public virtual T GetDataBlob<T>() where T : BaseDataBlob
        {
            return (T)DataBlobs[EntityManager.GetTypeIndex<T>()];
        }

        [PublicAPI]
        public virtual T GetDataBlob<T>(int typeIndex) where T : BaseDataBlob
        {
            return (T)DataBlobs[typeIndex];
        }

        [PublicAPI]
        public virtual void SetDataBlob<T>(T dataBlob) where T : BaseDataBlob
        {
            int typeIndex;
            EntityManager.TryGetTypeIndex(dataBlob.GetType(), out typeIndex);
            DataBlobs[typeIndex] = dataBlob;
            _protectedDataBlobMask_[typeIndex] = true;
        }

        [PublicAPI]
        public virtual void SetDataBlob<T>(T dataBlob, int typeIndex) where T : BaseDataBlob
        {
            DataBlobs[typeIndex] = dataBlob;
            _protectedDataBlobMask_[typeIndex] = true;
        }

        [PublicAPI]
        public virtual void RemoveDataBlob<T>() where T : BaseDataBlob
        {
            int typeIndex = EntityManager.GetTypeIndex<T>();
            DataBlobs[typeIndex] = null;
            _protectedDataBlobMask_[typeIndex] = false;
        }

        [PublicAPI]
        public virtual void RemoveDataBlob(int typeIndex)
        {
            DataBlobs[typeIndex] = null;
            _protectedDataBlobMask_[typeIndex] = false;
        }

        internal class ProtoEntityConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(ProtoEntity);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var protoEntity = new ProtoEntity();

                //StarObject (Entity)
                reader.Read(); // PropertyName Guid
                reader.Read(); // Actual Guid
                protoEntity.Guid = serializer.Deserialize<Guid>(reader); // Deserialize the Guid

                // Deserialize the dataBlobs
                reader.Read(); // PropertyName DATABLOB
                while (reader.TokenType == JsonToken.PropertyName)
                {
                    Type dataBlobType = Type.GetType("Pulsar4X.ECSLib." + (string)reader.Value);
                    reader.Read(); // StartObject (dataBlob)
                    BaseDataBlob dataBlob = (BaseDataBlob)serializer.Deserialize(reader, dataBlobType); // EndObject (dataBlob)
                    protoEntity.SetDataBlob(dataBlob);
                    reader.Read(); // PropertyName DATABLOB OR EndObject (Entity)
                }

                return protoEntity;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                ProtoEntity protoEntity = (ProtoEntity)value;
                writer.WriteStartObject(); // Start the Entity.
                writer.WritePropertyName("Guid"); // Write the Guid PropertyName
                serializer.Serialize(writer, protoEntity.Guid); // Write the Entity's guid.
                foreach (BaseDataBlob dataBlob in protoEntity.DataBlobs.Where(dataBlob => dataBlob != null))
                {
                    writer.WritePropertyName(dataBlob.GetType().Name); // Write the PropertyName of the dataBlob as the dataBlob's type.
                    serializer.Serialize(writer, dataBlob); // Serialize the dataBlob in this property.
                }
                writer.WriteEndObject(); // End then Entity.
            }
        }
    }
}