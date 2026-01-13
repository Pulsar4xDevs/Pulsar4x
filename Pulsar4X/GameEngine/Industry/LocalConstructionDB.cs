using System.Collections.Generic;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Industry;

public class LocalConstructionDB : BaseDataBlob
{
    /// <summary>
    /// Number of construction points provided by all installed LocalConstructionAtb components.
    /// </summary>
    public long PointsPerDay { get; set; }

    /// <summary>
    /// Queue of construction jobs to be processed in FIFO order.
    /// </summary>
    public Queue<LocalConstructionJob> BuildQueue { get; set; } = new Queue<LocalConstructionJob>();

    public LocalConstructionDB()
    {
    }

    public LocalConstructionDB(LocalConstructionDB other)
    {
        PointsPerDay = other.PointsPerDay;
        BuildQueue = new Queue<LocalConstructionJob>(other.BuildQueue);
    }

    public override object Clone()
    {
        return new LocalConstructionDB(this);
    }
}