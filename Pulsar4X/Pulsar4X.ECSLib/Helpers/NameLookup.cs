using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class NameLookup
    {

        internal static bool TryGetFirstEntityWithName(EntityManager manager, string name, out Entity entity)
        {
            List<Entity> list = manager.GetAllEntitiesWithDataBlob<NameDB>();
            return TryGetFirstEntityWithName(list, name, out entity);
        }
        internal static bool TryGetFirstEntityWithName(List<Entity> entitiesWithNameDB, string name, out Entity entity)
        { 
            foreach (var item in entitiesWithNameDB)
            {
                NameDB namedb = item.GetDataBlob<NameDB>();
                if (namedb.DefaultName == name)
                {
                    entity = item;
                    return true;
                }
            }
            entity = null;
            return false;
        }

        internal static Entity GetFirstEntityWithName(EntityManager manager, string name)
        {          
            List<Entity> list = manager.GetAllEntitiesWithDataBlob<NameDB>();
            return GetFirstEntityWithName(list, name);
        }

        internal static Entity GetFirstEntityWithName(List<Entity> entitiesWithNameDB, string name)
        {
            foreach (var item in entitiesWithNameDB)
            {
                NameDB namedb = item.GetDataBlob<NameDB>();
                if (namedb.DefaultName == name)
                {
                    return item;
                }
            }
            throw new Exception(name + " not found");
        }


        internal static MineralSD GetMineralSD(Game game, string name)
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
        
        internal static ProcessedMaterialSD GetMaterialSD(Game game, string name)
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

        internal static ComponentTemplateSD GetTemplateSD(Game game, string name)
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