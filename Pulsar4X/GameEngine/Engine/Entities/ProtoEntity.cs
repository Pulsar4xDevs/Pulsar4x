using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualBasic;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine;

public class ProtoEntity : IHasDataBlobs
{
    public List<BaseDataBlob> DataBlobs { get; set; } = new List<BaseDataBlob>();

    public ProtoEntity() { }
    public ProtoEntity(List<BaseDataBlob> dataBlobs)
    {
        DataBlobs = dataBlobs;
    }

    public bool TryGetDataBlob<T>(out T value) where T : BaseDataBlob
    {
        var type = typeof(T);
        var found = DataBlobs.Where(db => db.GetType() == type).Any();

        if(found)
        {
            value = GetDataBlob<T>();
            return true;
        }

        value = null;
        return false;
    }

    public T GetDataBlob<T>() where T : BaseDataBlob
    {
        var type = typeof(T);

        return (T)DataBlobs.Where(db => db.GetType() == type).First();
    }

    public void SetDataBlob(BaseDataBlob dataBlob)
    {
        DataBlobs.Add(dataBlob);
    }

    public bool TryGetDatablob<T>(out T value) where T : BaseDataBlob
    {
        var type = typeof(T);

        if(DataBlobs.Where(db => db.GetType() == type).Any())
        {
            value = GetDataBlob<T>();
            return true;
        }

        value = null;
        return false;
    }
}