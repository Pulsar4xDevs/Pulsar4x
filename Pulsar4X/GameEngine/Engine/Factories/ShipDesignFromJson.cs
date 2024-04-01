using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine.Designs;

namespace Pulsar4X.Engine.Factories;

public static class ShipDesignFromJson
{
    public static ShipDesign Create(Entity faction, FactionDataStore factionDataStore, string filePath)
    {
        string fileContents = File.ReadAllText(filePath);
        var rootJson = JObject.Parse(fileContents);

        var factionInfoDB = faction.GetDataBlob<FactionInfoDB>();
        var shipComponents = new List<(ComponentDesign, int)>();

        var designName = rootJson["name"].ToString();

        var components = (JArray?)rootJson["components"];
        if(components != null)
        {
            foreach(var component in components)
            {
                var id = component["id"].ToString();
                var amount = (int?)component["amount"] ?? 0;

                shipComponents.Add((
                  factionInfoDB.InternalComponentDesigns[id],
                  amount
                ));
            }
        }

        var armorId = rootJson["armor"]["id"].ToString();
        var armorThickness = (int?)rootJson["armor"]["thickness"] ?? 1;

        var armor = factionDataStore.Armor[armorId];
        var design = new ShipDesign(factionInfoDB, designName, shipComponents, (armor, armorThickness))
        {
          DamageProfileDB = new EntityDamageProfileDB(shipComponents, (armor, armorThickness))
        };

        return design;
    }
}