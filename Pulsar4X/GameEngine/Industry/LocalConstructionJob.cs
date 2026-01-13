using Pulsar4X.Components;

namespace Pulsar4X.Industry;

/// <summary>
/// Represents a construction project in the local construction queue.
/// </summary>
public class LocalConstructionJob
{
    /// <summary>
    /// The component design being constructed
    /// </summary>
    public ComponentDesign Design { get; set; }

    /// <summary>
    /// Construction points already applied to the current item
    /// </summary>
    public long PointsAccumulated { get; set; }

    public LocalConstructionJob(ComponentDesign design, int pointsAccumulated = 0)
    {
        Design = design;
        PointsAccumulated = pointsAccumulated;
    }

    /// <summary>
    /// Whether this job is complete
    /// </summary>
    public bool IsComplete => PointsAccumulated >= Design.IndustryPointCosts;

    /// <summary>
    /// Progress on the current item (0.0 to 1.0)
    /// </summary>
    public double CurrentItemProgress
    {
        get
        {
            if (Design.IndustryPointCosts == 0) return 1.0;
            return (double)PointsAccumulated / Design.IndustryPointCosts;
        }
    }
}
