using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine;

public class ProtoEntity : IHasDataBlobs
{
    public HashSet<Type> DataBlobTypes { get; } = new();
    public List<BaseDataBlob> DataBlobs { get; } = new();

    public ProtoEntity() { }
    public ProtoEntity(List<BaseDataBlob> dataBlobs)
    {
        foreach (BaseDataBlob baseDataBlob in dataBlobs)
        {
            SetDataBlob(baseDataBlob);
        }
    }

    public void SetDataBlob<T>(T dataBlob)
        where T : BaseDataBlob
    {
        SetDataBlob((BaseDataBlob)dataBlob);
    }

    public T GetDataBlob<T>() where T : BaseDataBlob
    {
        var type = typeof(T);

        return (T)DataBlobs.First(db => db.GetType() == type);
    }

    public void SetDataBlob(BaseDataBlob dataBlob)
    {
        Type dbType = dataBlob.GetType();
        if (DataBlobTypes.Contains(dbType))
        {
            var item = DataBlobs.Find(db => db.GetType() == dbType);
            if(item != null)
                DataBlobs.Remove(item);
        }

        DataBlobs.Add(dataBlob);
        DataBlobTypes.Add(dbType);
    }

    public bool TryGetDatablob<T>(out T? value) where T : BaseDataBlob
    {
        var type = typeof(T);

        if(DataBlobs.Any(db => db.GetType() == type))
        {
            value = GetDataBlob<T>();
            return true;
        }

        value = null;
        return false;
    }

    public List<BaseDataBlob> GetAllDataBlobs()
    {
        return DataBlobs;
    }
}