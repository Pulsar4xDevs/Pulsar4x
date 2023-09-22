using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Engine
{
    /// <summary>
    /// A ProtoEntity is an entity that is not stored in an EntityManager.
    /// It holds its own datablobs and provides all other functionality (but not performance) of the EntityManager.
    /// </summary>
    [PublicAPI]
    [JsonConverter(typeof(ProtoEntityConverter))]
    public class ProtoEntity: IGetValuesHash
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

        public virtual int GetValueCompareHash(int hash = 17)
        {
            foreach (var item in DataBlobs)
            {
                if (item != null)
                {
                    if (item is IGetValuesHash)
                        hash = ((IGetValuesHash)item).GetValueCompareHash(hash);
                    else
                        hash = Misc.ValueHash(item, hash);
                }
            }
            return hash;
        }

        internal class ProtoEntityConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(ProtoEntity);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                //bool exists1 = Testing.manager.EntityExistsGlobaly(Testing.entityID);
                var protoEntity = new ProtoEntity();
                //StarObject (Entity)
                reader.Read(); // PropertyName ID
                reader.Read(); // Actual ID
                protoEntity.Guid = serializer.Deserialize<Guid>(reader); // Deserialize the ID
                //bool exists2 = Testing.manager.EntityExistsGlobaly(Testing.entityID);
                // Deserialize the dataBlobs
                reader.Read(); // PropertyName DATABLOB
                while (reader.TokenType == JsonToken.PropertyName)
                {
                    var typestring = "Pulsar4X.ECSLib." + (string)reader.Value;
                    Type dataBlobType = Type.GetType(typestring);
                    reader.Read(); // StartObject (dataBlob)
                    if (reader.TokenType == JsonToken.EndObject)
                    {
                        break;
                    }
                    else
                    {
                        //bool exists3 = Testing.manager.EntityExistsGlobaly(Testing.entityID);
                        BaseDataBlob dataBlob = (BaseDataBlob)serializer.Deserialize(reader, dataBlobType); // EndObject (dataBlob)
                                                                                                            //bool exists4 = Testing.manager.EntityExistsGlobaly(Testing.entityID);
                        protoEntity.SetDataBlob(dataBlob);
                    }
                    reader.Read(); // PropertyName DATABLOB OR EndObject (Entity)
                    //bool exists5 = Testing.manager.EntityExistsGlobaly(Testing.entityID);
                }

                //bool exists6 = Testing.manager.EntityExistsGlobaly(Testing.entityID);
                return protoEntity;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                ProtoEntity protoEntity = (ProtoEntity)value;
                writer.WriteStartObject(); // Start the Entity.
                writer.WritePropertyName("ID"); // Write the ID PropertyName
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