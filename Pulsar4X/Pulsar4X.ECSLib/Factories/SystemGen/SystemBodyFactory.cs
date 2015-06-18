using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class SystemBodyFactory
    {
        private GalaxyFactory _galaxyGen;

        public SystemBodyFactory(GalaxyFactory galaxyGen)
        {
            _galaxyGen = galaxyGen;
        }

        /// <summary>
        /// Creates an uninitialized body in the specified system.
        /// </summary>
        public static Entity CreateBaseBody(StarSystem system)
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
            Entity newPlanet = new Entity(planetDBs);

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
            double starSpectralTypeRatio = _galaxyGen.Settings.StarSpecralTypePlanetGenerationRatio[starInfo.SpectralType];

            // Random value.
            double randomMultiplier = system.RNG.NextDouble();

            double percentOfMax = GMath.Clamp(starMassRatio * starSpectralTypeRatio * randomMultiplier, 0, 1);

            return (int)Math.Round(percentOfMax * _galaxyGen.Settings.MaxNoOfPlanets);
        }

        /// <summary>
        /// Generate all bodies for the specified star.
        /// </summary>
        internal void GenerateSystemBodiesForStar(StarSystem system, Entity star)
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
            var systemBodies = new List<Entity>(numberOfBodies);

            systemBodies.AddRange(GenerateBodiesForBand(system, star, SystemBand.HabitableBand, habitableZone, numHabitableZoneBodies, systemBodies));
            systemBodies.AddRange(GenerateBodiesForBand(system, star, SystemBand.InnerBand, innerZone, numInnerZoneBodies, systemBodies));
            systemBodies.AddRange(GenerateBodiesForBand(system, star, SystemBand.OuterBand, outerZone, numOuterZoneBodies, systemBodies));

            // Finalize all bodies that were actually added to the star.
            int bodyCount = 1;
            foreach (Entity body in systemBodies)
            {
                FinalizeBodies(system, body, bodyCount);
                body.Register(system.SystemManager);
                bodyCount++;
            }

            // Finally, comets!
            GenerateComets(system, star);
        }

        /// <summary>
        /// Generates a random number of comets for a given star. The number of generated will 
        /// be at least GalaxyGen.MiniumCometsPerSystem and never more then GalaxyGen.MaxNoOfComets.
        /// </summary>
        private void GenerateComets(StarSystem system, Entity star)
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

                Entity newComet = CreateBaseBody(system);
                NameDB cometName = newComet.GetDataBlob<NameDB>();
                cometName.Name[Entity.InvalidEntity] = starName.Name[Entity.InvalidEntity] + " - Comet " + (i + 1);

                SystemBodyDB cometBodyDB = newComet.GetDataBlob<SystemBodyDB>();
                cometBodyDB.Type = BodyType.Comet;

                MassVolumeDB cometMVDB = newComet.GetDataBlob<MassVolumeDB>();
                cometMVDB.Mass = GMath.SelectFromRange(_galaxyGen.Settings.SystemBodyMassByType[BodyType.Comet], system.RNG.NextDouble());
                cometMVDB.Volume = GMath.SelectFromRange(_galaxyGen.Settings.SystemBodyDensityByType[BodyType.Comet], system.RNG.NextDouble());

                GenerateCometOrbit(system, star, newComet);

                FinalizeSystemBodyDB(system, newComet);

                newComet.Register(system.SystemManager);
            }
        }

        /// <summary>
        /// Generates a very random orbit for comets. Doesn't care about other bodies.
        /// </summary>
        private void GenerateCometOrbit(StarSystem system, Entity star, Entity comet)
        {
            StarInfoDB starInfo = star.GetDataBlob<StarInfoDB>();
            double semiMajorAxis = GMath.SelectFromRange(_galaxyGen.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType], system.RNG.NextDouble());
            double eccentricity = GMath.SelectFromRange(_galaxyGen.Settings.BodyEccentricityByType[BodyType.Comet], system.RNG.NextDouble());
            double inclination = system.RNG.NextDouble() * _galaxyGen.Settings.MaxBodyInclination;
            double longitudeOfAscendingNode = system.RNG.NextDouble() * 360;
            double argumentOfPeriapsis = system.RNG.NextDouble() * 360;
            double meanAnomaly = system.RNG.NextDouble() * 360;

            comet.SetDataBlob(new OrbitDB(star, star.GetDataBlob<MassVolumeDB>(), comet.GetDataBlob<MassVolumeDB>(), semiMajorAxis,
                                            eccentricity, inclination, longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, _galaxyGen.Settings.J2000));
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
        private List<Entity> GenerateBodiesForBand(StarSystem system, Entity star, SystemBand systemBand, MinMaxStruct bandLimits, int numBodies, List<Entity> systemBodies)
        {
            var bodies = new List<Entity>(numBodies);

            int numAsteroidBelts = 0;

            // Generate basic bodies with types and masses.
            while (numBodies > 0)
            {
                Entity newBody = CreateBaseBody(system);

                MassVolumeDB newBodyMVDB = newBody.GetDataBlob<MassVolumeDB>();
                newBodyMVDB.Mass = 1; // Later we do some multiplication.
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
                        newBodyMVDB.Mass = noOfAsteroids;
                    }
                }

                // Mass multiplication here. This allows us to set the mass to the correct value for both asteroid belts and other bodies.
                newBodyMVDB.Mass *= GMath.SelectFromRange(_galaxyGen.Settings.SystemBodyMassByType[newBodyBodyDB.Type], system.RNG.NextDouble());

                bodies.Add(newBody);
                numBodies--;
            }

            // Generate the orbits for the bodies.
            // bodies list may be modified.
            // If a body cannot be put into a sane orbit, it is removed.
            GenerateOrbitsForBodies(system, star, ref bodies, bandLimits, systemBodies);

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
        private void GenerateOrbitsForBodies(StarSystem system, Entity parent, ref List<Entity> bodies, MinMaxStruct bandLimits, List<Entity> systemBodies)
        {
            double totalBandMass = 0;

            // Calculate the total mass of bodies we must place into orbit.
            foreach (Entity body in bodies)
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
            foreach (Entity systemBody in systemBodies)
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
                Entity currentBody = bodies[i];

                MassVolumeDB currentMVDB = currentBody.GetDataBlob<MassVolumeDB>();

                // Limit the orbit to the ratio of object mass and remaining distance.
                double massRatio = currentMVDB.Mass / remainingBandMass;
                double maxDistance = remainingDistance * massRatio + minDistance;
                // We'll either find this orbit, or eject it, so this body is no longer part of remaining mass.
                remainingBandMass -= currentMVDB.Mass;

                // Do the heavy math to find the orbit.
                OrbitDB currentOrbit = FindClearOrbit(system, parent, currentBody, insideApoapsis, outsidePeriapsis, insideMass, outsideMass, minDistance, maxDistance);

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
        private OrbitDB FindClearOrbit(StarSystem system, Entity parent, Entity body, double insideApoapsis, double outsidePeriapsis, double insideMass, double outsideMass, double minDistance, double maxDistance)
        {
            MassVolumeDB parentMVDB = parent.GetDataBlob<MassVolumeDB>();
            double parentMass = parentMVDB.Mass;

            MassVolumeDB myMVDB = body.GetDataBlob<MassVolumeDB>();

            // Adjust minDistance
            double gravAttractionInsiderNumerator = GameSettings.Science.GravitationalConstant * myMVDB.Mass * insideMass;
            double gravAttractionOutsideNumerator = GameSettings.Science.GravitationalConstant * myMVDB.Mass * outsideMass;
            double gravAttractionParentNumerator = GameSettings.Science.GravitationalConstant * myMVDB.Mass * parentMass;
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

            return new OrbitDB(parent, parentMVDB, myMVDB, sma, eccentricity, system.RNG.NextDouble() * _galaxyGen.Settings.MaxBodyInclination, system.RNG.NextDouble() * 360, system.RNG.NextDouble() * 360, system.RNG.NextDouble() * 360, _galaxyGen.Settings.J2000);
        }

        private void FinalizeBodies(StarSystem system, Entity body, int bodyCount)
        {
            OrbitDB bodyOrbit = body.GetDataBlob<OrbitDB>();
            SystemBodyDB systemBodyDB = body.GetDataBlob<SystemBodyDB>();

            if (systemBodyDB.Type == BodyType.Asteroid)
            {
                FinalizeAsteroidBelt(system, body, bodyCount);
                return;
            }

            FinalizeSystemBodyDB(system, body);
            FinalizeMassVolumeDB(system, body);
            FinalizeNameDB(body, bodyOrbit.Parent, bodyCount);

            GenerateMoons(system, body);

            // Recursive call to finalize children.
            int recursiveBodyCount = 1;
            int numChildren = bodyOrbit.Children.Count;
            for (int i = 0; i < numChildren; i++)
            {
                Entity child = bodyOrbit.Children[i];
                if (child.IsValid)
                {
                    FinalizeBodies(system, child, recursiveBodyCount);
                    recursiveBodyCount++;
                }
                else
                {
                    // Prune rejected children.
                    bodyOrbit.Children.RemoveAt(i);
                    i--;
                }
            }
        }

        private static void FinalizeNameDB(Entity body, Entity parent, int bodyCount)
        {
            // Set this body's name.
            string parentName = parent.GetDataBlob<NameDB>().Name[Entity.InvalidEntity];
            string bodyName = parentName + " - " + bodyCount;
            body.GetDataBlob<NameDB>().Name.Add(Entity.InvalidEntity, bodyName);
        }

        private void FinalizeMassVolumeDB(StarSystem system, Entity body)
        {
            MassVolumeDB massVolumeDB = body.GetDataBlob<MassVolumeDB>();
            SystemBodyDB systemBodyDB = body.GetDataBlob<SystemBodyDB>();

            // Fill the MVDB.Volume property by solving from a density selection.
            massVolumeDB.Volume = MassVolumeDB.GetVolume(massVolumeDB.Mass, GMath.SelectFromRange(_galaxyGen.Settings.SystemBodyDensityByType[systemBodyDB.Type], system.RNG.NextDouble()));
        }

        private void GenerateMoons(StarSystem system, Entity parent)
        {
            // BUG: Moons are not currently generating any properties, as seen in the resulting JSON files.
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
            var moons = new List<Entity>(numMoons);
            while (numMoons > 0)
            {
                Entity newMoon = CreateBaseBody(system);

                MassVolumeDB newMoonMVDB = newMoon.GetDataBlob<MassVolumeDB>();
                SystemBodyDB newMoonBodyDB = newMoon.GetDataBlob<SystemBodyDB>();

                newMoonBodyDB.Type = BodyType.Moon;
                
                // Enforce GalaxyFactory mass limits.
                MinMaxStruct moonMassMinMax = _galaxyGen.Settings.SystemBodyMassByType[newMoonBodyDB.Type];
                double maxRelativeMass = parentMVDB.Mass * _galaxyGen.Settings.MaxMoonMassRelativeToParentBody;
                if (maxRelativeMass > moonMassMinMax.Max)
                {
                    moonMassMinMax.Max = maxRelativeMass;
                }

                newMoonMVDB.Mass = GMath.SelectFromRange(moonMassMinMax, system.RNG.NextDouble());
                newMoonMVDB.Volume = GMath.SelectFromRange(_galaxyGen.Settings.SystemBodyDensityByType[BodyType.Moon], system.RNG.NextDouble());
                moons.Add(newMoon);
                numMoons--;
            }

            double minMoonOrbitDist = parentMVDB.Radius * _galaxyGen.Settings.MinMoonOrbitMultiplier;
            double maxMoonDistance = _galaxyGen.Settings.MaxMoonOrbitDistanceByPlanetType[parentBodyDB.Type] * massRatioOfParent;

            GenerateOrbitsForBodies(system, parent, ref moons, new MinMaxStruct(minMoonOrbitDist, maxMoonDistance), new List<Entity>());
        }

        private void FinalizeAsteroidBelt(StarSystem system, Entity body, int bodyCount)
        {
            MassVolumeDB beltMVDB = body.GetDataBlob<MassVolumeDB>();
            OrbitDB referenceOrbit = body.GetDataBlob<OrbitDB>();

            while (beltMVDB.Mass > 0)
            {
                Entity newBody = CreateBaseBody(system);
                newBody.Register(system.SystemManager);
                SystemBodyDB newBodyDB = newBody.GetDataBlob<SystemBodyDB>();

                if (system.RNG.NextDouble() > (1.0 / _galaxyGen.Settings.NumberOfAsteroidsPerDwarfPlanet))
                {
                    newBodyDB.Type = BodyType.Asteroid;
                }
                else
                {
                    newBodyDB.Type = BodyType.DwarfPlanet;
                }

                MassVolumeDB newBodyMVDB = newBody.GetDataBlob<MassVolumeDB>();

                newBodyMVDB.Mass = GMath.SelectFromRange(_galaxyGen.Settings.SystemBodyMassByType[newBodyDB.Type], system.RNG.NextDouble());

                FinalizeAsteroidOrbit(system, newBody, referenceOrbit);
                FinalizeSystemBodyDB(system, newBody);
                FinalizeMassVolumeDB(system, newBody);
                FinalizeNameDB(newBody, referenceOrbit.Parent, bodyCount);

                beltMVDB.Mass -= newBodyMVDB.Mass;
            }
        }

        /// <summary>
        /// Generates an orbit for an Asteroid or Dwarf SystemBody. The orbit will be a slight deviation of the reference orbit provided.
        /// </summary>
        private void FinalizeAsteroidOrbit(StarSystem system, Entity newBody, OrbitDB referenceOrbit)
        {
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
            newBody.SetDataBlob(OrbitDB.FromAsteroidFormat(referenceOrbit.Parent, referenceOrbit.Parent.GetDataBlob<MassVolumeDB>(),
                                                    newBody.GetDataBlob<MassVolumeDB>(), semiMajorAxis, eccentricity, inclination,
                                                    longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, _galaxyGen.Settings.J2000));
        }

        private void FinalizeSystemBodyDB(StarSystem system, Entity body)
        {
            SystemBodyDB bodyInfo = body.GetDataBlob<SystemBodyDB>();
            OrbitDB bodyOrbit = body.GetDataBlob<OrbitDB>();
            MassVolumeDB bodyMVDB = body.GetDataBlob<MassVolumeDB>();

            Entity parent = bodyOrbit.Parent;
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
            //< @todo Should we do Tidaly Locked bodies??? iirc bodies trend toward being tidaly locked over time...
            bodyInfo.LengthOfDay = new TimeSpan((int)Math.Round(GMath.SelectFromRange(0, bodyOrbit.OrbitalPeriod.TotalDays, system.RNG.NextDouble())), system.RNG.Next(0, 24), system.RNG.Next(0, 60), 0);
            // just a basic sainty check to make sure we dont end up with a planet rotating once every 3 minutes, It'd pull itself apart!!
            if (bodyInfo.LengthOfDay < TimeSpan.FromHours(_galaxyGen.Settings.MiniumPossibleDayLength))
                bodyInfo.LengthOfDay += TimeSpan.FromHours(_galaxyGen.Settings.MiniumPossibleDayLength);

            // Note that base temp does not take into account albedo or atmosphere.
            bodyInfo.BaseTemperature = (float)CalculateBaseTemperatureOfBody(star, starInfo, bodyOrbit.SemiMajorAxis + parentSMA);

            // generate Plate tectonics
            if (bodyInfo.Type != BodyType.Terrestrial && bodyInfo.Type != BodyType.Terrestrial)
            {
                bodyInfo.Tectonics = TectonicActivity.NA;  // We are not a Terrestrial body, we have no Tectonics!!!
            }
            else
            {
                bodyInfo.Tectonics = GenerateTectonicActivity(system, starInfo, bodyMVDB);
            }

            // Generate Magnetic field:
            bodyInfo.MagneticField = (float)GMath.SelectFromRange(_galaxyGen.Settings.PlanetMagneticFieldByType[bodyInfo.Type], system.RNG.NextDouble());
            if (bodyInfo.Tectonics == TectonicActivity.Dead)
                bodyInfo.MagneticField *= 0.1F; // reduce magnetic field of a dead world.

            // No radiation by default.
            bodyInfo.RadiationLevel = 0;

            // generat Minerals:
            MineralGeneration(system, body);

            // generate ruins:
            GenerateRuins(system, body);
        }

        /// <summary>
        /// Generate plate techtonics taking into consideration the mass of the planet and its age (via Star.Age).
        /// </summary>
        private TectonicActivity GenerateTectonicActivity(StarSystem system, StarInfoDB starInfo, MassVolumeDB bodyMass)
        {
            if (system.RNG.NextDouble() > _galaxyGen.Settings.TerrestrialBodyTectonicActiviyChance)
            {
                return TectonicActivity.Dead;
            }

            // this planet has some plate tectonics:
            // the following should give us a number between 0 and 1 for most bodies. Earth has a number of 0.217...
            // we converge in billion years instead of years (otherwise we get tiny numbers).
            double tectonicsChance = bodyMass.Mass / GameSettings.Units.EarthMassInKG / starInfo.Age * 100000000;
            tectonicsChance = GMath.Clamp(tectonicsChance, 0, 1);

            TectonicActivity t = TectonicActivity.NA;

            // step down the thresholds to get the correct activity:
            if (tectonicsChance < _galaxyGen.Settings.BodyTectonicsThresholds[TectonicActivity.Major])
                t = TectonicActivity.Major;
            if (tectonicsChance < _galaxyGen.Settings.BodyTectonicsThresholds[TectonicActivity.EarthLike])
                t = TectonicActivity.EarthLike;
            if (tectonicsChance < _galaxyGen.Settings.BodyTectonicsThresholds[TectonicActivity.Minor])
                t = TectonicActivity.Minor;
            if (tectonicsChance < _galaxyGen.Settings.BodyTectonicsThresholds[TectonicActivity.Dead])
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
        private void GenerateRuins(StarSystem system, Entity body)
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
            ruins.RuinSize = _galaxyGen.Settings.RuinsSizeDisrubution.Select(system.RNG.Next(0, 100));

            int quality = system.RNG.Next(0, 100);
            ruins.RuinQuality = _galaxyGen.Settings.RuinsQuilityDisrubution.Select(quality);
            if (ruins.RuinSize == RuinsDB.RSize.City && quality >= 95)
                ruins.RuinQuality = RuinsDB.RQuality.MultipleIntact;  // special case!!

            // Ruins count:
            ruins.RuinCount = (uint)GMath.SelectFromRange(_galaxyGen.Settings.RuinsCountRangeBySize[ruins.RuinSize], system.RNG.NextDouble());
            ruins.RuinCount = (uint)Math.Round(_galaxyGen.Settings.RuinsQuilityAdjustment[ruins.RuinQuality] * ruins.RuinCount);
        }

        /// <summary>
        /// This function ranomly generats minerals for a given system body. 
        /// Generation take into consideration the abundence of the mineral 
        /// and the bodies ratio of mass vs earth.
        /// </summary>
        public void MineralGeneration(StarSystem system, Entity body)
        {
            var bodyInfo = body.GetDataBlob<SystemBodyDB>();
            var bodyMass = body.GetDataBlob<MassVolumeDB>();

            // get the mass ratio for this body to earth:
            double massRatio = bodyMass.Mass / GameSettings.Units.EarthMassInKG;
            double genChance = massRatio * system.RNG.NextDouble();
            double genChanceThreshold = _galaxyGen.Settings.MineralGenerationChanceByBodyType[bodyInfo.Type];

            // now lets see if this body has minerals
            if (BodyType.Comet != bodyInfo.Type // comets always have minerals.
                && genChance < genChanceThreshold)
            {
                // check faild return:
                return;
            }

            // create some temp vars:
            double abundance;

            // this body has at least some minerals, lets generate them:
            foreach (var min in StaticDataManager.StaticDataStore.Minerals)
            {
                // create a MineralDepositInfo
                MineralDepositInfo mdi = new MineralDepositInfo();

                // get a genChance:
                abundance = min.Abundance[bodyInfo.Type];
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
        /// This function should be used when creating homewoerlds for the player race(s) or NPR Races.
        /// This function can also be used by the Space Master (not directly, but it is public for this reason).
        /// This function ensures that there is at least 50000 of every mineral and that every mineral has 
        /// an accissibility of at least 0.5.
        /// </summary>
        public void HomeworldMineralGeneration(StarSystem system, Entity body)
        {
            var bodyInfo = body.GetDataBlob<SystemBodyDB>();
            bodyInfo.Minerals.Clear();  // because this function can be called on exisiting bodies we need to clear any existing minerals.

            foreach (var min in StaticDataManager.StaticDataStore.Minerals)
            {
                // create a MineralDepositInfo
                MineralDepositInfo mdi = new MineralDepositInfo();
                mdi.Accessibility = GMath.Clamp(_galaxyGen.Settings.MinHomeworldMineralAccessibility + system.RNG.NextDouble() * min.Abundance[bodyInfo.Type], 0, 1);
                mdi.Amount = (int)Math.Round(_galaxyGen.Settings.MinHomeworldMineralAmmount + _galaxyGen.Settings.HomeworldMineralAmmount * system.RNG.NextDouble() * min.Abundance[bodyInfo.Type]);
                mdi.HalfOriginalAmount = mdi.Amount / 2;

                bodyInfo.Minerals.Add(min.ID, mdi);
            }
        }

    }
}