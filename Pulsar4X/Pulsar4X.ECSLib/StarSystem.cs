using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    [JsonObject(MemberSerialization.OptIn)]
    public class StarSystem
    {
        public Game Game { get; private set; }

        [PublicAPI]
        [JsonProperty]
        public Guid Guid { get; private set; }

        [PublicAPI]
        [JsonProperty]
        public NameDB NameDB { get; private set; }

        [PublicAPI]
        [JsonProperty]
        public int EconLastTickRun { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        [Obsolete("This property will be made internal. Use StarSystem GetEntity functions if accessing from outside Pulsar4x.ECSLib")]
        public EntityManager SystemManager { get; private set; }

        [PublicAPI]
        [JsonProperty]
        public int Seed { get; private set; }
        internal Random RNG { get; private set; }

        [JsonConstructor]
        internal StarSystem()
        {
        }

        public StarSystem(Game game, string name, int seed)
        {
            Game = game;
            Guid = Guid.NewGuid();
            NameDB = new NameDB(name);
            EconLastTickRun = 0;
            SystemManager = new EntityManager(game);
            Seed = seed;
            RNG = new Random(seed);

            game.StarSystems.Add(Guid, this);
        }

        /// <summary>
        /// Function to get system bodies from this system.
        /// Uses authentication system.
        /// </summary>
        /// <param name="authToken"></param>
        /// <returns></returns>
        [PublicAPI]
        public List<Entity> GetSystemBodies(AuthenticationToken authToken)
        {
            Player player = Game.GetPlayerForToken(authToken);
            bool systemFound = false;

            foreach (KeyValuePair<Entity, AccessRole> keyValuePair in player.AccessRoles)
            {
                Entity faction = keyValuePair.Key;
                AccessRole accessRole = keyValuePair.Value;

                if ((accessRole & AccessRole.SystemKnowledge) == AccessRole.SystemKnowledge)
                {
                    var factionInfoDB = faction.GetDataBlob<FactionInfoDB>();
                    foreach (Guid knownSystem in factionInfoDB.KnownSystems)
                    {
                        if (knownSystem == Guid)
                        {
                            systemFound = true;
                            break;
                        }
                    }

                    if (systemFound)
                    {
                        break;
                    }
                }
            }

            if (!systemFound)
            {
                return new List<Entity>();
            }

            var dataBlobMask = EntityManager.BlankDataBlobMask();
            dataBlobMask[EntityManager.GetTypeIndex<SystemBodyDB>()] = true;
            dataBlobMask[EntityManager.GetTypeIndex<StarInfoDB>()] = true;
            dataBlobMask[EntityManager.GetTypeIndex<JPSurveyableDB>()] = true;

            return SystemManager.GetAllEntitiesWithDataBlobs(dataBlobMask);
        }

        public List<Entity> GetOwnedEntities(AuthenticationToken authToken, Entity faction)
        {
            Player player = Game.GetPlayerForToken(authToken);

            if (player == null || !player.AccessRoles.ContainsKey(faction))
            {
                return new List<Entity>();
            }

            List<Entity> entityList = SystemManager.GetAllEntitiesWithDataBlob<OwnedDB>();
            var factionEntities = new List<Entity>(entityList);

            foreach (Entity entity in entityList)
            {
                if (entity.GetDataBlob<OwnedDB>().Faction == faction)
                {
                    factionEntities.Remove(entity);
                }
            }

            AccessRole role = player.AccessRoles[faction];
            if ((role & AccessRole.UnitVision) != AccessRole.UnitVision)
            {
                List<Entity> shipList = factionEntities.GetEntititiesWithDataBlob<ShipInfoDB>();
                foreach (Entity shipEntity in shipList)
                {
                    entityList.Remove(shipEntity);
                }
            }

            if ((role & AccessRole.ColonyVision) != AccessRole.ColonyVision)
            {
                List<Entity> colonyList = factionEntities.GetEntititiesWithDataBlob<ColonyInfoDB>();
                foreach (Entity colonyEntity in colonyList)
                {
                    entityList.Remove(colonyEntity);
                }
            }

            return factionEntities;
        }
    }
}
