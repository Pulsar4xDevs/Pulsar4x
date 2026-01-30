using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;

namespace Pulsar4X.JumpPoints
{
    public static class JPFactory
    {
        public static Entity CreateJumpPoint(StarSystemFactory ssf, StarSystem system, Entity gravityRoot)
        {
            var primaryStarInfoDB = system.GetFirstEntityWithDataBlob<StarInfoDB>().GetDataBlob<OrbitDB>().Root.GetDataBlob<StarInfoDB>();

            var jpPositionLimits = ssf.GalaxyGen.Settings.OrbitalDistanceByStarSpectralType[primaryStarInfoDB.SpectralType];

            double X = GeneralMath.Lerp(jpPositionLimits, system.RNGNextDouble());
            double Y = GeneralMath.Lerp(jpPositionLimits, system.RNGNextDouble());

            // Randomly flip the position sign to allow negative values.
            if (system.RNGNext(0, 100) < 50)
            {
                X = -X;
            }
            if (system.RNGNext(0, 100) < 50)
            {
                Y = -Y;
            }

            var x_km = Distance.AuToKm(X);
            var y_km = Distance.AuToKm(Y);

            NameDB jpNameDB = new NameDB("Jump Point");
            PositionDB jpPositionDB = new PositionDB(x_km * 1000, y_km * 1000, 0, gravityRoot);
            jpPositionDB.MoveType = PositionDB.MoveTypes.None;
            JumpPointDB jpTransitableDB = new JumpPointDB
            {
                IsStabilized = system.Game.Settings.AllJumpPointsStabilized ?? false
            };

            if (!jpTransitableDB.IsStabilized)
            {
                // TODO: Introduce a random chance to stablize jumppoints.
            }

            var dataBlobs = new List<BaseDataBlob> { jpNameDB, jpTransitableDB, jpPositionDB};

            Entity jumpPoint = Entity.Create();
            jumpPoint.FactionOwnerID = Game.NeutralFactionId;
            system.AddEntity(jumpPoint, dataBlobs);
            return jumpPoint;
        }

        /// <summary>
        /// Gets the number of jumppoints that should generated for a system.
        /// Based on Aurora 7.0 mechanics as described here:
        /// http://aurora2.pentarch.org/index.php?topic=7255.msg80028#msg80028
        /// </summary>
        public static int GetNumJPForSystem(StarSystem system)
        {
            Entity primaryStar = system.GetFirstEntityWithDataBlob<StarInfoDB>().GetDataBlob<OrbitDB>().Root;
            var starMVDB = primaryStar.GetDataBlob<MassVolumeDB>();

            int numJumpPoints = 0;
            int baseJPChance = 90;

            double jpChance;
            int random;
            do
            {
                numJumpPoints++;

                jpChance = baseJPChance + (starMVDB.MassDry / UniversalConstants.Units.SolarMassInKG);

                if (jpChance > 90)
                {
                    jpChance = 90;
                }

                if (baseJPChance == 90)
                {
                    baseJPChance = 60;
                }
                else if (baseJPChance == 60)
                {
                    baseJPChance = 30;
                }

                random = system.RNGNext(0, 100);
            } while (jpChance > random);

            return numJumpPoints;
        }

        /// <summary>
        /// Generates jump points for this system.
        /// </summary>
        public static void GenerateJumpPoints(StarSystemFactory ssf, StarSystem system, Entity gravityRoot)
        {
            int numJumpPoints = GetNumJPForSystem(system);

            // Cap JumpPoints to maxSystems - 1 to ensure we don't create more than can be linked
            // In a 2-system game, each system should have at most 1 JP
            int maxSystems = system.Game.Settings.MaxSystems;
            if (maxSystems > 1 && numJumpPoints > maxSystems - 1)
            {
                numJumpPoints = maxSystems - 1;
            }

            while (numJumpPoints > 0)
            {
                numJumpPoints--;

                CreateJumpPoint(ssf, system, gravityRoot);
            }
        }


        private static void CreateConnection(Game game, Entity jumpPoint)
        {
            var jpTransitableDB = jumpPoint.GetDataBlob<JumpPointDB>();
            var jpPositionDB = jumpPoint.GetDataBlob<PositionDB>();

            // FIXME: commented out because it wasn't implemented
            //StarSystem system = (StarSystem)game.Systems[jpPositionDB.SystemGuid];
            //int systemIndex = system.SystemIndex;
        }

        private static void LinkJumpPoints(Entity JP1, Entity JP2)
        {
            var jp1TransitableDB = JP1.GetDataBlob<JumpPointDB>();
            var jp2TransitableDB = JP2.GetDataBlob<JumpPointDB>();

            jp1TransitableDB.DestinationId = JP2.Id;
            jp2TransitableDB.DestinationId = JP1.Id;
        }

        /// <summary>
        /// Links all unlinked JumpPoints across all systems in the game.
        /// JumpPoints are paired between different systems to create inter-system connections.
        /// </summary>
        public static void LinkAllJumpPoints(Game game)
        {
            // Collect all unlinked JumpPoints grouped by system
            var unlinkedBySystem = new Dictionary<string, List<Entity>>();

            foreach (var system in game.Systems)
            {
                var jumpPoints = system.GetAllEntitiesWithDataBlob<JumpPointDB>()
                    .Where(jp => jp.GetDataBlob<JumpPointDB>().DestinationId <= 0)
                    .ToList();

                if (jumpPoints.Count > 0)
                {
                    unlinkedBySystem[system.ID] = jumpPoints;
                }
            }

            // If we have fewer than 2 systems with JumpPoints, nothing to link
            if (unlinkedBySystem.Count < 2)
                return;

            var systemIds = unlinkedBySystem.Keys.ToList();
            var random = new Random(game.Settings.MasterSeed);

            // Keep linking JumpPoints until we can't make any more pairs
            bool madeLink;
            do
            {
                madeLink = false;

                // Find two systems that both have unlinked JumpPoints
                var systemsWithJPs = systemIds.Where(id => unlinkedBySystem[id].Count > 0).ToList();

                if (systemsWithJPs.Count < 2)
                    break;

                // Pick two different systems randomly
                int idx1 = random.Next(systemsWithJPs.Count);
                string system1Id = systemsWithJPs[idx1];
                systemsWithJPs.RemoveAt(idx1);

                int idx2 = random.Next(systemsWithJPs.Count);
                string system2Id = systemsWithJPs[idx2];

                // Get one JumpPoint from each system
                var jp1List = unlinkedBySystem[system1Id];
                var jp2List = unlinkedBySystem[system2Id];

                var jp1 = jp1List[random.Next(jp1List.Count)];
                var jp2 = jp2List[random.Next(jp2List.Count)];

                // Link them
                LinkJumpPoints(jp1, jp2);

                // Remove from unlinked lists
                jp1List.Remove(jp1);
                jp2List.Remove(jp2);

                madeLink = true;
            } while (madeLink);
        }
    }
}
