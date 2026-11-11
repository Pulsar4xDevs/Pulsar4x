using System.Collections.Generic;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
using Pulsar4X.Damage;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Modding;

namespace Pulsar4X.Ships;

public static class ShipDesignFromJson
{
    public static ShipDesign Create(Entity faction, FactionDataStore factionDataStore, ShipDesignBlueprint shipDesignBlueprint)
    {
        var factionInfoDB = faction.GetDataBlob<FactionInfoDB>();
        var shipComponents = new List<(ComponentDesign, int)>();

        foreach(var component in shipDesignBlueprint.Components)
        {
            if (!factionInfoDB.InternalComponentDesigns.TryGetValue(component.Id, out var componentDesign))
            {
                throw new ModLoadException(new ModLoadError(
                    shipDesignBlueprint.ModNamespace,
                    shipDesignBlueprint.JsonFileName,
                    nameof(ShipDesignBlueprint),
                    shipDesignBlueprint.UniqueID,
                    "Components",
                    component.Id));
            }

            shipComponents.Add((componentDesign, (int)component.Amount));
        }

        if (!factionDataStore.Armor.TryGetValue(shipDesignBlueprint.Armor.Id, out var armor))
        {
            throw new ModLoadException(new ModLoadError(
                shipDesignBlueprint.ModNamespace,
                shipDesignBlueprint.JsonFileName,
                nameof(ShipDesignBlueprint),
                shipDesignBlueprint.UniqueID,
                "Armor.Id",
                shipDesignBlueprint.Armor.Id));
        }

        var design = new ShipDesign(factionInfoDB, shipDesignBlueprint.Name, shipComponents, (armor, shipDesignBlueprint.Armor.Thickness), shipDesignBlueprint.UniqueID)
        {
            DamageProfileDB = new EntityDamageProfileDB(shipComponents, (armor, shipDesignBlueprint.Armor.Thickness)),
            Tanker = shipDesignBlueprint.Tanker,
        };
        design.Initialise(factionInfoDB);
        return design;
    }
}
