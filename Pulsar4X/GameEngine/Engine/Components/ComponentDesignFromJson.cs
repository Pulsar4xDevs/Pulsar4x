using System;
using Newtonsoft.Json.Linq;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
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
                    designer.ComponentDesignProperties[property.Key].SetValueFromInput(property.AsInt, clampToTrack: false);
                }
                else if(property.Value.Type == JTokenType.Float)
                {
                    designer.ComponentDesignProperties[property.Key].SetValueFromInput(property.AsDouble, clampToTrack: false);
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
}
