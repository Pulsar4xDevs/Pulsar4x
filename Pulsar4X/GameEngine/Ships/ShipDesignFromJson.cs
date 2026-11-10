using System.Collections.Generic;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
using Pulsar4X.Damage;
using Pulsar4X.Engine;
using Pulsar4X.Factions;

namespace Pulsar4X.Ships;

public static class ShipDesignFromJson
{
    public static ShipDesign Create(Entity faction, FactionDataStore factionDataStore, ShipDesignBlueprint shipDesignBlueprint)
    {
        var factionInfoDB = faction.GetDataBlob<FactionInfoDB>();
        var shipComponents = new List<(ComponentDesign, int)>();

        foreach(var component in shipDesignBlueprint.Components)
        {
            shipComponents.Add((
                factionInfoDB.InternalComponentDesigns[component.Id],
                (int)component.Amount
            ));
        }

        var armor = factionDataStore.Armor[shipDesignBlueprint.Armor.Id];
        var design = new ShipDesign(factionInfoDB, shipDesignBlueprint.Name, shipComponents, (armor, shipDesignBlueprint.Armor.Thickness), shipDesignBlueprint.UniqueID)
        {
            DamageProfileDB = new EntityDamageProfileDB(shipComponents, (armor, shipDesignBlueprint.Armor.Thickness)),
            Tanker = shipDesignBlueprint.Tanker,
        };
        design.Initialise(factionInfoDB);
        return design;
    }
}
