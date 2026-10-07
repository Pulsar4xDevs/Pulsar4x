using Newtonsoft.Json;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Industry;

public class AsteroidMinerAtb : IComponentDesignAttribute
{
    [JsonProperty]
    public int UnitsPerDay { get; set; }

    public AsteroidMinerAtb(int unitsPerDay)
    {
        UnitsPerDay = unitsPerDay;
    }

    public AsteroidMinerAtb(double unitsPerDay)
        : this((int)unitsPerDay)
    {
    }

    public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        if (parentEntity.TryGetDataBlob<AsteroidMineAbilityDB>(out var ability))
            ability.UnitsPerDay += UnitsPerDay;
        else
            parentEntity.SetDataBlob(new AsteroidMineAbilityDB { UnitsPerDay = UnitsPerDay });
    }

    public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        if (!parentEntity.TryGetDataBlob<AsteroidMineAbilityDB>(out var ability))
            return;

        if (UnitsPerDay >= ability.UnitsPerDay)
            parentEntity.RemoveDataBlob<AsteroidMineAbilityDB>();
        else
            ability.UnitsPerDay -= UnitsPerDay;
    }

    public string AtbName() => "Asteroid Miner";

    public string AtbDescription() => $"Rate {UnitsPerDay} units/day";
}
