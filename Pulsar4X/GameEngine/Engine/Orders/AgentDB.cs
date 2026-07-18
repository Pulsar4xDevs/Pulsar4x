using Pulsar4X.Datablobs;

namespace GameEngine.Engine.Orders;
/// <summary>
/// Holds RPG style attributes and personality properties for an administrator/commander
/// These directly influence how an agent carries out his goals.
/// </summary>

public class AgentDB : BaseDataBlob
{
    
    //basic peronality traits. 
    public float Aggression { get; set; } = 1f;      // affects Attack/Defend weighting
    public float Caution { get; set; } = 1f;         // boosts StayAlive, DontRunOutOfFuel
    public float Curiosity { get; set; } = 1f;       // boosts ExploreJP, ScanAnomalies
    public float Greed { get; set; } = 1f;           // boosts MakeProfit, Trade
    public float Loyalty { get; set; } = 1f;         // how strictly they follow superior orders
}