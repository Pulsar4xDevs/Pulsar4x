using System;
using Pulsar4X.Datablobs;
using Pulsar4X.Events;
using Pulsar4X.Extensions;
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

            if(totalSurveyPoints >= geoSurveyableDB.GeoSurveyStatus[Fleet.FactionOwnerID])
            {
                // Survey is complete
                geoSurveyableDB.GeoSurveyStatus[Fleet.FactionOwnerID] = 0;

                EventManager.Instance.Publish(
                    Event.Create(
                        EventType.GeoSurveyCompleted,
                        atDateTime,
                        $"Geo Survey of {Target.GetName(Fleet.FactionOwnerID)} complete",
                        Fleet.FactionOwnerID,
                        Target.Manager.ManagerID,
                        Target.Id));
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