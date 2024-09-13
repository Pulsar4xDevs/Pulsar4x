using System.IO;
using Newtonsoft.Json.Linq;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine.Factories;

public static class ComponentDesignFromJson
{
    public static ComponentDesign Create(Entity faction, FactionDataStore factionDataStore, string filePath)
    {
        string fileContents = File.ReadAllText(filePath);
        var rootJson = JObject.Parse(fileContents);

        var templateName = rootJson["templateId"].ToString();
        var designName = rootJson["name"].ToString();
        var id = rootJson["id"] == null ? null : rootJson["id"].ToString();

        ComponentDesign design;
        var blueprint = factionDataStore.ComponentTemplates[templateName];
        var designer = new ComponentDesigner(blueprint, factionDataStore, faction.GetDataBlob<FactionTechDB>(), id){
            Name = designName
        };

        var attributes = (JArray?)rootJson["attributes"];
        if(attributes != null)
        {
            foreach(var attribute in attributes)
            {
                var key = attribute["key"].ToString();
                var valueType = attribute["value"];
                if(valueType.Type == JTokenType.Integer)
                {
                    designer.ComponentDesignAttributes[key].SetValueFromInput((int?)attribute["value"] ?? 0);
                }
                else if(valueType.Type == JTokenType.Float)
                {
                    designer.ComponentDesignAttributes[key].SetValueFromInput((double?)attribute["value"] ?? 0.0);
                }
                else
                {
                    designer.ComponentDesignAttributes[key].SetValueFromString(attribute["value"].ToString());
                }
            }
        }

        design = designer.CreateDesign(faction);
        factionDataStore.IncrementTechLevel(design.TechID);

        return design;
    }
}