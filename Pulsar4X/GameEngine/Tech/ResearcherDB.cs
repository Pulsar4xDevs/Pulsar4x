using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Technology;

public class ResearcherDB : BaseDataBlob
{
    /// <summary>
    /// The *base* amount of points per day the researcher outputs
    /// </summary>
    [JsonProperty]
    public int BaseResearchPoints { get; internal set; } = 0;

    /// <summary>
    /// The actual amount of points per day the researcher outputs.
    /// The base value is modified by category bonus, scientist bonus, corporation bonuses
    /// as appropriate to get the calculated cost.
    /// </summary>
    [JsonProperty]
    public int CalculatedResearchPoints { get; internal set; } = 0;

    /// <summary>
    /// key = category Id
    /// value = percentage bonus to the category (0-1 range IE: 0.1 is a 10 percent bonus)
    /// </summary>
    [JsonProperty]
    public Dictionary<string, double> BonusCategories { get; internal set; } = new ();

    /// <summary>
    /// The *base* cost per day to operate the researcher
    /// </summary>
    [JsonProperty]
    public decimal BaseCostPerDay { get; internal set; } = 0;

    /// <summary>
    /// The actual cost per day to operate the researcher.
    /// The base cost is modified by various bonuses to get the calculated cost.
    /// </summary>
    [JsonProperty]
    public decimal CalculatedCostPerDay { get; internal set; } = 0;

    /// <summary>
    /// Represents the funding level for the researcher. A value from 0-5.
    /// 0 = no funding, multipies the research point output by 0, multiplies the cost per day by 0
    /// 1 = standard funding, multiplies the research point output by 1, multiplies the cost per day by 1
    /// 2 = enhanced funding, multpilies the research point output by 2, multiplies the cost per day by 3
    /// 3 = gung-ho funding, multiplies the research point output by 3, multiplies the cost per day by 7
    /// 4 = full speed ahead, multiplies the research point output by 4, multiplies the cost per day by 13
    /// 5 = spared no expense, multiplies the research point output by 5, multiplies the cost by day by 22
    /// </summary>
    [JsonProperty]
    public byte FundingLevel { get; set; } = 1;

    /// <summary>
    /// The entity Id of the scientist assigned to the researcher
    /// </summary>
    [JsonProperty]
    public int ScientistId { get; internal set; } = -1;

    /// <summary>
    /// The entity Id of the location the lab is at
    /// </summary>
    [JsonProperty]
    public int LocationId { get; internal set; } = -1;

    /// <summary>
    /// The Id of the tech this researcher is researching
    /// </summary>
    [JsonProperty]
    public ReorderableSafeQueue<string> TechQueue { get; private set; } = new ();

    /// <summary>
    /// Needed for the UI
    /// </summary>
    [JsonProperty]
    public ComponentDesign Design { get; internal set; }

    public ResearcherDB(ComponentDesign design)
    {
        Design = design;
    }
}