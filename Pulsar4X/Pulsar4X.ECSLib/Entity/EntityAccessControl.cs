using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    internal static class EntityAccessControl
    {
        public static bool IsAuthorized(Game game, AuthenticationToken authToken, Entity entity)
        {
            // Initial player verification.
            Player authorizedPlayer = game?.GetPlayerForToken(authToken);

            if (authorizedPlayer == null || entity == null || !entity.IsValid)
            {
                return false;
            }
            return authorizedPlayer == game.SpaceMaster || CheckAuthorization(authorizedPlayer, entity);
        }

        private static bool CheckAuthorization(Player authorizedPlayer, Entity entity)
        {
            // Get the datablob mask to avoid unnecessary validation checks on method calls.
            ComparableBitArray entityMask = entity.DataBlobMask;

            if (IsSystemBodyAuthorized(authorizedPlayer, entity, entityMask))
            {
                return true;
            }

            if (IsOwnedEntityAuthorized(authorizedPlayer, entity, entityMask))
            {
                return true;
            }

            return false;
        }

        private static bool IsOwnedEntityAuthorized(Player authorizedPlayer, Entity entity, ComparableBitArray entityMask)
        {
            if (entityMask[EntityManager.GetTypeIndex<OwnedDB>()])
            {
                var entityOwnedDB = entity.GetDataBlob<OwnedDB>();
                var factions = new List<Entity>();

                if (entityMask[EntityManager.GetTypeIndex<SensorProfileDB>()])
                {
                    factions = FactionsWithAccess(authorizedPlayer, AccessRole.SensorVision);
                }
                if (entityMask[EntityManager.GetTypeIndex<ColonyInfoDB>()])
                {
                    // Check if entity is a SensorContact
                    if (entityOwnedDB.EntityOwner == entityOwnedDB.ObjectOwner)
                    {
                        // Entity is not a SensorContact
                        factions = FactionsWithAccess(authorizedPlayer, AccessRole.ColonyVision);
                    }
                }
                else if (entityMask[EntityManager.GetTypeIndex<ShipInfoDB>()])
                {
                    var entityShipInfoDB = entity.GetDataBlob<ShipInfoDB>();
                    if (entityOwnedDB.EntityOwner == entityOwnedDB.ObjectOwner)
                    {
                        if (entityShipInfoDB.IsClassDefinition())
                        {
                            factions = FactionsWithAccess(authorizedPlayer, AccessRole.Intelligence);
                        }
                        else
                        {
                            // Entity is an actual ship
                            factions = FactionsWithAccess(authorizedPlayer, AccessRole.UnitVision);
                        }
                    }
                }
                else if (entityMask[EntityManager.GetTypeIndex<FactionInfoDB>()])
                {
                    if (entityOwnedDB.EntityOwner == entityOwnedDB.ObjectOwner)
                    {
                        factions = FactionsWithAccess(authorizedPlayer, AccessRole.FullAccess);
                    }
                }

                foreach (Entity faction in factions)
                {
                    if (faction == entityOwnedDB.EntityOwner)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool IsSystemBodyAuthorized(Player authorizedPlayer, Entity entity, ComparableBitArray entityMask)
        {
            if (entityMask[EntityManager.GetTypeIndex<StarInfoDB>()] ||
                entityMask[EntityManager.GetTypeIndex<SystemBodyInfoDB>()] ||
                entityMask[EntityManager.GetTypeIndex<JPSurveyableDB>()] ||
                entityMask[EntityManager.GetTypeIndex<TransitableDB>()])
            {
                // Entity systemBody
                var entityPositionDB = entity.GetDataBlob<PositionDB>();

                List<Entity> factions = FactionsWithAccess(authorizedPlayer, AccessRole.SystemKnowledge);
                foreach (Entity faction in factions)
                {
                    var factionInfoDB = faction.GetDataBlob<FactionInfoDB>();
                    foreach (Guid knownSystem in factionInfoDB.KnownSystems)
                    {
                        if (knownSystem == entityPositionDB.SystemGuid)
                        {
                            if (!entityMask[EntityManager.GetTypeIndex<TransitableDB>()])
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

        private static List<Entity> FactionsWithAccess(Player authorizedPlayer, AccessRole accessRole)
        {
            var factions = new List<Entity>();
            foreach (KeyValuePair<Entity, AccessRole> keyValuePair in authorizedPlayer.AccessRoles)
            {
                Entity faction = keyValuePair.Key;
                AccessRole access = keyValuePair.Value;

                if (access.HasFlag(accessRole))
                {
                    factions.Add(faction);
                }
            }
            return factions;
        } 
    }
}
