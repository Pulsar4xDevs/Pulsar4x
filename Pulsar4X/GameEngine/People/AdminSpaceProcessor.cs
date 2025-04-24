using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.ECSLib;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace GameEngine.People;

public class AdminSpaceProcessor : IInstanceProcessor
{
    internal override void ProcessEntity(Entity entity, DateTime atDateTime)
    {
        if(entity.TryGetDataBlob<AdminSpaceDB>(out var adminSpaceDB))
            CalcEntityAdminSpace(entity, adminSpaceDB);
    }

    internal static void CalcEntityAdminSpace(Entity entity, AdminSpaceDB adminSpaceDB)
    {
        var level = 0;
        var seats = 0;
        if (entity.GetDataBlob<ComponentInstancesDB>().TryGetComponentsByAttribute<AdminSpaceAtb>(out var adminSpaces))
        {
            List<AdminSpaceAbilityState> commanderSeats = new List<AdminSpaceAbilityState>();
            adminSpaceDB.CommanderSeats = commanderSeats;
            foreach (var adminSpace in adminSpaces)
            {
                var state = adminSpace.GetAbilityState<AdminSpaceAbilityState>();
                
                var attributes = adminSpace.GetAttributes();
                var atb = (AdminSpaceAtb)attributes[typeof(AdminSpaceAtb)];
                if(level > (int)atb.AdminLevel)
                {level = (int)atb.AdminLevel;}
                seats += atb.ConsoleSpace;

            }
            
            
        }
    }
}