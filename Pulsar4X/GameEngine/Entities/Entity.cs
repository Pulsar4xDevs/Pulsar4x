using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine;

[DebuggerDisplay("{DebuggerDisplay}")]
public class Entity : IHasDataBlobs, IEquatable<Entity>
{
    public int Id { get; private set; }

    [JsonIgnore]
    public EntityManager? Manager { get; internal set; }

    [JsonConstructor]
    private Entity(int id)
    {
        Id = id;
    }

    public static Entity Create()
    {
        int entityId = EntityIDGenerator.GenerateUniqueID();
        return new Entity(entityId);
    }

    public static readonly Entity InvalidEntity = new Entity(-1);

    [JsonIgnore]
    public bool IsValid { get; internal set; } = false;

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

    public bool TryGetDatablob<T>([NotNullWhen(true)] out T? value) where T : BaseDataBlob
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
        Manager.TagEntityForRemoval(this);
        //manager does this:
        //Manager = null;
        //FactionOwnerID = -1;
    }

    public bool Equals(Entity? other)
    {
        return other != null
            && this.Id == other.Id
            && this.FactionOwnerID == other.FactionOwnerID
            && this.Manager.ManagerID.Equals(other.Manager.ManagerID);
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
