using System.Collections.Generic;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Industry;

public class LocalConstructionDB : BaseDataBlob
{
    /// <summary>
    /// Installation points per day from mounted ConstructionAtb components.
    /// </summary>
    public long PointsPerDay { get; set; }

    /// <summary>
    /// Ship-refit points per day. Spent only on refit jobs (normal jobs on a shipyard line), never on installation work.
    /// </summary>
    public long RefitPointsPerDay { get; set; }

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
        RefitPointsPerDay = other.RefitPointsPerDay;
        BuildQueue = new Queue<LocalConstructionJob>(other.BuildQueue);
    }

    public override object Clone()
    {
        return new LocalConstructionDB(this);
    }
}