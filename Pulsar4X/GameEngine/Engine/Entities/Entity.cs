using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine;

public delegate void EntityChangeHandler (EntityChangeData.EntityChangeType changeType, BaseDataBlob db);

[DebuggerDisplay("{DebuggerDisplay}")]
public class Entity : IHasDataBlobs
{
    public int Id { get; private set; }

    [JsonIgnore]
    public EntityManager Manager { get; internal set; }

    public event EntityChangeHandler ChangeEvent;

    [JsonConstructor]
    public Entity(int id)
    {
        Id = id;
    }

    public static Entity Create()
    {
        var entityId = EntityIDGenerator.GenerateUniqueID();
        return new Entity(entityId);
    }

    public static readonly Entity InvalidEntity = new Entity(-1);

    [JsonIgnore]
    public bool IsValid => Id >= 0 && Manager != null && Manager != EntityManager.InvalidManager && Manager.IsValidEntity(this) && this.AreAllDependenciesPresent();

    public T GetDataBlob<T>() where T : BaseDataBlob
    {
        return Manager.GetDataBlob<T>(Id);
    }

    public BaseDataBlob GetDataBlob(Type type)
    {
        return Manager.GetDataBlob(Id, type);
    }

    public bool HasDataBlob<T>() where T : BaseDataBlob
    {
        return Manager.HasDataBlob<T>(Id);
    }

    public bool HasDataBlob(Type type)
    {
        return Manager.HasDataBlob(Id, type);
    }

    public bool TryGetDatablob<T>(out T value) where T : BaseDataBlob
    {
        if(Manager.HasDataBlob<T>(Id))
        {
            value = Manager.GetDataBlob<T>(Id);
            return true;
        }

        value = null;
        return false;
    }

    public List<BaseDataBlob> GetAllDataBlobs()
    {
        return Manager.GetAllDataBlobsForEntity(Id);
    }

    public void SetDataBlob<T>(T dataBlob) where T : BaseDataBlob
    {
        Manager.SetDataBlob(Id, dataBlob);
    }

    public void RemoveDataBlob<T>() where T : BaseDataBlob
    {
        Manager.RemoveDatablob<T>(Id);
    }

    public void InvokeChangeEvent(EntityChangeData.EntityChangeType changeType, BaseDataBlob db)
    {
        ChangeEvent?.Invoke(changeType, db);
    }

    [JsonIgnore]
    public DateTime StarSysDateTime
    {
        get { return Manager.StarSysDateTime; }
    }

    public int FactionOwnerID { get; set; }

    [JsonIgnore]
    public Entity GetFactionOwner => Manager.Game.Factions[FactionOwnerID];

    public void AddComponent(ComponentInstance componentInstance)
    {
        componentInstance.ParentEntity = this;
        var instancesDB = GetDataBlob<ComponentInstancesDB>();
        instancesDB.AddComponentInstance(componentInstance);

        foreach (var atbkvp in componentInstance.Design.AttributesByType)
        {
            atbkvp.Value.OnComponentInstallation(this, componentInstance);
        }

        ReCalcProcessor.ReCalcAbilities(this);
    }

    public void AddComponent(ComponentDesign componentDesign, int count = 1)
    {
        for(int i = 0; i < count; i++)
        {
            AddComponent(new ComponentInstance(componentDesign));
        }
    }

    public void AddComponent(List<ComponentInstance> instances)
    {
        foreach(var instance in instances)
        {
            AddComponent(instance);
        }
    }

    public void AddComponent(List<ComponentDesign> designs)
    {
        foreach(var design in designs)
        {
            AddComponent(design);
        }
    }

    public void RemoveComponent(ComponentInstance instance)
    {
        instance.ParentEntity = this;
        var instancesDB = GetDataBlob<ComponentInstancesDB>();

        foreach (var atbkvp in instance.Design.AttributesByType)
        {
            atbkvp.Value.OnComponentUninstallation(this, instance);
        }

        instancesDB.RemoveComponentInstance(instance);
    }

    public void Destroy()
    {
        Manager.RemoveEntity(this);
        Manager = null;
        FactionOwnerID = -1;
    }

    [JsonIgnore]
    private string DebuggerDisplay
    {
        get
        {
            string value = $"({Id})";
            if(HasDataBlob<NameDB>()) value += " " + GetDataBlob<NameDB>().OwnersName;

            return value;
        }
    }
}
