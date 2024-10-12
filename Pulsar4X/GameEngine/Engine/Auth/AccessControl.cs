using System;
using System.Collections.Generic;
using Pulsar4X.DataStructures;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;

namespace Pulsar4X.Engine.Auth
{
    internal static class AccessControl
    {
        public static bool IsAuthorized(Game game, AuthenticationToken authToken, Entity entity)
        {
            // Initial player verification.
            Player? authorizedPlayer = game?.GetPlayerForToken(authToken);

            if (authorizedPlayer is null || entity == null || !entity.IsValid)
            {
                return false;
            }
            return authorizedPlayer == game.SpaceMaster || CheckAuthorization(authorizedPlayer, entity);
        }

        private static bool CheckAuthorization(Player authorizedPlayer, Entity entity)
        {
            // Get the datablob mask to avoid unnecessary validation checks on method calls.
            List<Type> blobTypes = entity.Manager.GetAllDataBlobTypesForEntity(entity.Id);

            if (IsSystemBodyAuthorized(authorizedPlayer, entity, blobTypes))
            {
                return true;
            }

            if (IsOwnedEntityAuthorized(authorizedPlayer, entity, blobTypes))
            {
                return true;
            }

            return false;
        }

        private static bool IsOwnedEntityAuthorized(Player authorizedPlayer, Entity entity, List<Type> dataBlobTypes)
        {
            //TODO: TotalyHacked because fuck knows how we're going to do this now.
            return true;
        }

        //private static bool IsOwnedEntityAuthorized(Player authorizedPlayer, Entity entity, ComparableBitArray entityMask)
        //{

        //    if (entityMask[EntityManager.GetTypeIndex<OwnedDB>()])
        //    {
        //        var entityOwnedDB = entity.GetDataBlob<OwnedDB>();
        //        var factions = new List<Entity>();

        //        if (entityMask[EntityManager.GetTypeIndex<SensorProfileDB>()])
        //        {
        //            factions = FactionsWithAccess(authorizedPlayer, AccessRole.SensorVision);
        //        }
        //        /*
        //        if (entityMask[EntityManager.GetTypeIndex<ColonyInfoDB>()])
        //        {
        //            // Check if entity is a SensorContact
        //            if (entityOwnedDB.OwnedByFaction == entityOwnedDB.ObjectOwner)
        //            {
        //                // Entity is not a SensorContact
        //                factions = FactionsWithAccess(authorizedPlayer, AccessRole.ColonyVision);
        //            }
        //        }
        //        else if (entityMask[EntityManager.GetTypeIndex<ShipInfoDB>()])
        //        {
        //            var entityShipInfoDB = entity.GetDataBlob<ShipInfoDB>();
        //            if (entityOwnedDB.OwnedByFaction == entityOwnedDB.ObjectOwner)
        //            {
        //                if (entityShipInfoDB.IsClassDefinition())
        //                {
        //                    factions = FactionsWithAccess(authorizedPlayer, AccessRole.Intelligence);
        //                }
        //                else
        //                {
        //                    // Entity is an actual ship
        //                    factions = FactionsWithAccess(authorizedPlayer, AccessRole.UnitVision);
        //                }
        //            }
        //        }
        //        else if (entityMask[EntityManager.GetTypeIndex<FactionInfoDB>()])
        //        {
        //            if (entityOwnedDB.OwnedByFaction == entityOwnedDB.ObjectOwner)
        //            {
        //                factions = FactionsWithAccess(authorizedPlayer, AccessRole.FullAccess);
        //            }
        //        } */

        //        foreach (Entity faction in factions)
        //        {
        //            if (faction == entityOwnedDB.OwnedByFaction)
        //            {
        //                return true;
        //            }
        //        }
        //    }
        //    return false;
        //}

        private static bool IsSystemBodyAuthorized(Player authorizedPlayer, Entity entity, List<Type> dataBlobTypes)
        {
            if(dataBlobTypes.Contains(typeof(StarInfoDB)) ||
                dataBlobTypes.Contains(typeof(SystemBodyInfoDB)) ||
                dataBlobTypes.Contains(typeof(JPSurveyableDB)) ||
                dataBlobTypes.Contains(typeof(JumpPointDB)))
            {
                // Entity systemBody
                var entityPositionDB = entity.GetDataBlob<PositionDB>();

                List<int> factions = FactionsWithAccess(authorizedPlayer, AccessRole.SystemKnowledge);
                foreach (int factionId in factions)
                {
                    var faction = entity.Manager.Game.Factions[factionId];
                    var factionInfoDB = faction.GetDataBlob<FactionInfoDB>();
                    foreach (var knownSystem in factionInfoDB.KnownSystems)
                    {
                        if (knownSystem == entity.Manager.ManagerID)
                        {
                            if (!dataBlobTypes.Contains(typeof(JumpPointDB)))
                            {
                                return true;
                            }

                            if (factionInfoDB.KnownJumpPoints.ContainsKey(knownSystem))
                            {
                                List<Entity> knownJumpPoints = factionInfoDB.KnownJumpPoints[knownSystem];
                                foreach (Entity knownJumpPoint in knownJumpPoints)
                                {
                                    if (knownJumpPoint == entity)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private static List<int> FactionsWithAccess(Player authorizedPlayer, AccessRole accessRole)
        {
            var factions = new List<int>();
            foreach (KeyValuePair<int, AccessRole> keyValuePair in authorizedPlayer.AccessRoles)
            {
                int factionId = keyValuePair.Key;
                AccessRole access = keyValuePair.Value;

                if (access.HasFlag(accessRole))
                {
                    factions.Add(factionId);
                }
            }
            return factions;
        }
    }
}
