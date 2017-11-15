using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    //TODO: make these internal or use auth (tests currently use these)
    public static class NameLookup
    {

        internal static Entity TryGetFirstEntityWithName(EntityManager manager, string name)
        {          
            List<Entity> list = manager.GetAllEntitiesWithDataBlob<NameDB>();
            return TryGetFirstEntityWithName(list, name);
        }

        internal static Entity TryGetFirstEntityWithName(List<Entity> entitysWithNameDB, string name)
        {
            foreach (var item in entitysWithNameDB)
            {
                NameDB namedb = item.GetDataBlob<NameDB>();
                if (namedb.DefaultName == name)
                {
                    
                    return item;
                }
            }
            throw new Exception(name + " not found");
        }


        internal static MineralSD TryGetMineralSD(Game game, string name)
        {
            foreach (var kvp in game.StaticData.Minerals)
            {
                if (name == kvp.Value.Name)
                {
                    return kvp.Value;
                }
            }
            throw new Exception(name + " not found");
        }
        
        internal static ProcessedMaterialSD TryGetMaterialSD(Game game, string name)
        {
            foreach (var kvp in game.StaticData.ProcessedMaterials)
            {
                if (name == kvp.Value.Name)
                {
                    return kvp.Value;
                }
            }
            throw new Exception(name + " not found");
        }

        internal static ComponentTemplateSD TryGetTemplateSD(Game game, string name)
        {
            foreach (var kvp in game.StaticData.ComponentTemplates)
            {
                if (name == kvp.Value.Name)
                {
                    return kvp.Value;
                }
            }
            throw new Exception(name + " not found");
        }
    }
}