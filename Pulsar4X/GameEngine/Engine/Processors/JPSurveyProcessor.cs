using System;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Engine;

public class JPSurveyProcessor : IInstanceProcessor
{
    public Entity Fleet { get; internal set; }
    public Entity Target { get; internal set; }

    public JPSurveyProcessor() {}

    public JPSurveyProcessor(Entity fleet, Entity target)
    {
        Fleet = fleet;
        Target = target;
    }

    internal override void ProcessEntity(Entity entity, DateTime atDateTime)
    {
        // TODO: need to only get the survey points from ships that are at the survey location
        uint totalSurveyPoints = GetSurveyPoints(Fleet);

        if(Target.TryGetDatablob<JPSurveyableDB>(out var jpSurveyableDB))
        {
            if(!jpSurveyableDB.SurveyPointsRemaining.ContainsKey(Fleet.FactionOwnerID))
                jpSurveyableDB.SurveyPointsRemaining[Fleet.FactionOwnerID] = jpSurveyableDB.PointsRequired;

            if(totalSurveyPoints > jpSurveyableDB.SurveyPointsRemaining[Fleet.FactionOwnerID])
            {
                jpSurveyableDB.SurveyPointsRemaining[Fleet.FactionOwnerID] = 0;
            }
            else
            {
                jpSurveyableDB.SurveyPointsRemaining[Fleet.FactionOwnerID] -= totalSurveyPoints;
            }
        }
    }

    private uint GetSurveyPoints(Entity entity)
    {
        uint totalSurveyPoints = 0;

        if(entity.TryGetDatablob<JPSurveyAbilityDB>(out var jpSurveyAbilityDB))
        {
            totalSurveyPoints += jpSurveyAbilityDB.Speed;
        }

        if(entity.TryGetDatablob<FleetDB>(out var fleetDB))
        {
            foreach(var child in fleetDB.Children)
            {
                totalSurveyPoints += GetSurveyPoints(child);
            }
        }

        return totalSurveyPoints;
    }
}