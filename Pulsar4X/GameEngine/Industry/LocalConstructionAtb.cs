using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Industry;

public class LocalConstructionAtb : IComponentDesignAttribute
{
    public byte Level { get; set; }
    public int PointsPerDay { get; set; }

    public LocalConstructionAtb(int level, int pointsPerDay)
    {
        Level = (byte)level;
        PointsPerDay = pointsPerDay;
    }

    public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        // Ensure the LocalConstructionDB exists.
        if (!parentEntity.TryGetDataBlob<LocalConstructionDB>(out var localConstructionDB))
        {
            localConstructionDB = new LocalConstructionDB();
            parentEntity.SetDataBlob(localConstructionDB);
        }

        // Add this component's construction points to the total.
        localConstructionDB.PointsPerDay += Level * PointsPerDay;
    }

    public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        if (parentEntity.TryGetDataBlob<LocalConstructionDB>(out var localConstructionDB))
        {
            // Subtract this component's construction points from the total.
            localConstructionDB.PointsPerDay -= Level * PointsPerDay;

            // If no construction points remain, remove the DataBlob.
            if (localConstructionDB.PointsPerDay <= 0)
            {
                parentEntity.RemoveDataBlob<LocalConstructionDB>();
            }
        }
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