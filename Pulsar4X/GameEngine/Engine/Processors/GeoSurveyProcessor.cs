using System;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Engine;

public class GeoSurveyProcessor : IInstanceProcessor
{
    public Entity Fleet { get; internal set; }
    public Entity Target { get; internal set; }

    public GeoSurveyProcessor() {}

    public GeoSurveyProcessor(Entity fleet, Entity target)
    {
        Fleet = fleet;
        Target = target;
    }

    internal override void ProcessEntity(Entity entity, DateTime atDateTime)
    {
        // TODO: need to only get the survey points from ships that are at the survey location
        uint totalSurveyPoints = GetSurveyPoints(Fleet);

        if(Target.TryGetDatablob<GeoSurveyableDB>(out var geoSurveyableDB))
        {
            if(!geoSurveyableDB.GeoSurveyStatus.ContainsKey(Fleet.FactionOwnerID))
                geoSurveyableDB.GeoSurveyStatus[Fleet.FactionOwnerID] = geoSurveyableDB.PointsRequired;

            if(totalSurveyPoints > geoSurveyableDB.GeoSurveyStatus[Fleet.FactionOwnerID])
            {
                geoSurveyableDB.GeoSurveyStatus[Fleet.FactionOwnerID] = 0;
            }
            else
            {
                geoSurveyableDB.GeoSurveyStatus[Fleet.FactionOwnerID] -= totalSurveyPoints;
            }
        }
    }

    private uint GetSurveyPoints(Entity entity)
    {
        uint totalSurveyPoints = 0;

        if(entity.TryGetDatablob<GeoSurveyAbilityDB>(out var geoSurveyAbilityDB))
        {
            totalSurveyPoints += geoSurveyAbilityDB.Speed;
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