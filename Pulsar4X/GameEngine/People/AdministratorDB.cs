using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.People;

public class AdministratorDB : BaseDataBlob
{
    /// <summary>
    /// The amount of points per day the researcher outputs
    /// </summary>
    public ModifiableValue<int> PointsPerDay { get; internal set; } = new (0);

    /// <summary>
    /// key = category Id
    /// value = percentage bonus to the category (0-1 range IE: 0.1 is a 10 percent bonus)
    /// </summary>
    public Dictionary<string, double> BonusCategories { get; internal set; } = new ();

    /// <summary>
    /// The *base* cost per day to operate the researcher
    /// </summary>
    public ModifiableValue<decimal> CostPerDay { get; internal set; } = new(0);

    /// <summary>
    /// Represents the funding level for the researcher. A value from 0-5.
    /// 0 = no funding, multipies the research point output by 0, multiplies the cost per day by 0
    /// 1 = standard funding, multiplies the research point output by 1, multiplies the cost per day by 1
    /// 2 = enhanced funding, multpilies the research point output by 2, multiplies the cost per day by 3
    /// 3 = gung-ho funding, multiplies the research point output by 3, multiplies the cost per day by 7
    /// 4 = full speed ahead, multiplies the research point output by 4, multiplies the cost per day by 13
    /// 5 = spared no expense, multiplies the research point output by 5, multiplies the cost by day by 22
    /// </summary>
    public byte FundingLevel { get; set; } = 1;

    public string FundingPointModifierId { get; internal set; } = "";

    public string FundingCostModifierId { get; internal set; } = "";

    /// <summary>
    /// The entity Id of the admin assigned
    /// </summary>
    public int AdministratorId { get; internal set; } = -1;

    /// <summary>
    /// The entity Id of the location the lab is at
    /// </summary>
    public int LocationId { get; internal set; } = -1;



    /// <summary>
    /// Needed for the UI
    /// </summary>
    public IConstructableDesign Design { get; internal set; }

    public AdministratorDB(IConstructableDesign design)
    {
        Design = design;
    }
}