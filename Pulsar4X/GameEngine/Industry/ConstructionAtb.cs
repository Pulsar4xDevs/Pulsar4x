using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Industry;

public class ConstructionAtb : IComponentDesignAttribute
{
    [JsonProperty]
    public Dictionary<string, int> ConstructionPoints { get; private set; } = new();

    [JsonConstructor]
    private ConstructionAtb() { }

    public ConstructionAtb(Dictionary<string, double> rates)
    {
        foreach (var (key, value) in rates)
            ConstructionPoints[key] = (int)value;
    }

    public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        Apply(parentEntity, 1);
    }

    public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        Apply(parentEntity, -1);
    }

    /// <summary>
    /// Installation points and ship-refit points share the blob and stay separate pools.
    /// The blob drops only when both pools are empty.
    /// </summary>
    void Apply(Entity parentEntity, int sign)
    {
        bool hasInstallation = ConstructionPoints.TryGetValue("installation", out int installation) && installation != 0;
        bool hasRefit = ConstructionPoints.TryGetValue("ship-refit", out int refit) && refit != 0;
        if (!hasInstallation && !hasRefit)
            return;

        if (!parentEntity.TryGetDataBlob<LocalConstructionDB>(out var localConstructionDB))
        {
            if (sign < 0)
                return;
            localConstructionDB = new LocalConstructionDB();
            parentEntity.SetDataBlob(localConstructionDB);
        }

        if (hasInstallation)
            localConstructionDB.PointsPerDay += sign * installation;
        if (hasRefit)
            localConstructionDB.RefitPointsPerDay += sign * refit;

        if (localConstructionDB.PointsPerDay <= 0 && localConstructionDB.RefitPointsPerDay <= 0)
            parentEntity.RemoveDataBlob<LocalConstructionDB>();
    }

    public string AtbName()
    {
        return "Local Construction";
    }

    public string AtbDescription()
    {
        return "Allows for local construction of components and ships.";
    }
}
