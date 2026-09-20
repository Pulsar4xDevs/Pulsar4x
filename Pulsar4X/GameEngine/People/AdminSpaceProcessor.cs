using System;
using System.Collections.Generic;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace GameEngine.People;

public class AdminSpaceProcessor : IInstanceProcessor
{
    internal override void ProcessEntity(Entity entity, DateTime atDateTime)
    {
        if(entity.TryGetDataBlob<AdminSpaceDB>(out var adminSpaceDB))
        {
            CalcEntityAdminSpace(entity, adminSpaceDB);

            // Update colony hex map if this is a colony
            if (entity.HasDataBlob<ColonyInfoDB>())
            {
                ColonyHexMapProcessor.ForceUpdateColonyHexMap(entity);
            }
        }
    }

    /// <summary>
    /// Currently this resets the list, need to check if we want that, or keep exsisting.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="adminSpaceDB"></param>
    internal static void CalcEntityAdminSpace(Entity entity, AdminSpaceDB adminSpaceDB)
    {
        var seats = 0;
        if (entity.GetDataBlob<ComponentInstancesDB>().TryGetComponentsByAttribute<AdminSpaceAtb>(out var adminSpaces))
        {
            List<AdminSpaceAbilityState> commanderSeats = new List<AdminSpaceAbilityState>();
            adminSpaceDB.CommanderSeats = commanderSeats;
            foreach (var adminSpace in adminSpaces)
            {
                var attributes = adminSpace.GetAttributes();
                var atb = (AdminSpaceAtb)attributes[typeof(AdminSpaceAtb)];

                seats += atb.ConsoleSpace;

                var state = new AdminSpaceAbilityState(atb.AdminLevel, adminSpace.Name);
                commanderSeats.Add(state);
            }
        }
    }
}