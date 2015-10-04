using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.Remoting.Messaging;

namespace Pulsar4X.ECSLib
{
    public class SystemBodyFactory
    {
        private GalaxyFactory _galaxyGen;
        private DateTime _currDateTime;

        public SystemBodyFactory(GalaxyFactory galaxyGen)
        {
            _galaxyGen = galaxyGen;
            _currDateTime = DateTime.Now;
        }

        /// <summary>
        /// Creates an uninitialized body in the specified system.
        /// </summary>
        public static ProtoEntity CreateBaseBody()
        {
            var position = new PositionDB(0, 0, 0);
            var massVolume = new MassVolumeDB();
            var planetInfo = new SystemBodyDB();
            var name = new NameDB();
            var orbit = new OrbitDB();
            var atmo = new AtmosphereDB();
            var ruins = new RuinsDB();

            var planetDBs = new List<BaseDataBlob>
            {
                position,
                massVolume,
                planetInfo,
                name,
                orbit,
                atmo,
                ruins
            };
            ProtoEntity newPlanet = ProtoEntity.Create(planetDBs);

            return newPlanet;
        }

        /// <summary>
        /// Calculates the number of bodies this star will have.
        /// </summary>
        private int CalcNumBodiesForStar(StarSystem system, MassVolumeDB starMassInfo, StarInfoDB starInfo)
        {
            if (system.RNG.NextDouble() > _galaxyGen.Settings.PlanetGenerationChance)
            {
                // Star will not have planets.
                return 0;
            }

            // Mass Multiplier
            double starMassRatio = GMath.GetPercentage(starMassInfo.Mass, _galaxyGen.Settings.StarMassBySpectralType[starInfo.SpectralType]);

            // Star type Multiplier.
            double starSpectralTypeRatio = _galaxyGen.Settings.StarSpectralTypePlanetGenerationRatio[starInfo.SpectralType];

            // Random value.
            double randomMultiplier = system.RNG.NextDouble();

            double percentOfMax = GMath.Clamp(starMassRatio * starSpectralTypeRatio * randomMultiplier, 0, 1);

            return (int)Math.Round(percentOfMax * _galaxyGen.Settings.MaxNoOfPlanets);
        }

        /// <summary>
        /// Generate all bodies for the specified star.
        /// </summary>
        internal void GenerateSystemBodiesForStar(StaticDataStore staticData, StarSystem system, Entity star, DateTime currentDateTime)
        {
            // Get required info from the star.
            MassVolumeDB starMassInfo = star.GetDataBlob<MassVolumeDB>();
            StarInfoDB starInfo = star.GetDataBlob<StarInfoDB>();

            // Calculate number of system bodies to generate.
            int numberOfBodies = CalcNumBodiesForStar(system, starMassInfo, starInfo);

            if (numberOfBodies == 0)
                return;

            // Now calculate the "Bands."
            MinMaxStruct innerZone;
            MinMaxStruct habitableZone;
            MinMaxStruct outerZone;
            bool skipHabitableZone = false;
            if (starInfo.MinHabitableRadius > _galaxyGen.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Max ||
                starInfo.MaxHabitableRadius < _galaxyGen.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Min)
            {
                // Habitable zone either too close or too far from star.
                // Only generating inner and outer zones.
                skipHabitableZone = true;
                innerZone = new MinMaxStruct(_galaxyGen.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Min, _galaxyGen.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Max * 0.5);
                habitableZone = new MinMaxStruct(starInfo.MinHabitableRadius, starInfo.MaxHabitableRadius); // Still need this for later.
                outerZone = new MinMaxStruct(_galaxyGen.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Max * 0.5, _galaxyGen.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Max);
            }
            else
            {
                innerZone = new MinMaxStruct(_galaxyGen.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Min, starInfo.MinHabitableRadius);
                habitableZone = new MinMaxStruct(starInfo.MinHabitableRadius, starInfo.MaxHabitableRadius);
                outerZone = new MinMaxStruct(starInfo.MaxHabitableRadius, _galaxyGen.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Max);
            }

            // Now generate planet numbers.
            int numInnerZoneBodies = 0;
            int numHabitableZoneBodies = 0;
            int numOuterZoneBodies = 0;

            while (numberOfBodies > 0)
            {
                // Select a band to add a body to.
                SystemBand selectedBand = _galaxyGen.Settings.BandBodyWeight.Select(system.RNG.NextDouble());
                // Add a body to that band.
                switch (selectedBand)
                {
                    case SystemBand.InnerBand:
                        numInnerZoneBodies++;
                        numberOfBodies--;
                        break;
                    case SystemBand.HabitableBand:
                        if (skipHabitableZone)
                            break;
                        numHabitableZoneBodies++;
                        numberOfBodies--;
                        break;
                    case SystemBand.OuterBand:
                        numOuterZoneBodies++;
                        numberOfBodies--;
                        break;
                }
            }

            // Generate the bodies in each band.
            var systemBodies = new List<ProtoEntity>(numberOfBodies);

            systemBodies.AddRange(GenerateBodiesForBand(system, star, SystemBand.HabitableBand, habitableZone, numHabitableZoneBodies, systemBodies, currentDateTime));
            systemBodies.AddRange(GenerateBodiesForBand(system, star, SystemBand.InnerBand, innerZone, numInnerZoneBodies, systemBodies, currentDateTime));
            systemBodies.AddRange(GenerateBodiesForBand(system, star, SystemBand.OuterBand, outerZone, numOuterZoneBodies, systemBodies, currentDateTime));

            // Finalize all bodies that were actually added to the star.
            int bodyCount = 1;
            foreach (ProtoEntity protoBody in systemBodies)
            {
                Entity body = Entity.Create(system.SystemManager, protoBody);
                FinalizeBodies(staticData, system, body, bodyCount, currentDateTime);
                bodyCount++;
            }

            // Finally, comets!
            GenerateComets(staticData, system, star, currentDateTime);
        }

        /// <summary>
        /// Generates a random number of comets for a given star. The number of generated will 
        /// be at least GalaxyGen.MiniumCometsPerSystem and never more then GalaxyGen.MaxNoOfComets.
        /// </summary>
        private void GenerateComets(StaticDataStore staticData, StarSystem system, Entity star, DateTime currentDateTime)
        {
            // first lets get a random number between our minium nad maximum number of comets:
            int min = _galaxyGen.Settings.MiniumCometsPerSystem;
            if (min > _galaxyGen.Settings.MaxNoOfComets)
                min = _galaxyGen.Settings.MaxNoOfComets;

            int noOfComets = system.RNG.Next(min, _galaxyGen.Settings.MaxNoOfComets + 1);

            // now lets create the comets:
            for (int i = 0; i < noOfComets; ++i)
            {
                NameDB starName = star.GetDataBlob<NameDB>();

                ProtoEntity newCometProto = CreateBaseBody();
                NameDB cometName = newCometProto.GetDataBlob<NameDB>();
                cometName.SetName(Entity.InvalidEntity, starName.DefaultName + " - Comet " + (i + 1));

                SystemBodyDB cometBodyDB = newCometProto.GetDataBlob<SystemBodyDB>();
                cometBodyDB.Type = BodyType.Comet;

                MassVolumeDB cometMVDB = MassVolumeDB.NewFromMassAndDensity(
                    GMath.SelectFromRange(_galaxyGen.Settings.SystemBodyMassByType[BodyType.Comet], system.RNG.NextDouble()),
                    GMath.SelectFromRange(_galaxyGen.Settings.SystemBodyDensityByType[BodyType.Comet], system.RNG.NextDouble()));
                newCometProto.SetDataBlob(cometMVDB, EntityManager.GetTypeIndex<MassVolumeDB>());

                GenerateCometOrbit(system, star, newCometProto, currentDateTime);

                FinalizeSystemBodyDB(staticData, system, newCometProto);

                Entity.Create(system.SystemManager, newCometProto);
            }
        }

        /// <summary>
        /// Generates a very random orbit for comets. Doesn't care about other bodies.
        /// </summary>
        private void GenerateCometOrbit(StarSystem system, Entity star, ProtoEntity comet, DateTime currentDateTime)
        {
            StarInfoDB starInfo = star.GetDataBlob<StarInfoDB>();
            MassVolumeDB starMVDB = star.GetDataBlob<MassVolumeDB>();
            MassVolumeDB cometMVDB = comet.GetDataBlob<MassVolumeDB>();

            double semiMajorAxis = GMath.SelectFromRange(_galaxyGen.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType], system.RNG.NextDouble());
            double eccentricity = GMath.SelectFromRange(_galaxyGen.Settings.BodyEccentricityByType[BodyType.Comet], system.RNG.NextDouble());
            double inclination = system.RNG.NextDouble() * _galaxyGen.Settings.MaxBodyInclination;
            double longitudeOfAscendingNode = system.RNG.NextDouble() * 360;
            double argumentOfPeriapsis = system.RNG.NextDouble() * 360;
            double meanAnomaly = system.RNG.NextDouble() * 360;

            comet.SetDataBlob(new OrbitDB(star, starMVDB.Mass, cometMVDB.Mass, semiMajorAxis, eccentricity, inclination, longitudeOfAscendingNode,
                                            argumentOfPeriapsis, meanAnomaly, currentDateTime));
        }

        /// <summary>
        /// Generates the bodies for the specified SystemBand.
        /// This allows us to tweak how many habitable/inner/outer bodies there are.
        /// </summary>
        /// <param name="system">System we're working with.</param>
        /// <param name="star">Star we're generating for.</param>
        /// <param name="systemBand">Enum specifying which band we're working in.</param>
        /// <param name="bandLimits">MinMax structure representing the distance this band represents.</param>
        /// <param name="numBodies">Number of bodies to try to generate in this band.</param>
        /// <param name="systemBodies">List of systemBodies already present. Required for Orbit generation.</param>
        private List<ProtoEntity> GenerateBodiesForBand(StarSystem system, Entity star, SystemBand systemBand, MinMaxStruct bandLimits, int numBodies, List<ProtoEntity> systemBodies, DateTime currentDateTime)
        {
            List<ProtoEntity> bodies = new List<ProtoEntity>(numBodies);

            int numAsteroidBelts = 0;

            // Generate basic bodies with types and masses.
            while (numBodies > 0)
            {
                ProtoEntity newBody = CreateBaseBody();

                double massMultiplyer = 1; // Later we do some multiplication.
                SystemBodyDB newBodyBodyDB = newBody.GetDataBlob<SystemBodyDB>();

                newBodyBodyDB.Type = _galaxyGen.Settings.GetBandBodyTypeWeight(systemBand).Select(system.RNG.NextDouble());
                

                if (newBodyBodyDB.Type == BodyType.Asteroid)
                {
                    if (numAsteroidBelts == _galaxyGen.Settings.MaxNoOfAsteroidBelts)
                    {
                        // Max number of belts reach. Reroll until we've got... not an asteroid belt.
                        while (newBodyBodyDB.Type == BodyType.Asteroid)
                        {
                            newBodyBodyDB.Type = _galaxyGen.Settings.GetBandBodyTypeWeight(systemBand).Select(system.RNG.NextDouble());
                        }
                    }
                    else
                    {
                        // We calculate the entire mass of the asteroid belt here.
                        // Note, this "numOfAsteroids" is not the final number. When we 
                        // finalize this asteroid belt, we'll generate asteroids until we run out of mass.
                        double noOfAsteroids = system.RNG.NextDouble() * _galaxyGen.Settings.MaxNoOfAsteroidsPerBelt;
                        massMultiplyer = noOfAsteroids;
                    }
                }

                // generate Mass volume DB in full here, to avoid problems later:
                double density;
                if (newBodyBodyDB.Type == BodyType.Asteroid)
                {
                    // Mass multiplication here. This allows us to set the mass to the correct value for both asteroid belts and other bodies.
                    massMultiplyer *= GMath.SelectFromRange(_galaxyGen.Settings.SystemBodyMassByType[newBodyBodyDB.Type], system.RNG.NextDouble());  // cache final mass in massMultiplyer.
                    var minMaxDensity = _galaxyGen.Settings.SystemBodyDensityByType[newBodyBodyDB.Type];
                    density = (minMaxDensity.Min + minMaxDensity.Max) / 2.0;
                }
                else
                {
                    massMultiplyer *= GMath.SelectFromRange(_galaxyGen.Settings.SystemBodyMassByType[newBodyBodyDB.Type], Math.Pow(system.RNG.NextDouble(), 3)); // cache mass, alos cube random nuber to make smaller bodies more likly.
                    density = GMath.SelectFromRange(_galaxyGen.Settings.SystemBodyDensityByType[newBodyBodyDB.Type], system.RNG.NextDouble());
                }
                
                var mvDB = MassVolumeDB.NewFromMassAndDensity(massMultiplyer, density);
                newBody.SetDataBlob(mvDB);

                bodies.Add(newBody);
                numBodies--;
            }

            // Generate the orbits for the bodies.
            // bodies list may be modified.
            // If a body cannot be put into a sane orbit, it is removed.
            GenerateOrbitsForBodies(system, star, ref bodies, bandLimits, systemBodies, currentDateTime);

            return bodies;
        }

        /// <summary>
        /// Places passed bodies into a sane orbit around the parent.
        /// </summary>
        /// <param name="system">System we're working with.</param>
        /// <param name="parent">Parent entity we're working with.</param>
        /// <param name="bodies">List of bodies to place into orbit. May be modified if bodies cannot be placed in a sane orbit.</param>
        /// <param name="bandLimits">MinMax structure representing the distance limits for the orbit.</param>
        /// <param name="systemBodies">List of bodies already orbiting this parent. We work around these.</param>
        private void GenerateOrbitsForBodies(StarSystem system, Entity parent, ref List<ProtoEntity> bodies, MinMaxStruct bandLimits, List<ProtoEntity> systemBodies, DateTime currentDateTime)
        {
            double totalBandMass = 0;

            // Calculate the total mass of bodies we must place into orbit.
            foreach (ProtoEntity body in bodies)
            {
                MassVolumeDB bodyMVDB = body.GetDataBlob<MassVolumeDB>();
                totalBandMass += bodyMVDB.Mass;
            }

            // Prepare the loop variables.
            double remainingBandMass = totalBandMass;

            double minDistance = bandLimits.Min; // The minimum distance we can place a body.
            double remainingDistance = bandLimits.Max - minDistance; // distance remaining that we can place bodies into.

            double insideApoapsis = double.MinValue; // Apoapsis of the orbit that is inside of the next body.
            double outsidePeriapsis = double.MaxValue; // Periapsis of the orbit that is outside of the next body.
            double insideMass = 0; // Mass of the object that is inside of the next body.
            double outsideMass = 0; // Mass of the object that is outside of the next body.

            // Find the inside and outside bodies.
            foreach (ProtoEntity systemBody in systemBodies)
            {
                OrbitDB bodyOrbit = systemBody.GetDataBlob<OrbitDB>();
                MassVolumeDB bodyMass = systemBody.GetDataBlob<MassVolumeDB>();

                // Find if the current systemBody is within the bandLimit
                // and is in a higher orbit than the previous insideOrbit.
                if (bodyOrbit.Apoapsis <= bandLimits.Min && bodyOrbit.Apoapsis > insideApoapsis)
                {
                    insideApoapsis = bodyOrbit.Apoapsis;
                    insideMass = bodyMass.Mass;
                }
                // Otherwise, find if the current systemBody is within the bandLimit
                // and is in a lower orbit than the previous outsideOrbit.
                else if (bodyOrbit.Periapsis >= bandLimits.Max && bodyOrbit.Periapsis < outsidePeriapsis)
                {
                    outsidePeriapsis = bodyOrbit.Periapsis;
                    outsideMass = bodyMass.Mass;
                }
                // Note, we build our insideOrbit and outsideOrbits, then we try to build orbits between insideOrbit and outsideOrbit.
                // If there's only one body, but it's right INSIDE the bandLimits, then our insideOrbit will be very close to our bandLimit,
                // and we likely wont be able to find a sane orbit, even if there's plenty of room on the inside side.
            }

            // for loop because we might modify bodies.
            for (int i = 0; i < bodies.Count; i++)
            {
                ProtoEntity currentBody = bodies[i];

                MassVolumeDB currentMVDB = currentBody.GetDataBlob<MassVolumeDB>();

                // Limit the orbit to the ratio of object mass and remaining distance.
                double massRatio = currentMVDB.Mass / remainingBandMass;
                double maxDistance = remainingDistance * massRatio + minDistance;
                // We'll either find this orbit, or eject it, so this body is no longer part of remaining mass.
                remainingBandMass -= currentMVDB.Mass;

                // Do the heavy math to find the orbit.
                OrbitDB currentOrbit = FindClearOrbit(system, parent, currentBody, insideApoapsis, outsidePeriapsis, insideMass, outsideMass, minDistance, maxDistance, currentDateTime);

                if (currentOrbit == null)
                {
                    // Failed to find a clear orbit. This body is "ejected."
                    bodies.RemoveAt(i);
                    i--; // Keep i at the right spot.
                    continue;
                }
                currentBody.SetDataBlob(currentOrbit);

                insideMass = currentMVDB.Mass;
                insideApoapsis = currentOrbit.Apoapsis;
            }
        }

        /// <summary>
        /// Finds a gravitationally stable orbit between the insideApoapsis and the outsidePeriapsis for a body.
        /// </summary>
        private OrbitDB FindClearOrbit(StarSystem system, Entity parent, ProtoEntity body, double insideApoapsis, double outsidePeriapsis, double insideMass, double outsideMass, double minDistance, double maxDistance, DateTime currentDateTime)
        {
            MassVolumeDB parentMVDB = parent.GetDataBlob<MassVolumeDB>();
            double parentMass = parentMVDB.Mass;

            MassVolumeDB myMVDB = body.GetDataBlob<MassVolumeDB>();
            double myMass = myMVDB.Mass;

            // Adjust minDistance
            double gravAttractionInsiderNumerator = GameConstants.Science.GravitationalConstant * myMass * insideMass;
            double gravAttractionOutsideNumerator = GameConstants.Science.GravitationalConstant * myMass * outsideMass;
            double gravAttractionParentNumerator = GameConstants.Science.GravitationalConstant * myMass * parentMass;
            double gravAttractionToInsideOrbit = gravAttractionInsiderNumerator / ((minDistance - insideApoapsis) * (minDistance - insideApoapsis));
            double gravAttractionToOutsideOrbit = gravAttractionOutsideNumerator / ((outsidePeriapsis - maxDistance) * (outsidePeriapsis - maxDistance));
            double gravAttractionToParent = gravAttractionParentNumerator / (minDistance * minDistance);

            // Make sure we're 20x more attracted to our Parent, then our inside neighbor.
            while (gravAttractionToInsideOrbit * _galaxyGen.Settings.OrbitGravityFactor > gravAttractionToParent)
            {
                // We're too attracted to our inside neighbor, increase minDistance by 1%.
                // Assuming our parent is more massive than our inside neighbor, then this will "tip" us to be more attracted to parent.
                minDistance += minDistance * 0.01;

                // Reevaluate our gravitational attractions with new minDistance.
                gravAttractionToInsideOrbit = gravAttractionInsiderNumerator / ((minDistance - insideApoapsis) * (minDistance - insideApoapsis));
                gravAttractionToOutsideOrbit = gravAttractionOutsideNumerator / ((outsidePeriapsis - maxDistance) * (outsidePeriapsis - maxDistance));
                gravAttractionToParent = gravAttractionParentNumerator / (minDistance * minDistance);
            }

            if (gravAttractionToOutsideOrbit * _galaxyGen.Settings.OrbitGravityFactor > gravAttractionToParent || minDistance > maxDistance)
            {
                // Unable to find suitable orbit. This body is rejected.
                return null;
            }

            double sma = GMath.SelectFromRange(minDistance, maxDistance, system.RNG.NextDouble());

            // Calculate max eccentricity.
            // First calc max eccentricity for the apoapsis.
            double maxApoEccentricity = (maxDistance - sma) / sma;
            // Now calc max eccentricity for periapsis.
            double minPeriEccentricity = -((minDistance - sma) / sma);

            // Use the smaller value.
            if (minPeriEccentricity < maxApoEccentricity)
            {
                // We use maxApoEccentricity in next calc.
                maxApoEccentricity = minPeriEccentricity;
            }
            
            // Enforce GalaxyFactory settings.
            MinMaxStruct eccentricityMinMax = _galaxyGen.Settings.BodyEccentricityByType[body.GetDataBlob<SystemBodyDB>().Type];
            if (eccentricityMinMax.Max > maxApoEccentricity)
            {
                eccentricityMinMax.Max = maxApoEccentricity;
            }
            // GalaxyFactory settings disallow this orbit. Eject.
            if (eccentricityMinMax.Min > eccentricityMinMax.Max)
            {
                return null;
            }

            // Now select a random eccentricity within the limits.
            double eccentricity = GMath.SelectFromRange(eccentricityMinMax, system.RNG.NextDouble());

            return new OrbitDB(parent, parentMass, myMass, sma, eccentricity, system.RNG.NextDouble() * _galaxyGen.Settings.MaxBodyInclination, system.RNG.NextDouble() * 360, system.RNG.NextDouble() * 360, system.RNG.NextDouble() * 360, currentDateTime);
        }

        private void FinalizeBodies(StaticDataStore staticData, StarSystem system, Entity body, int bodyCount, DateTime currentDateTime)
        {
            OrbitDB bodyOrbit = body.GetDataBlob<OrbitDB>();
            SystemBodyDB systemBodyDB = body.GetDataBlob<SystemBodyDB>();

            if (systemBodyDB.Type == BodyType.Asteroid)
            {
                FinalizeAsteroidBelt(staticData, system, body, bodyCount);
                return;
            }

            FinalizeSystemBodyDB(staticData, system, body);
            FinalizeNameDB(body, bodyOrbit.Parent, bodyCount);

            GenerateMoons(system, body, currentDateTime);

            // if there were any moons generated, finalize them:
            if (bodyOrbit.Children.Count > 0)
            {
                // Remove any invalid children (ones that failed generation).
                bodyOrbit.Children.RemoveAll(child => !child.IsValid);

                // Recursive call to finalize children.
                int numChildren = bodyOrbit.Children.Count; // update as the count may have changed.
                int recursiveBodyCount = 1;
                for (int i = 0; i < numChildren; i++)
                {
                    Entity child = bodyOrbit.Children[i];
                    FinalizeBodies(staticData, system, child, recursiveBodyCount, currentDateTime);
                    recursiveBodyCount++;
                }
            }
        }

        private static void FinalizeNameDB(ProtoEntity body, Entity parent, int bodyCount, string suffix = "")
        {
            // Set this body's name.
            string parentName = parent.GetDataBlob<NameDB>().DefaultName;
            string bodyName = parentName + " - " + bodyCount + suffix;
            body.GetDataBlob<NameDB>().SetName(Entity.InvalidEntity, bodyName);
        }

        private void GenerateMoons(StarSystem system, Entity parent, DateTime currentDateTime)
        {
            // BUG: Moons are currently taking a large ratio of mass compared to parents, and when formed on GasGiants can be extremely large.
            SystemBodyDB parentBodyDB = parent.GetDataBlob<SystemBodyDB>();

            // first lets see if this planet gets moons:
            if (system.RNG.NextDouble() > _galaxyGen.Settings.MoonGenerationChanceByPlanetType[parentBodyDB.Type])
                return; // no moons for you :(

            // Okay lets work out the number of moons based on:
            // The mass of the parent in proportion to the maximum mass for a planet of that type.
            // The MaxNoOfMoonsByPlanetType
            // And a random number for randomness.
            MassVolumeDB parentMVDB = parent.GetDataBlob<MassVolumeDB>();
            double massRatioOfParent = parentMVDB.Mass / _galaxyGen.Settings.SystemBodyMassByType[parentBodyDB.Type].Max;
            double moonGenChance = massRatioOfParent * system.RNG.NextDouble() * _galaxyGen.Settings.MaxNoOfMoonsByPlanetType[parentBodyDB.Type];
            moonGenChance = GMath.Clamp(moonGenChance, 1, _galaxyGen.Settings.MaxNoOfMoonsByPlanetType[parentBodyDB.Type]);
            int numMoons = (int)Math.Round(moonGenChance);

            // first pass to gen mass etc:
            var moons = new List<ProtoEntity>(numMoons);
            while (numMoons > 0)
            {
                ProtoEntity newMoon = CreateBaseBody();
                SystemBodyDB newMoonBodyDB = newMoon.GetDataBlob<SystemBodyDB>();

                newMoonBodyDB.Type = BodyType.Moon;
                
                // Enforce GalaxyFactory mass limits.
                MinMaxStruct moonMassMinMax = _galaxyGen.Settings.SystemBodyMassByType[newMoonBodyDB.Type];
                double maxRelativeMass = parentMVDB.Mass * _galaxyGen.Settings.MaxMoonMassRelativeToParentBody;
                if (maxRelativeMass < moonMassMinMax.Max)
                {
                    moonMassMinMax.Max = maxRelativeMass;
                }

                MassVolumeDB newMoonMVDB = MassVolumeDB.NewFromMassAndDensity(
                    GMath.SelectFromRange(moonMassMinMax, system.RNG.NextDouble()),
                    GMath.SelectFromRange(_galaxyGen.Settings.SystemBodyDensityByType[BodyType.Moon], system.RNG.NextDouble()));
                newMoon.SetDataBlob(newMoonMVDB, EntityManager.GetTypeIndex<MassVolumeDB>());

                moons.Add(newMoon);
                numMoons--;
            }

            double minMoonOrbitDist = parentMVDB.Radius * _galaxyGen.Settings.MinMoonOrbitMultiplier;
            double maxMoonDistance = _galaxyGen.Settings.MaxMoonOrbitDistanceByPlanetType[parentBodyDB.Type] * massRatioOfParent;

            GenerateOrbitsForBodies(system, parent, ref moons, new MinMaxStruct(minMoonOrbitDist, maxMoonDistance), new List<ProtoEntity>(), currentDateTime);

            // create proper entities:
            foreach (var moon in moons)
            {
                Entity.Create(system.SystemManager, moon);
            }
        }

        private void FinalizeAsteroidBelt(StaticDataStore staticData, StarSystem system, Entity body, int bodyCount)
        {
            MassVolumeDB beltMVDB = body.GetDataBlob<MassVolumeDB>();
            OrbitDB referenceOrbit = body.GetDataBlob<OrbitDB>();

            int asteroidCount = 1;
            while (beltMVDB.Mass > 0)
            {
                ProtoEntity newProtoBody = CreateBaseBody();
                Entity newBody = Entity.Create(system.SystemManager, newProtoBody);
                SystemBodyDB newBodyDB = newBody.GetDataBlob<SystemBodyDB>();

                if (system.RNG.NextDouble() > (1.0 / _galaxyGen.Settings.NumberOfAsteroidsPerDwarfPlanet))
                {
                    newBodyDB.Type = BodyType.Asteroid;
                }
                else
                {
                    newBodyDB.Type = BodyType.DwarfPlanet;
                }

                MassVolumeDB mvDB = MassVolumeDB.NewFromMassAndDensity(
                    GMath.SelectFromRange(_galaxyGen.Settings.SystemBodyMassByType[newBodyDB.Type], system.RNG.NextDouble()),
                    GMath.SelectFromRange(_galaxyGen.Settings.SystemBodyDensityByType[newBodyDB.Type], system.RNG.NextDouble()));
                newBody.SetDataBlob(mvDB, EntityManager.GetTypeIndex<MassVolumeDB>());

                FinalizeAsteroidOrbit(system, newBody, referenceOrbit);
                FinalizeSystemBodyDB(staticData, system, newBody);
                FinalizeNameDB(newBody, referenceOrbit.Parent, bodyCount, "-A" + asteroidCount.ToString());

                beltMVDB.Mass -= mvDB.Mass;
                asteroidCount++;
            }

            // now we are finished with the belt reference asteroid, remove it:
            body.Destroy();
        }

        /// <summary>
        /// Generates an orbit for an Asteroid or Dwarf SystemBody. The orbit will be a slight deviation of the reference orbit provided.
        /// </summary>
        private void FinalizeAsteroidOrbit(StarSystem system, Entity newBody, OrbitDB referenceOrbit)
        {
            if (referenceOrbit.Parent == null)
            {
                throw new InvalidOperationException("Invalid Reference Orbit.");
            }
            // we will use the reference orbit + MaxAsteroidOrbitDeviation to constrain the orbit values:

            // Create semiMajorAxis:
            double deviation = referenceOrbit.SemiMajorAxis * _galaxyGen.Settings.MaxAsteroidOrbitDeviation;
            double min = referenceOrbit.SemiMajorAxis - deviation;
            double max = referenceOrbit.SemiMajorAxis + deviation;
            double semiMajorAxis = GMath.SelectFromRange(min, max, system.RNG.NextDouble());  // don't need to raise to power, reference orbit already did that.

            deviation = referenceOrbit.Eccentricity * Math.Pow(_galaxyGen.Settings.MaxAsteroidOrbitDeviation, 2);
            min = referenceOrbit.Eccentricity - deviation;
            max = referenceOrbit.Eccentricity + deviation;
            double eccentricity = GMath.SelectFromRange(min, max, system.RNG.NextDouble()); // get random eccentricity needs better distribution.

            deviation = referenceOrbit.Inclination * _galaxyGen.Settings.MaxAsteroidOrbitDeviation;
            min = referenceOrbit.Inclination - deviation;
            max = referenceOrbit.Inclination + deviation;
            double inclination = GMath.SelectFromRange(min, max, system.RNG.NextDouble()); // doesn't do much at the moment but may as well be there. Need better Dist.

            deviation = referenceOrbit.ArgumentOfPeriapsis * _galaxyGen.Settings.MaxAsteroidOrbitDeviation;
            min = referenceOrbit.ArgumentOfPeriapsis - deviation;
            max = referenceOrbit.ArgumentOfPeriapsis + deviation;
            double argumentOfPeriapsis = GMath.SelectFromRange(min, max, system.RNG.NextDouble());

            deviation = referenceOrbit.LongitudeOfAscendingNode * _galaxyGen.Settings.MaxAsteroidOrbitDeviation;
            min = referenceOrbit.LongitudeOfAscendingNode - deviation;
            max = referenceOrbit.LongitudeOfAscendingNode + deviation;
            double longitudeOfAscendingNode = GMath.SelectFromRange(min, max, system.RNG.NextDouble());

            // Keep the starting point of the orbit completely random.
            double meanAnomaly = system.RNG.NextDouble() * 360;

            // now Create the orbit:
            MassVolumeDB parentMVDB = referenceOrbit.Parent.GetDataBlob<MassVolumeDB>();
            MassVolumeDB myMVDB = newBody.GetDataBlob<MassVolumeDB>();
            newBody.SetDataBlob(OrbitDB.FromAsteroidFormat(referenceOrbit.Parent, parentMVDB.Mass, myMVDB.Mass, semiMajorAxis, eccentricity, inclination,
                                                    longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, _galaxyGen.Settings.J2000));
        }

        /// <summary>
        /// This function puts all the finishing touiches on a system body data blob.
        /// </summary>
        private void FinalizeSystemBodyDB(StaticDataStore staticData, StarSystem system, ProtoEntity body)
        {
            SystemBodyDB bodyInfo = body.GetDataBlob<SystemBodyDB>();
            OrbitDB bodyOrbit = body.GetDataBlob<OrbitDB>();
            MassVolumeDB bodyMVDB = body.GetDataBlob<MassVolumeDB>();

            Entity parent = bodyOrbit.Parent;
            if (parent == null)
            {
                throw new InvalidOperationException("Body cannot be finalized without a parent.");
            }
            double parentSMA = 0;

            Entity star;
            if (parent.HasDataBlob<StarInfoDB>())
            {
                // Parent is a star.
                star = parent;
            }
            else
            {
                OrbitDB parentOrbit = parent.GetDataBlob<OrbitDB>();
                parentSMA += parentOrbit.SemiMajorAxis;
                if (parentOrbit.Parent == null)
                {
                    throw new InvalidOperationException("Body cannot be finalized without a root star.");
                }
                star = parentOrbit.Parent;
            }

            StarInfoDB starInfo = star.GetDataBlob<StarInfoDB>();

            switch (bodyInfo.Type)
            {
                case BodyType.Asteroid:
                case BodyType.Comet:
                case BodyType.DwarfPlanet:
                case BodyType.Moon:
                case BodyType.Terrestrial:
                    bodyInfo.SupportsPopulations = true;
                    break;
                default:
                    bodyInfo.SupportsPopulations = false;
                    break;
            }

            // Create some of the basic stats:
            bodyInfo.AxialTilt = (float)(system.RNG.NextDouble() * _galaxyGen.Settings.MaxBodyInclination);

            // generate the planets day length:
            //< @todo Should we do Tidally Locked bodies??? iirc bodies trend toward being tidally locked over time...
            bodyInfo.LengthOfDay = new TimeSpan((int)Math.Round(GMath.SelectFromRange(0, bodyOrbit.OrbitalPeriod.TotalDays, system.RNG.NextDouble())), system.RNG.Next(0, 24), system.RNG.Next(0, 60), 0);
            // just a basic sanity check to make sure we don't end up with a planet rotating once every 3 minutes, It'd pull itself apart!!
            if (bodyInfo.LengthOfDay < TimeSpan.FromHours(_galaxyGen.Settings.MiniumPossibleDayLength))
                bodyInfo.LengthOfDay += TimeSpan.FromHours(_galaxyGen.Settings.MiniumPossibleDayLength);

            // Note that base temp does not take into account albedo or atmosphere.
            bodyInfo.BaseTemperature = (float)CalculateBaseTemperatureOfBody(star, starInfo, bodyOrbit.SemiMajorAxis + parentSMA);

            // generate Plate tectonics
            if (bodyInfo.Type == BodyType.Terrestrial)
                bodyInfo.Tectonics = GenerateTectonicActivity(system, starInfo, bodyMVDB);
            else
                bodyInfo.Tectonics = TectonicActivity.NA;  // We are not a Terrestrial body, we have no Tectonics!!!

            // Generate Magnetic field, must be done before atmosphere:
            bodyInfo.MagneticField = (float)GMath.SelectFromRange(_galaxyGen.Settings.PlanetMagneticFieldByType[bodyInfo.Type], system.RNG.NextDouble());
            if (bodyInfo.Tectonics == TectonicActivity.Dead)
                bodyInfo.MagneticField *= 0.1F; // reduce magnetic field of a dead world.

            // Generate atmosphere:
            GenerateAtmosphere(system, body, staticData);

            // No radiation by default.
            bodyInfo.RadiationLevel = 0;

            // generat Minerals:
            MineralGeneration(staticData, system, body);

            // generate ruins:
            GenerateRuins(system, body);

            // run orbit for current date/time to get an initial position ofr the body:
            var positionDB = body.GetDataBlob<PositionDB>();
            positionDB.Position = OrbitProcessor.GetPosition(bodyOrbit, _currDateTime);
        }

        /// <summary>
        /// Generate plate techtonics taking into consideration the mass of the planet and its age (via Star.Age).
        /// </summary>
        private TectonicActivity GenerateTectonicActivity(StarSystem system, StarInfoDB starInfo, MassVolumeDB bodyMass)
        {
            if (system.RNG.NextDouble() > _galaxyGen.Settings.TerrestrialBodyTectonicActivityChance)
            {
                return TectonicActivity.Dead;
            }

            // this planet has some plate tectonics:
            // the following should give us a number between 0 and 1 for most bodies. Earth has a number of 0.217...
            // we converge in billion years instead of years (otherwise we get tiny numbers).
            double tectonicsChance = bodyMass.Mass / GameConstants.Units.EarthMassInKG / starInfo.Age * 100000000;
            tectonicsChance = GMath.Clamp(tectonicsChance, 0, 1);

            TectonicActivity t;

            // step down the thresholds to get the correct activity:
            if (tectonicsChance >= _galaxyGen.Settings.BodyTectonicsThresholds[TectonicActivity.Major])
                t = TectonicActivity.Major;
            else if (tectonicsChance >= _galaxyGen.Settings.BodyTectonicsThresholds[TectonicActivity.EarthLike])
                t = TectonicActivity.EarthLike;
            else if (tectonicsChance >= _galaxyGen.Settings.BodyTectonicsThresholds[TectonicActivity.Minor])
                t = TectonicActivity.Minor;
            else
                t = TectonicActivity.Dead;

            return t;
        }

        /// <summary>
        /// Calculates the temperature of a body given its parent star and its distance from that star.
        /// @note For info on how the Temp. is calculated see: http://en.wikipedia.org/wiki/Stefan%E2%80%93Boltzmann_law
        /// </summary>
        /// <returns>Temperature in Degrees C</returns>
        private static double CalculateBaseTemperatureOfBody(Entity star, StarInfoDB starInfo, double distanceFromStar)
        {
            MassVolumeDB starMVDB = star.GetDataBlob<MassVolumeDB>();
            double temp = Temperature.ToKelvin(starInfo.Temperature);
            temp = temp * Math.Sqrt(starMVDB.Radius / (2 * distanceFromStar));
            return Temperature.ToCelsius(temp);
        }


        /// <summary>
        /// This function generate ruins for the specified system Body.
        /// @todo Make Ruins Generation take star age/type into consideration??
        /// </summary>
        private void GenerateRuins(StarSystem system, ProtoEntity body)
        {
            // cache some DBs:
            var atmo = body.GetDataBlob<AtmosphereDB>();
            var bodyType = body.GetDataBlob<SystemBodyDB>().Type;
            var ruins = body.GetDataBlob<RuinsDB>();

            // first we will check that this body type can have ruins on it:
            if (bodyType != BodyType.Terrestrial
                || bodyType != BodyType.Moon)
            {
                return; // wrong type.
            }
            else if (atmo.Exists == false && (atmo.Pressure > 2.5 || atmo.Pressure < 0.01))
            {
                return; // no valid atmosphere!
            }
            else if (system.RNG.NextDouble() > _galaxyGen.Settings.RuinsGenerationChance)
            {
                return; // that's right... lucked out on this one.
            }

            // now if we have survived the gauntlet lets gen some Ruins!!
            ruins.RuinSize = _galaxyGen.Settings.RuinsSizeDistribution.Select(system.RNG.Next(0, 100));

            int quality = system.RNG.Next(0, 100);
            ruins.RuinQuality = _galaxyGen.Settings.RuinsQualityDistribution.Select(quality);
            if (ruins.RuinSize == RuinsDB.RSize.City && quality >= 95)
                ruins.RuinQuality = RuinsDB.RQuality.MultipleIntact;  // special case!!

            // Ruins count:
            ruins.RuinCount = (uint)GMath.SelectFromRange(_galaxyGen.Settings.RuinsCountRangeBySize[ruins.RuinSize], system.RNG.NextDouble());
            ruins.RuinCount = (uint)Math.Round(_galaxyGen.Settings.RuinsQualityAdjustment[ruins.RuinQuality] * ruins.RuinCount);
        }

        /// <summary>
        /// This function randomly generats minerals for a given system body. 
        /// Generation take into consideration the abundance of the mineral 
        /// and the bodies ratio of mass vs earth.
        /// </summary>
        public void MineralGeneration(StaticDataStore staticData, StarSystem system, ProtoEntity body)
        {
            var bodyInfo = body.GetDataBlob<SystemBodyDB>();
            var bodyMass = body.GetDataBlob<MassVolumeDB>();

            // get the mass ratio for this body to earth:
            double massRatio = bodyMass.Mass / GameConstants.Units.EarthMassInKG;
            double genChance = massRatio * system.RNG.NextDouble();
            double genChanceThreshold = _galaxyGen.Settings.MineralGenerationChanceByBodyType[bodyInfo.Type];

            // now lets see if this body has minerals
            if (BodyType.Comet != bodyInfo.Type // comets always have minerals.
                && genChance < genChanceThreshold)
            {
                // check failed return:
                return;
            }

            // this body has at least some minerals, lets generate them:
            foreach (var min in staticData.Minerals)
            {
                // create a MineralDepositInfo
                MineralDepositInfo mdi = new MineralDepositInfo();

                // get a genChance:
                double abundance = min.Abundance[bodyInfo.Type];
                genChance = massRatio * system.RNG.NextDouble() * abundance;

                if (genChance >= genChanceThreshold)
                {
                    mdi.Accessibility = GMath.Clamp(_galaxyGen.Settings.MinMineralAccessibility + genChance, 0, 1);
                    mdi.Amount = (int)Math.Round(_galaxyGen.Settings.MaxMineralAmmountByBodyType[bodyInfo.Type] * genChance);
                    mdi.HalfOriginalAmount = mdi.Amount / 2;

                    bodyInfo.Minerals.Add(min.ID, mdi);
                }
            }
        }

        /// <summary>
        /// This generates the rich assortment of all minerals for a homeworld. 
        /// This function should be used when creating homeworlds for the player race(s) or NPR Races.
        /// This function can also be used by the Space Master (not directly, but it is public for this reason).
        /// This function ensures that there is at least 50000 of every mineral and that every mineral has 
        /// an accessibility of at least 0.5.
        /// </summary>
        public void HomeworldMineralGeneration(StaticDataStore staticData, StarSystem system, Entity body)
        {
            var bodyInfo = body.GetDataBlob<SystemBodyDB>();
            bodyInfo.Minerals.Clear();  // because this function can be called on existing bodies we need to clear any existing minerals.

            foreach (var min in staticData.Minerals)
            {
                // create a MineralDepositInfo
                MineralDepositInfo mdi = new MineralDepositInfo
                {
                    Accessibility = GMath.Clamp(_galaxyGen.Settings.MinHomeworldMineralAccessibility + system.RNG.NextDouble() * min.Abundance[bodyInfo.Type], 0, 1), 
                    Amount = (int)Math.Round(_galaxyGen.Settings.MinHomeworldMineralAmmount + _galaxyGen.Settings.HomeworldMineralAmmount * system.RNG.NextDouble() * min.Abundance[bodyInfo.Type])
                };
                mdi.HalfOriginalAmount = mdi.Amount / 2;

                bodyInfo.Minerals.Add(min.ID, mdi);
            }
        }

        /// <summary>
        /// This function generates atmosphere for a body, including it's albedo and surface temp.
        /// </summary>
        /// <remarks>
        /// We first need to decid if this body has an atmosphere, the bigger the mor likly it is to have one.
        /// if it does then we need to add a primary gas (e.g. Nitrigen), a secondary gas (e.g. oxygen)
        /// Followed by up to 5 trace gases (e.g. Argon). 
        /// The bigger the body the more likly it is to have an atmo gas it should have and the more trace gases.
        /// </remarks>
        public void GenerateAtmosphere(StarSystem system, ProtoEntity body, StaticDataStore staticData)
        {
            var atmoDB = body.GetDataBlob<AtmosphereDB>();
            if (atmoDB == null)
                return; // no atmosphere for this body.

            SystemBodyDB bodyDB = body.GetDataBlob<SystemBodyDB>();
            MassVolumeDB mvDB = body.GetDataBlob<MassVolumeDB>();
            OrbitDB orbit = body.GetDataBlob<OrbitDB>();

            // Set Albeado (all bodies have an albedo):
            atmoDB.Albedo = (float)GMath.SelectFromRange(_galaxyGen.Settings.PlanetAlbedoByType[bodyDB.Type], system.RNG.NextDouble());

            // Atmo modifer is used to determine how thick the atmosphere should be, higher = thicker.
            double atmoModifer = _galaxyGen.Settings.AtmosphereGenerationModifier[bodyDB.Type] * (mvDB.Mass / GameConstants.Units.EarthMassInKG);
            double atmoGenChance = GMath.Clamp(atmoModifer, 0, 1); // used to detmine if we should haver an atmosphere at all.

            if (atmoGenChance > system.RNG.NextDouble())
            {
                // we can generate an atmosphere!
                // first we should decide how thick it should be:
                double newATM = GenAtmosphereThickness(mvDB.Mass, bodyDB, orbit, atmoModifer, system.RNG.NextDouble());

                // set an initial surface temp to the base temp, adjusted for albedo:
                atmoDB.SurfaceTemperature = atmoDB.SurfaceTemperature = bodyDB.BaseTemperature * (1 - atmoDB.Albedo);

                // now we will want to select gasses for the atmosphere:
                SelectGases(newATM, atmoModifer, bodyDB, mvDB, atmoDB, system, staticData);
            }

            // finally Run the atmo processor over it to create the greenhous factors and descriptions etc.
            // We want to run this even for bodies without an atmosphere.
            AtmosphereProcessor.UpdateAtmosphere(atmoDB, bodyDB);

            // Add hydrospher if terra like world that has an atmosphere:
            if ((bodyDB.Type == BodyType.Terrestrial || bodyDB.Type == BodyType.Terrestrial) && atmoDB.Exists)
            {
                if (system.RNG.NextDouble() > 0.75)
                {
                    atmoDB.Hydrosphere = true;
                    atmoDB.HydrosphereExtent = (short)(system.RNG.NextDouble() * 100);
                }
            }
        }

        /// <summary>
        /// Works out how thick the atmosphere for the body should be, returns the value in atm.
        /// </summary>
        double GenAtmosphereThickness(double bodyMass, SystemBodyDB body, OrbitDB orbit,  double atmoModifer, double randomModifer)
        {
            switch (body.Type)
            {
                case BodyType.GasDwarf:
                case BodyType.GasGiant:
                case BodyType.IceGiant:
                    return 1;   // gas planet types always have an atmosphere of 1 atm.
                case BodyType.Moon:
                case BodyType.Terrestrial:
                case BodyType.Asteroid:
                case BodyType.Comet:
                case BodyType.DwarfPlanet:
                default:
                    // this will produce 1 atm for earth like planets, less for smaller planets, more for larger:
                    double massRatio = (bodyMass / GameConstants.Units.EarthMassInKG);
                    double atm = massRatio * massRatio * atmoModifer;
                    
                    // now we have a nice starting atm, lets modify it:
                    // first we will reduce it if the planet is closer to the star, increase it if it is further away using the ewchosphere of the star:
                    StarInfoDB starInfo;
                    double ecosphereRatio = 1;
                    if (body.Type == BodyType.Moon)
                    {
                        // if moon get planet orbit, then star
                        var parentOrbitDB = orbit.ParentDB as OrbitDB;
                        starInfo = parentOrbitDB.Parent.GetDataBlob<StarInfoDB>();
                        ecosphereRatio = (parentOrbitDB.SemiMajorAxis / starInfo.EcoSphereRadius);
                    }
                    else
                    {
                        // if planet get star:
                        starInfo = orbit.Parent.GetDataBlob<StarInfoDB>();
                        ecosphereRatio = GMath.Clamp(orbit.SemiMajorAxis / starInfo.EcoSphereRadius, 0.1, 2);
                    }

                    atm = atm * ecosphereRatio;  // if inside eco sphere this will reduce atmo, increase it if outside.
                    
                    // now we will see if this planet should be venus like pressure cooker:
                    // if the planet is very close it will 
                    double inverseEchoshpereRatio = 1 - (GMath.Clamp(ecosphereRatio, 0, 1));
                    if (randomModifer < _galaxyGen.Settings.RunawayGreenhouseEffectChance * inverseEchoshpereRatio)
                    {
                        atm *= _galaxyGen.Settings.RunawayGreenhouseEffectMultiplyer;
                    }
                    else
                    {
                        // if we arn't a pressure cooker planet, then lets modify the atmosphere pressure according to the magnetic feild:
                        double magneticFieldRatio = body.MagneticField / _galaxyGen.Settings.PlanetMagneticFieldByType[body.Type].Max;
                        atm *= magneticFieldRatio;
                    }

                    // finally clamp the atmosphere to a resonable value:
                    return GMath.Clamp(atm, _galaxyGen.Settings.MinMaxAtmosphericPressure.Min, _galaxyGen.Settings.MinMaxAtmosphericPressure.Max);
            }
        }

        /// <summary>
        /// Selects suitable gases to make up an atmosphere.
        /// </summary>
        void SelectGases(double atm, double atmoModifer, SystemBodyDB body, MassVolumeDB bodyMassDB, AtmosphereDB atmoDB, StarSystem system, StaticDataStore staticData)
        {
            // get the gas list:
            var gases = new WeightedList<AtmosphericGasSD>();

            // creat a gas list for this planet, it should not include any gases that are too light or which would boil away due to too high temp.
            foreach (var possibleGas in staticData.AtmosphericGases)
            {
                if (Temperature.ToKelvin(possibleGas.Value.BoilingPoint) * 5 > Temperature.ToKelvin(body.BaseTemperature))
                {
                    // then it is too cold for this gas to boil off
                    if (possibleGas.Value.MinGravity < bodyMassDB.SurfaceGravity)
                    {
                        gases.Add(possibleGas.Weight, possibleGas.Value);
                    }
                }
            }

            // do a quick safty check:
            if (gases.Count() < 2)
                return; // bail on selecting gases, we don't have enough!!

            // get the primary gass:
            double percentage = 0.6 + 0.3 * system.RNG.NextDouble();
            var gas = gases.Select(system.RNG.NextDouble());
            atmoDB.Composition.Add(gas, (float)(percentage * atm));
            gases.Remove(gas);

            // get the secondary gas:
            percentage = 0.98 - percentage;
            gas = gases.Select(system.RNG.NextDouble());
            atmoDB.Composition.Add(gas, (float)(percentage * atm));
            gases.Remove(gas);

            // get the trace gases, note that we will not care so much about 
            int noOfTraceGases = (int)GMath.Clamp(Math.Round(5 * atmoModifer), 1, 5);

            // do another quick safty check:
            if (gases.Count() < noOfTraceGases + 1)
                return; // bail, not enough gases left to select trace gases.

            double remainingPercentage = 0.02;
            percentage = 0;
            for (int i = 0; i < noOfTraceGases + 1; ++i)
            {
                percentage = (remainingPercentage - percentage) * system.RNG.NextDouble();  // just use random numbers, it will be close enough.
                gas = gases.Select(system.RNG.NextDouble()); 
                atmoDB.Composition.Add(gas, (float)(percentage * atm));
                gases.Remove(gas);
            }
        }
    }
}