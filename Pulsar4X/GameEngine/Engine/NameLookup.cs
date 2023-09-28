using System;
using System.Linq;
using System.Collections.Generic;
using Pulsar4X.Blueprints;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine.Industry;

namespace Pulsar4X.Engine
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
    }
}