namespace Pulsar4X.Factions;

/// <summary>
/// Fixed numbers for the Ceres trade test. Not a price model.
/// Ship factions use <see cref="StartingFunds"/>. The depot blueprint uses <see cref="DepotFunds"/>.
/// Other colonies keep Offer stock's ask 0 and bid 0.
/// </summary>
public static class CeresStart
{
    public const int StartingFunds = 100;
    public const int DepotFunds = 100000;
    public const int ChartCount = 3;
    public const decimal ChartAsk = 10m;
    public const decimal FuelAsk = 3m;
    public const decimal IronAsk = 2m;
    public const decimal IronBid = 1m;

    /// <summary>
    /// One rate for every mineral in a direct offer. Half the iron bid. Not a price model.
    /// </summary>
    public const decimal LotPrice = IronBid / 2m;

    public const decimal WaterAsk = 1m;

    public const string DepotColonyId = "colony-ceres-depot";
    public const string SurveyFactionName = "Strata Survey";
    public const string MiningFactionName = "Lode Mining";
    public const string DepotFactionName = "Ceres Depot";
    public const string SurveyFleetName = "Survey Flight";
    public const string MiningFleetName = "Mining Flight";
}
