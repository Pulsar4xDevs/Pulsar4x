using System;
using System.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.Events;
using Pulsar4X.Extensions;
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

            if(totalSurveyPoints >= jpSurveyableDB.SurveyPointsRemaining[Fleet.FactionOwnerID])
            {
                // Survey is complete
                jpSurveyableDB.SurveyPointsRemaining[Fleet.FactionOwnerID] = 0;

                EventManager.Instance.Publish(
                    Event.Create(
                        EventType.JumpPointSurveyCompleted,
                        atDateTime,
                        $"Survey of {Target.GetName(Fleet.FactionOwnerID)} complete",
                        Fleet.FactionOwnerID,
                        Target.Manager.ManagerGuid,
                        Target.Id));

                // Roll is see if a jump point is revealed
                var surveyLocationsRemaining = Fleet.Manager.GetAllDataBlobsOfType<JPSurveyableDB>()
                                                .Where(db => !db.IsSurveyComplete(Fleet.FactionOwnerID))
                                                .ToList();
                var jpRemaining = Fleet.Manager.GetAllDataBlobsOfType<JumpPointDB>()
                                                .Where(db => !db.IsDiscovered.Contains(Fleet.FactionOwnerID))
                                                .ToList();
                
                if(surveyLocationsRemaining.Count < 1 && jpRemaining.Count > 0)
                {
                    // TODO: don't roll just discover the last JP
                }

                var chance = (double)jpRemaining.Count / (double)surveyLocationsRemaining.Count;
                var roll = Target.Manager.Game.RNG.NextDouble();

                if(chance >= roll)
                {
                    var jp = jpRemaining.First(); // TODO: pick randomly from remaining
                    jp.IsDiscovered.Add(Fleet.FactionOwnerID);

                    EventManager.Instance.Publish(
                        Event.Create(
                            EventType.JumpPointDetected,
                            atDateTime,
                            $"Jump Point discovered",
                            Fleet.FactionOwnerID,
                            jp.OwningEntity.Manager.ManagerGuid,
                            jp.OwningEntity.Id));
                }
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