namespace Pulsar4X.Factions;

/// <summary>
/// Fixed numbers for the Ceres trade test. Not a price model.
/// Blueprint <c>StartingFunds</c> uses <see cref="StartingFunds"/>.
/// Other colonies keep Offer stock's ask 0 and bid 0.
/// </summary>
public static class CeresStart
{
    public const int StartingFunds = 100;
    public const int ChartCount = 3;
    public const decimal ChartAsk = 10m;
    public const decimal FuelAsk = 3m;
    public const decimal IronAsk = 2m;
    public const decimal IronBid = 1m;
    public const decimal WaterAsk = 1m;

    public const string DepotColonyId = "colony-ceres-depot";
    public const string SurveyFactionName = "Strata Survey";
    public const string MiningFactionName = "Lode Mining";
    public const string DepotFactionName = "Ceres Depot";
}
