using System;
using System.IO;
using Newtonsoft.Json.Linq;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Events;
using Pulsar4X.Factions;

namespace Pulsar4X.Engine.Factories;

public static class ComponentDesignFromJson
{
    public static ComponentDesign Create(Entity faction, FactionDataStore factionDataStore, ComponentDesignBlueprint componentDesignBlueprint)
    {
        ComponentDesign componentDesign;

        if(!factionDataStore.ComponentTemplates.ContainsKey(componentDesignBlueprint.TemplateId))
        {
            throw new Exception($"{componentDesignBlueprint.TemplateId} was not found in the faction data store. Please check the TemplateId matches a ComponentTemplate UniqueId");
        }

        var componentTemplateBlueprint = factionDataStore.ComponentTemplates[componentDesignBlueprint.TemplateId];
        var designer = new ComponentDesigner(componentTemplateBlueprint, factionDataStore, faction.GetDataBlob<FactionTechDB>(), componentDesignBlueprint.UniqueID) {
            Name = componentDesignBlueprint.Name
        };

        if(componentDesignBlueprint.Properties != null)
        {
            foreach(var property in componentDesignBlueprint.Properties)
            {
                if(property.Value.Type == JTokenType.Integer)
                {
                    designer.ComponentDesignProperties[property.Key].SetValueFromInput(property.AsInt);
                }
                else if(property.Value.Type == JTokenType.Float)
                {
                    designer.ComponentDesignProperties[property.Key].SetValueFromInput(property.AsDouble);
                }
                else if(property.AsString != null)
                {
                    designer.ComponentDesignProperties[property.Key].SetValueFromString(property.AsString);
                }
                else
                {
                    designer.ComponentDesignProperties[property.Key].SetValueFromString(property.Value.ToString());
                }
            }
        }

        componentDesign = designer.CreateDesign(faction);
        factionDataStore.IncrementTechLevel(componentDesign.TechID);

        return componentDesign;
    }

    public static ComponentDesign Create(Entity faction, FactionDataStore factionDataStore, string filePath)
    {
        string fileContents = File.ReadAllText(filePath);
        var rootJson = JObject.Parse(fileContents);

        var templateName = rootJson["templateId"].ToString();
        var designName = rootJson["name"].ToString();
        var id = rootJson["id"] == null ? null : rootJson["id"].ToString();

        ComponentDesign design;
        if (!factionDataStore.ComponentTemplates.ContainsKey(templateName))
        {
            var factionID = faction.Id;
            string errstr = templateName + " not found in faction data store, this  may be due to the faction data being incorrect or needs to be included in DefaultItems";
            var e = Event.Create(EventType.DataParseError, DateTime.Now, errstr, factionID, null, factionID, null  );
            Events.EventManager.Instance.Publish(e);
            throw new Exception(errstr);
        }
        var blueprint = factionDataStore.ComponentTemplates[templateName];
        var designer = new ComponentDesigner(blueprint, factionDataStore, faction.GetDataBlob<FactionTechDB>(), id){
            Name = designName
        };

        var properties = (JArray?)rootJson["Properties"];
        if(properties != null)
        {
            foreach(var prop in properties)
            {
                var key = prop["key"].ToString();
                var valueType = prop["value"];
                if(valueType.Type == JTokenType.Integer)
                {
                    designer.ComponentDesignProperties[key].SetValueFromInput((int?)prop["value"] ?? 0);
                }
                else if(valueType.Type == JTokenType.Float)
                {
                    designer.ComponentDesignProperties[key].SetValueFromInput((double?)prop["value"] ?? 0.0);
                }
                else
                {
                    designer.ComponentDesignProperties[key].SetValueFromString(prop["value"].ToString());
                }
            }
        }

        design = designer.CreateDesign(faction);
        factionDataStore.IncrementTechLevel(design.TechID);

        return design;
    }
}