using System;
using System.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.Events;
using Pulsar4X.Extensions;
using Pulsar4X.Interfaces;
using Pulsar4X.Messaging;

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
                RollToDiscoverJumpPoint(atDateTime);
                MarkSurveyAsComplete(jpSurveyableDB, atDateTime);
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

    private void MarkSurveyAsComplete(JPSurveyableDB jpSurveyableDB, DateTime atDateTime)
    {
        // Mark the survey as complete
        jpSurveyableDB.SurveyPointsRemaining[Fleet.FactionOwnerID] = 0;

        // Hide the survey location from the faction that just completed the survey
        jpSurveyableDB.OwningEntity.Manager.HideNeutralEntityFromFaction(Fleet.FactionOwnerID, jpSurveyableDB.OwningEntity.Id);

        EventManager.Instance.Publish(
            Event.Create(
                EventType.JumpPointSurveyCompleted,
                atDateTime,
                $"Survey of {Target.GetName(Fleet.FactionOwnerID)} complete",
                Fleet.FactionOwnerID,
                Target.Manager.ManagerGuid,
                Target.Id));
    }

    private void RollToDiscoverJumpPoint(DateTime atDateTime)
    {
        // Roll is see if a jump point is revealed
        var surveyLocationsRemaining = Fleet.Manager.GetAllDataBlobsOfType<JPSurveyableDB>()
                                        .Where(db => !db.IsSurveyComplete(Fleet.FactionOwnerID))
                                        .ToList();
        var jpRemaining = Fleet.Manager.GetAllDataBlobsOfType<JumpPointDB>()
                            .Where(db => !db.IsDiscovered.Contains(Fleet.FactionOwnerID))
                            .ToList();
        
        var chance = (double)jpRemaining.Count / (double)surveyLocationsRemaining.Count;
        var roll = Target.Manager.Game.RNG.NextDouble();

        if(chance >= roll)
        {
            var jp = jpRemaining.First(); // TODO: pick randomly from remaining
            jp.IsDiscovered.Add(Fleet.FactionOwnerID);

            // Show the jump point to the faction that just completed the survey
            jp.OwningEntity.Manager.ShowNeutralEntityToFaction(Fleet.FactionOwnerID, jp.OwningEntity.Id);

            EventManager.Instance.Publish(
                Event.Create(
                    EventType.JumpPointDetected,
                    atDateTime,
                    $"Jump Point discovered",
                    Fleet.FactionOwnerID,
                    jp.OwningEntity.Manager.ManagerGuid,
                    jp.OwningEntity.Id));

            // If this was the last jump point, hide the rest of the survey locations
            if(jpRemaining.Count == 1)
            {
                foreach(var surveyLocation in surveyLocationsRemaining)
                {
                    if(surveyLocation.OwningEntity.Id == Target.Id) continue;

                    surveyLocation.OwningEntity.Manager.HideNeutralEntityFromFaction(Fleet.FactionOwnerID, surveyLocation.OwningEntity.Id);
                }
            }

            RevealOtherSide(jp, atDateTime);
        }
    }

    private void RevealOtherSide(JumpPointDB jumpPointDB, DateTime atDateTime)
    {
        if(Fleet.Manager.TryGetGlobalEntityById(jumpPointDB.DestinationId, out var destinationEntity))
        {
            var factionInfoDB = Fleet.Manager.Game.Factions[Fleet.FactionOwnerID].GetDataBlob<FactionInfoDB>();

            // Check to see if the system has been discovered yet
            if(!factionInfoDB.KnownSystems.Contains(destinationEntity.Manager.ManagerGuid))
            {
                factionInfoDB.KnownSystems.Add(destinationEntity.Manager.ManagerGuid);

                EventManager.Instance.Publish(
                    Event.Create(
                        EventType.NewSystemDiscovered,
                        atDateTime,
                        $"New system discovered",
                        Fleet.FactionOwnerID,
                        destinationEntity.Manager.ManagerGuid,
                        destinationEntity.Id));

                MessagePublisher.Instance.Publish(
                    Message.Create(
                        MessageTypes.StarSystemRevealed,
                        destinationEntity.Id,
                        destinationEntity.Manager.ManagerGuid,
                        Fleet.FactionOwnerID));
            }

            // Reveal the JP
            if(destinationEntity.TryGetDatablob<JumpPointDB>(out var destinationDB))
            {
                destinationDB.IsDiscovered.Add(Fleet.FactionOwnerID);
                destinationEntity.Manager.ShowNeutralEntityToFaction(Fleet.FactionOwnerID, destinationEntity.Id);

                EventManager.Instance.Publish(
                    Event.Create(
                        EventType.JumpPointDetected,
                        atDateTime,
                        $"Jump Point discovered",
                        Fleet.FactionOwnerID,
                        destinationEntity.Manager.ManagerGuid,
                        destinationEntity.Id));
            }
            
        }
    }
}