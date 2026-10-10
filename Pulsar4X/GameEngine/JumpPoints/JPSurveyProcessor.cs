using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using Pulsar4X.Extensions;
using Pulsar4X.Fleets;
using Pulsar4X.Interfaces;
using Pulsar4X.Movement;
using Pulsar4X.People;

namespace Pulsar4X.JumpPoints;

public class JPSurveyProcessor : IHotloopProcessor
{
    public TimeSpan RunFrequency { get; } = TimeSpan.FromHours(1);
    public TimeSpan FirstRunOffset { get; } = TimeSpan.FromHours(1);
    public Type GetParameterType { get; } = typeof(JPSurveyDB);

    public JPSurveyProcessor() {}

    public void Init(Game game)
    {
    }

    public void ProcessEntity(Entity entity, int deltaSeconds)
    {
        if (entity.TryGetDataBlob<JPSurveyDB>(out var jpSurveyDB)
            && entity.TryGetDataBlob<JPSurveyAbilityDB>(out var jpSurveyAbilityDB)
            && entity.Manager.TryGetDataBlob<JPSurveyableDB>(jpSurveyDB.TargetId, out var jpSurveyableDB))
        {
            // Factions are lazily added to the surveys
            if(!jpSurveyableDB.SurveyPointsRemaining.ContainsKey(entity.FactionOwnerID))
                jpSurveyableDB.SurveyPointsRemaining[entity.FactionOwnerID] = jpSurveyableDB.PointsRequired;

            // Check if the survey has been completed (possibly some other entity completed the survey already
            if (jpSurveyableDB.SurveyPointsRemaining[entity.FactionOwnerID] == 0)
            {
                // If the survey is completed remove the JPSurveyDB and return
                entity.RemoveDataBlob<JPSurveyDB>();
                return;
            }

            // Make sure the surveyor is within distance of the target
            var distance =  MoveMath.GetDistanceBetween(entity, jpSurveyableDB.OwningEntity);
            if (distance < 100000) // FIXME: needs to be an attribute of the JPSurveyAbilityDB
            {
                uint rate = CommanderSkills.SurveyRate(jpSurveyAbilityDB.Speed, entity);
                if (rate >= jpSurveyableDB.SurveyPointsRemaining[entity.FactionOwnerID])
                {
                    RollToDiscoverJumpPoint(entity.StarSysDateTime, entity, jpSurveyableDB.OwningEntity);
                    MarkSurveyAsComplete(jpSurveyableDB, entity, entity.StarSysDateTime);
                }
                else
                {
                    jpSurveyableDB.SurveyPointsRemaining[entity.FactionOwnerID] -= rate;
                }
            }
        }
    }

    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        List<JPSurveyDB> surveyors = manager.GetAllDataBlobsOfType<JPSurveyDB>();

        foreach (var db in surveyors)
        {
            ProcessEntity(db.OwningEntity, deltaSeconds);
        }

        return surveyors.Count;
    }

    private void MarkSurveyAsComplete(JPSurveyableDB jpSurveyableDB, Entity surveyingEntity, DateTime atDateTime)
    {
        // Mark the survey as complete
        jpSurveyableDB.SurveyPointsRemaining[surveyingEntity.FactionOwnerID] = 0;

        // Hide the survey location from the faction that just completed the survey
        jpSurveyableDB.OwningEntity.Manager.HideNeutralEntityFromFaction(surveyingEntity.FactionOwnerID, jpSurveyableDB.OwningEntity.Id);

        EventManager.Instance.Publish(
            Event.Create(
                EventType.JumpPointSurveyCompleted,
                atDateTime,
                $"Survey of {jpSurveyableDB.OwningEntity.GetName(surveyingEntity.FactionOwnerID)} complete",
                surveyingEntity.FactionOwnerID,
                jpSurveyableDB.OwningEntity.Manager.ManagerID,
                jpSurveyableDB.OwningEntity.Id));
    }

    private void RollToDiscoverJumpPoint(DateTime atDateTime, Entity discoveringEntity, Entity discoveredEntity)
    {
        // Roll is see if a jump point is revealed
        var surveyLocationsRemaining = discoveringEntity.Manager.GetAllDataBlobsOfType<JPSurveyableDB>()
                                                        .Where(db => !db.IsSurveyComplete(discoveringEntity.FactionOwnerID))
                                                        .ToList();
        var jpRemaining = discoveringEntity.Manager.GetAllDataBlobsOfType<JumpPointDB>()
                                           .Where(db => !db.IsDiscovered.Contains(discoveringEntity.FactionOwnerID))
                                           .ToList();

        var chance = (double)jpRemaining.Count / (double)surveyLocationsRemaining.Count;
        var roll = discoveredEntity.Manager.RNGNextDouble();

        if(chance >= roll)
        {
            var jp = jpRemaining.First(); // TODO: pick randomly from remaining
            if (jp.OwningEntity != null && discoveringEntity.Manager?.Game is { } game)
                JumpPointReveal.Grant(game, discoveringEntity.FactionOwnerID, jp.OwningEntity, atDateTime);

            // If this was the last jump point, hide the rest of the survey locations
            if(jpRemaining.Count == 1)
            {
                foreach(var surveyLocation in surveyLocationsRemaining)
                {
                    if(surveyLocation.OwningEntity.Id == discoveredEntity.Id) continue;

                    surveyLocation.OwningEntity.Manager.HideNeutralEntityFromFaction(discoveringEntity.FactionOwnerID, surveyLocation.OwningEntity.Id);
                }
            }
        }
    }

}