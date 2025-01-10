using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
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
            DamageProfileDB = new EntityDamageProfileDB(shipComponents, (armor, shipDesignBlueprint.Armor.Thickness))
        };
        design.Initialise(factionInfoDB);
        return design;
    }

    public static ShipDesign Create(Entity faction, FactionDataStore factionDataStore, string filePath)
    {
        string fileContents = File.ReadAllText(filePath);
        var rootJson = JObject.Parse(fileContents);

        var factionInfoDB = faction.GetDataBlob<FactionInfoDB>();
        var shipComponents = new List<(ComponentDesign, int)>();

        var id = (string?)rootJson["id"] ?? null;
        var designName = rootJson["name"].ToString();

        var components = (JArray?)rootJson["components"];
        if(components != null)
        {
            foreach(var component in components)
            {
                var designId = component["id"].ToString();
                var amount = (int?)component["amount"] ?? 0;

                shipComponents.Add((
                  factionInfoDB.InternalComponentDesigns[designId],
                  amount
                ));
            }
        }

        var armorId = rootJson["armor"]["id"].ToString();
        var armorThickness = (int?)rootJson["armor"]["thickness"] ?? 1;

        var armor = factionDataStore.Armor[armorId];
        var design = new ShipDesign(factionInfoDB, designName, shipComponents, (armor, armorThickness), id)
        {
          DamageProfileDB = new EntityDamageProfileDB(shipComponents, (armor, armorThickness))
        };
        design.Initialise(factionInfoDB);
        return design;
    }
}