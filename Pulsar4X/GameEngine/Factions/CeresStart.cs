using Pulsar4X.Energy;
using Pulsar4X.Engine;

namespace Pulsar4X.Factions;

/// <summary>
/// Numbers for the Ceres trade test. Ship factions use <see cref="StartingFunds"/>. The depot
/// blueprint uses <see cref="DepotFunds"/>. Since energy economy phase 5 the depot's goods are
/// priced from embodied energy (<see cref="EnergyPricing"/>): ask = E x credits per kWh x (1 + markup),
/// the iron bid is a share of its ask. Chart prices are intel, not goods, and stay fixed.
/// Other colonies keep Offer stock's ask 0 and bid 0.
/// </summary>
public static class CeresStart
{
    public const int StartingFunds = 100;
    public const int DepotFunds = 100000;
    public const int ChartCount = 3;
    public const decimal ChartAsk = 10m;

    public const string FuelId = "methalox";
    public const string IronId = "iron";
    public const string WaterId = "water";

    /// <summary>The depot's methalox ask (3 before phase 5).</summary>
    public static decimal FuelAsk(Game? game, FactionDataStore data) => EnergyPricing.SeedAsk(game, data, FuelId);

    /// <summary>The depot's iron ask (2 before phase 5).</summary>
    public static decimal IronAsk(Game? game, FactionDataStore data) => EnergyPricing.SeedAsk(game, data, IronId);

    /// <summary>The depot's iron bid (1 before phase 5).</summary>
    public static decimal IronBid(Game? game, FactionDataStore data) => EnergyPricing.SeedBid(game, data, IronId);

    /// <summary>The depot's water ask (1 before phase 5).</summary>
    public static decimal WaterAsk(Game? game, FactionDataStore data) => EnergyPricing.SeedAsk(game, data, WaterId);

    /// <summary>
    /// One rate for every mineral in a direct offer: half the iron bid (every mineral carries the
    /// same extraction energy by default), at least 0.01.
    /// </summary>
    public static decimal LotPrice(Game? game, FactionDataStore data)
    {
        decimal half = System.Math.Round(IronBid(game, data) / 2m, 4, System.MidpointRounding.AwayFromZero);
        return half < 0.01m ? 0.01m : half;
    }

    /// <summary><see cref="LotPrice(Game, FactionDataStore)"/> with the buying office's faction data (0 if unknown).</summary>
    public static decimal LotPrice(Game game, int officeFactionId)
        => game != null && game.Factions.TryGetValue(officeFactionId, out var faction)
           && faction.TryGetDataBlob<FactionInfoDB>(out var info)
            ? LotPrice(game, info.Data)
            : 0m;

    public const string DepotColonyId = "colony-ceres-depot";
    public const string SurveyFactionName = "Strata Survey";
    public const string MiningFactionName = "Lode Mining";
    public const string DepotFactionName = "Ceres Depot";
    public const string SurveyFleetName = "Survey Flight";
    public const string MiningFleetName = "Mining Flight";
}
