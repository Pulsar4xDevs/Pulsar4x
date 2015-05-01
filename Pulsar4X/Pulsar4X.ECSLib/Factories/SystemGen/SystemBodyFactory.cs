using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class SystemBodyFactory
    {
        public static Entity CreateBaseBody(StarSystem system)
        {
            var position = new PositionDB(0, 0, 0);
            var massVolume = new MassVolumeDB();
            var planetInfo = new SystemBodyDB();
            var name = new NameDB();
            var orbit = new OrbitDB();

            var planetDBs = new List<BaseDataBlob>
            {
                position,
                massVolume,
                planetInfo,
                name,
                orbit,
            };
            Entity newPlanet = Entity.Create(system.SystemManager, planetDBs);

            return newPlanet;
        }

        private static int CalcNumBodiesForStar(StarSystem system, MassVolumeDB starMassInfo, StarInfoDB starInfo)
        {
            if (system.RNG.NextDouble() > GalaxyFactory.Settings.PlanetGenerationChance)
            {
                // Star will not have planets.
                return 0;
            }

            // Mass Multiplier
            double starMassRatio = GMath.GetPercentage(starMassInfo.Mass, GalaxyFactory.Settings.StarMassBySpectralType[starInfo.SpectralType]);

            // Star type Multiplier.
            double starSpectralTypeRatio = GalaxyFactory.Settings.StarSpecralTypePlanetGenerationRatio[starInfo.SpectralType];

            // Random value.
            double randomMultiplier = system.RNG.NextDouble();

            double percentOfMax = GMath.Clamp(starMassRatio * starSpectralTypeRatio * randomMultiplier, 0, 1);

            return (int)Math.Round(percentOfMax * GalaxyFactory.Settings.MaxNoOfPlanets);
        }

        internal static void GenerateSystemBodiesForStar(StarSystem system, Entity star)
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
            if (starInfo.MinHabitableRadius > GalaxyFactory.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Max ||
                starInfo.MaxHabitableRadius < GalaxyFactory.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Min)
            {
                // Habital zone either too close or too far from star.
                // Only generating inner and outer zones.
                skipHabitableZone = true;
                innerZone = new MinMaxStruct (GalaxyFactory.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Min, GalaxyFactory.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Max * 0.5);
                habitableZone = new MinMaxStruct(starInfo.MinHabitableRadius, starInfo.MaxHabitableRadius); // Still need this for later.
                outerZone = new MinMaxStruct (GalaxyFactory.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Max * 0.5, GalaxyFactory.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Max);
            }
            else
            {
                innerZone = new MinMaxStruct(GalaxyFactory.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Min, starInfo.MinHabitableRadius);
                habitableZone = new MinMaxStruct(starInfo.MinHabitableRadius, starInfo.MaxHabitableRadius);
                outerZone = new MinMaxStruct(starInfo.MaxHabitableRadius, GalaxyFactory.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Max);
            }

            // Now generate planet numbers.
            int numInnerZoneBodies = 0;
            int numHabitableZoneBodies = 0;
            int numOuterZoneBodies = 0;

            while (numberOfBodies > 0)
            {
                // Select a band to add a body to.
                SystemBand selectedBand = GalaxyFactory.Settings.BandBodyWeight.Select(system.RNG.NextDouble());
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
                bodyCount++;
            }
        }

        private static List<Entity> GenerateBodiesForBand(StarSystem system, Entity star, SystemBand systemBand, MinMaxStruct bandLimits, int numBodies, List<Entity> systemBodies)
        {
            var bodies = new List<Entity>(numBodies);

            while (numBodies > 0)
            {
                Entity newBody = CreateBaseBody(system);

                MassVolumeDB newBodyMVDB = newBody.GetDataBlob<MassVolumeDB>();
                SystemBodyDB newBodyBodyDB = newBody.GetDataBlob<SystemBodyDB>();

                newBodyBodyDB.Type = GalaxyFactory.Settings.BandBodyTypeWeight[systemBand].Select(system.RNG.NextDouble());
                newBodyMVDB.Mass = GMath.RNG_NextDoubleRange(system.RNG, GalaxyFactory.Settings.SystemBodyMassByType[newBodyBodyDB.Type]);

                if (newBodyBodyDB.Type == BodyType.Asteroid)
                {
                    // We calculate the entire mass of the asteroid belt here.
                    // Note, this "numOfAsteroids" is not the final number. When we 
                    // finalize this asteroid belt, we'll generate asteroids until we run out of mass.
                    double noOfAsteroids = system.RNG.NextDouble() * GalaxyFactory.Settings.MaxNoOfAsteroidsPerBelt;
                    newBodyMVDB.Mass *= noOfAsteroids;
                }

                bodies.Add(newBody);
                numBodies--;
            }

            GenerateOrbitsForBodies(system, star, bodies, bandLimits, systemBodies);

            return bodies;
        }

        private static void GenerateOrbitsForBodies(StarSystem system, Entity star, List<Entity> bodies, MinMaxStruct bandLimits, List<Entity> systemBodies)
        {
            double totalBandMass = 0;

            foreach (Entity body in bodies)
            {
                MassVolumeDB bodyMVDB = body.GetDataBlob<MassVolumeDB>();

                totalBandMass += bodyMVDB.Mass;
            }

            double remainingBandMass = totalBandMass;

            double minDistance = bandLimits.Min;
            double remainingDistance = bandLimits.Max - minDistance;

            double insideApoapsis = double.MinValue;
            double outsidePeriapsis = double.MaxValue;
            double insideMass = 0;
            double outsideMass = 0;

            foreach (Entity systemBody in systemBodies)
            {
                OrbitDB bodyOrbit = systemBody.GetDataBlob<OrbitDB>();
                MassVolumeDB bodyMass = systemBody.GetDataBlob<MassVolumeDB>();

                if (bodyOrbit.Apoapsis > insideApoapsis && bodyOrbit.Apoapsis <= bandLimits.Min)
                {
                    insideApoapsis = bodyOrbit.Apoapsis;
                    insideMass = bodyMass.Mass;
                }
                else if (bodyOrbit.Periapsis < outsidePeriapsis && bodyOrbit.Periapsis >= bandLimits.Max)
                {
                    outsidePeriapsis = bodyOrbit.Periapsis;
                    outsideMass = bodyMass.Mass;
                }
            }

            for (int i = 0; i < bodies.Count; i++)
            {
                Entity currentBody = bodies[i];

                MassVolumeDB currentMVDB = currentBody.GetDataBlob<MassVolumeDB>();

                double massRatio = currentMVDB.Mass / remainingBandMass;
                double maxDistance = remainingDistance * massRatio + minDistance;

                OrbitDB currentOrbit = FindClearOrbit(system, star, currentMVDB, insideApoapsis, outsidePeriapsis, insideMass, outsideMass, minDistance, maxDistance);
                currentBody.SetDataBlob(currentOrbit);

                remainingBandMass -= currentMVDB.Mass;

                if (currentOrbit == null)
                {
                    // Failed to find a clear orbit. This body is "ejected."
                    bodies.RemoveAt(i);
                    i--;
                    continue;
                }

                insideMass = currentMVDB.Mass;
                insideApoapsis = currentOrbit.Apoapsis;
            }
        }

        private static OrbitDB FindClearOrbit(StarSystem system, Entity parent, MassVolumeDB myMVDB, double insideApoapsis, double outsidePeriapsis, double insideMass, double outsideMass, double minDistance, double maxDistance)
        {
            MassVolumeDB parentMVDB = parent.GetDataBlob<MassVolumeDB>();
            double parentMass = parentMVDB.Mass;

            // Adjust minDistance
            double gravAttractionInsiderNumerator = GameSettings.Science.GravitationalConstant * myMVDB.Mass * insideMass;
            double gravAttractionOutsideNumerator = GameSettings.Science.GravitationalConstant * myMVDB.Mass * outsideMass;
            double gravAttractionParentNumerator = GameSettings.Science.GravitationalConstant * myMVDB.Mass * parentMass;
            double gravAttractionToInsideOrbit = gravAttractionInsiderNumerator / ((minDistance - insideApoapsis) * (minDistance - insideApoapsis));
            double gravAttractionToOutisdeOrbit = gravAttractionOutsideNumerator / ((outsidePeriapsis - maxDistance) * (outsidePeriapsis - maxDistance));
            double gravAttractionToParent = gravAttractionParentNumerator / (minDistance * minDistance);

            // Make sure we're 20x more attracted to our Parent, then our inside neighbor.
            while (gravAttractionToInsideOrbit * GalaxyFactory.Settings.OrbitGravityFactor > gravAttractionToParent)
            {
                // We're too attracted to our inside neighbor, increase minDistance by 1%.
                // Assuming our parent is more massive than our inside neightbor, then this will "tip" us to be more attracted to parent.
                minDistance += minDistance * 0.01;

                // Reevaluate our gravitational attractions with new minDistance.
                gravAttractionToInsideOrbit = gravAttractionInsiderNumerator / ((minDistance - insideApoapsis) * (minDistance - insideApoapsis));
                gravAttractionToOutisdeOrbit = gravAttractionOutsideNumerator / ((outsidePeriapsis - maxDistance) * (outsidePeriapsis - maxDistance));
                gravAttractionToParent = gravAttractionParentNumerator / (minDistance * minDistance);
            }

            if (gravAttractionToOutisdeOrbit * GalaxyFactory.Settings.OrbitGravityFactor > gravAttractionToParent || minDistance > maxDistance)
            {
                // Unable to find suitable orbit. This body is rejected.
                return null;
            }

            double sma = GMath.RNG_NextDoubleRange(system.RNG, minDistance, maxDistance);

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

            // Now scale down eccentricity by a random factor.
            double eccentricity = system.RNG.NextDouble() * maxApoEccentricity;

            return new OrbitDB(parent, parentMVDB, myMVDB, sma, eccentricity, system.RNG.NextDouble() * GalaxyFactory.Settings.MaxPlanetInclination, system.RNG.NextDouble() * 360, system.RNG.NextDouble() * 360, system.RNG.NextDouble() * 360, Game.Instance.CurrentDateTime);
        }

        private static void FinalizeBodies(StarSystem system, Entity body, int bodyCount)
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
            FinalizeNameDB(system, body, bodyOrbit.Parent, bodyCount);

            GenerateMoons(system, body);

            // Recursive call to finalize children.
            int recusiveBodyCount = 1;
            foreach (Entity child in bodyOrbit.Children)
            {
                FinalizeBodies(system, child, recusiveBodyCount);
                recusiveBodyCount++;
            }
        }

        private static void FinalizeNameDB(StarSystem system, Entity body, Entity parent, int bodyCount)
        {
            // Set this body's name.
            string parentName = parent.GetDataBlob<NameDB>().Name[Entity.GetInvalidEntity()];
            string bodyName = parentName + " - " + bodyCount;
            body.GetDataBlob<NameDB>().Name.Add(Entity.GetInvalidEntity(), bodyName);
        }

        private static void FinalizeMassVolumeDB(StarSystem system, Entity body)
        {
            MassVolumeDB massVolumeDB = body.GetDataBlob<MassVolumeDB>();
            SystemBodyDB systemBodyDB = body.GetDataBlob<SystemBodyDB>();

            // Fill the MVDB.Volume property by solving from a density selection.
            massVolumeDB.Volume = MassVolumeDB.GetVolume(massVolumeDB.Mass, GMath.RNG_NextDoubleRange(system.RNG, GalaxyFactory.Settings.SystemBodyDensityByType[systemBodyDB.Type]));
        }

        private static void GenerateMoons(StarSystem system, Entity parent)
        {
            SystemBodyDB parentBodyDB = parent.GetDataBlob<SystemBodyDB>();

            // first lets see if this planet gets moons:
            if (system.RNG.NextDouble() > GalaxyFactory.Settings.MoonGenerationChanceByPlanetType[parentBodyDB.Type])
                return; // no moons for you :(

            // Okay lets work out the number of moons based on:
            // The mass of the parent in proportion to the maximum mass for a planet of that type.
            // The MaxNoOfMoonsByPlanetType
            // And a random number for randomness.
            MassVolumeDB parentMVDB = parent.GetDataBlob<MassVolumeDB>();
            double massRatioOfParent = parentMVDB.Mass / GalaxyFactory.Settings.SystemBodyMassByType[parentBodyDB.Type].Max;
            double moonGenChance = massRatioOfParent * system.RNG.NextDouble() * GalaxyFactory.Settings.MaxNoOfMoonsByPlanetType[parentBodyDB.Type];
            moonGenChance = GMath.Clamp(moonGenChance, 1, GalaxyFactory.Settings.MaxNoOfMoonsByPlanetType[parentBodyDB.Type]);
            int numMoons = (int)Math.Round(moonGenChance);

            // now we need to work out the moon type
            // we will do this by looking at the base temp of the parent.
            // if the base temp of the planet / 150K is  > 1 then it will always be terrestrial.
            // i.e. a planet hotter then GalaxyFactory.Settings.IceMoonMaximumParentTemperature will always have PlanetType.Moon.
            double tempRatio = Temperature.ToKelvin(parentBodyDB.BaseTemperature) / GalaxyFactory.Settings.IceMoonMaximumParentTemperature;

            // first pass to gen mass etc:
            var moons = new List<Entity>(numMoons);
            while (numMoons > 0)
            {
                Entity newMoon = CreateBaseBody(system);

                MassVolumeDB newMoonMVDB = newMoon.GetDataBlob<MassVolumeDB>();
                SystemBodyDB newMoonBodyDB = newMoon.GetDataBlob<SystemBodyDB>();

                newMoonBodyDB.Type = BodyType.Moon;
                newMoonMVDB.Mass = GMath.RNG_NextDoubleRange(system.RNG, GalaxyFactory.Settings.SystemBodyMassByType[newMoonBodyDB.Type]);

                moons.Add(newMoon);
            }

            double minMoonOrbitDist = parentMVDB.Radius * GalaxyFactory.Settings.MinMoonOrbitMultiplier;
            double maxMoonDistance = GalaxyFactory.Settings.MaxMoonOrbitDistanceByPlanetType[parentBodyDB.Type] * massRatioOfParent;

            GenerateOrbitsForBodies(system, parent, moons, new MinMaxStruct(minMoonOrbitDist, maxMoonDistance), new List<Entity>());
        }

        private static void FinalizeAsteroidBelt(StarSystem system, Entity body, int bodyCount)
        {
            MassVolumeDB beltMVDB = body.GetDataBlob<MassVolumeDB>();
            OrbitDB referenceOrbit = body.GetDataBlob<OrbitDB>();

            while (beltMVDB.Mass > 0)
            {
                Entity newBody = CreateBaseBody(system);
                SystemBodyDB newBodyDB = newBody.GetDataBlob<SystemBodyDB>();

                if (system.RNG.NextDouble() > (1 / GalaxyFactory.Settings.NumberOfAsteroidsPerDwarfPlanet))
                {
                    newBodyDB.Type = BodyType.Asteroid;
                }
                else
                {
                    newBodyDB.Type = BodyType.DwarfPlanet;
                }

                MassVolumeDB newBodyMVDB = newBody.GetDataBlob<MassVolumeDB>();

                newBodyMVDB.Mass = GMath.RNG_NextDoubleRange(system.RNG, GalaxyFactory.Settings.SystemBodyMassByType[newBodyDB.Type]);

                FinalizeAsteroidOrbit(system, newBody, referenceOrbit);
                FinalizeSystemBodyDB(system, newBody);
                FinalizeMassVolumeDB(system, newBody);
                FinalizeNameDB(system, newBody, referenceOrbit.Parent, bodyCount);

                beltMVDB.Mass -= newBodyMVDB.Mass;
            }
        }

        /// <summary>
        /// Generates an orbit for an Asteroid or Dwarf SystemBody. The orbit will be a slight deviation of the reference orbit provided.
        /// </summary>
        private static void FinalizeAsteroidOrbit(StarSystem system, Entity newBody, OrbitDB referenceOrbit)
        {
            // we will use the reference orbit + MaxAsteriodOrbitDeviation to constrain the orbit values:

            // Create smeiMajorAxis:
            double deviation = referenceOrbit.SemiMajorAxis * GalaxyFactory.Settings.MaxAsteroidOrbitDeviation;
            double min = referenceOrbit.SemiMajorAxis - deviation;
            double max = referenceOrbit.SemiMajorAxis + deviation;
            double semiMajorAxis = GMath.RNG_NextDoubleRange(system.RNG, min, max);  // dont need to raise to power, reference orbit already did that.

            deviation = referenceOrbit.Eccentricity * Math.Pow(GalaxyFactory.Settings.MaxAsteroidOrbitDeviation, 2);
            min = referenceOrbit.Eccentricity - deviation;
            max = referenceOrbit.Eccentricity + deviation;
            double eccentricity = GMath.RNG_NextDoubleRange(system.RNG, min, max); // get random eccentricity needs better distrubution.

            deviation = referenceOrbit.Inclination * GalaxyFactory.Settings.MaxAsteroidOrbitDeviation;
            min = referenceOrbit.Inclination - deviation;
            max = referenceOrbit.Inclination + deviation;
            double inclination = GMath.RNG_NextDoubleRange(system.RNG, min, max); // doesn't do much at the moment but may as well be there. Neet better Dist.

            deviation = referenceOrbit.ArgumentOfPeriapsis * GalaxyFactory.Settings.MaxAsteroidOrbitDeviation;
            min = referenceOrbit.ArgumentOfPeriapsis - deviation;
            max = referenceOrbit.ArgumentOfPeriapsis + deviation;
            double argumentOfPeriapsis = GMath.RNG_NextDoubleRange(system.RNG, min, max);

            deviation = referenceOrbit.LongitudeOfAscendingNode * GalaxyFactory.Settings.MaxAsteroidOrbitDeviation;
            min = referenceOrbit.LongitudeOfAscendingNode - deviation;
            max = referenceOrbit.LongitudeOfAscendingNode + deviation;
            double longitudeOfAscendingNode = GMath.RNG_NextDoubleRange(system.RNG, min, max);

            // Keep the starting point of the orbit completly random.
            double meanAnomaly = system.RNG.NextDouble() * 360;

            // now Create the orbit:
            newBody.SetDataBlob(OrbitDB.FromAsteroidFormat(referenceOrbit.Parent, referenceOrbit.Parent.GetDataBlob<MassVolumeDB>(),
                                                    newBody.GetDataBlob<MassVolumeDB>(), semiMajorAxis, eccentricity, inclination,
                                                    longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, Game.Instance.CurrentDateTime));
        }

        private static void FinalizeSystemBodyDB(StarSystem system, Entity body)
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
            bodyInfo.AxialTilt = (float)(system.RNG.NextDouble() * GalaxyFactory.Settings.MaxPlanetInclination);

            // generate the planets day length:
            //< @todo Should we do Tidaly Locked bodies??? iirc bodies trend toward being tidaly locked over time...
            bodyInfo.LengthOfDay = new TimeSpan((int)Math.Round(GMath.RNG_NextDoubleRange(system.RNG, 0, bodyOrbit.OrbitalPeriod.TotalDays)), system.RNG.Next(0, 24), system.RNG.Next(0, 60), 0);
            // just a basic sainty check to make sure we dont end up with a planet rotating once every 3 minutes, It'd pull itself apart!!
            if (bodyInfo.LengthOfDay < TimeSpan.FromHours(GalaxyFactory.Settings.MiniumPossibleDayLength))
                bodyInfo.LengthOfDay += TimeSpan.FromHours(GalaxyFactory.Settings.MiniumPossibleDayLength);

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
            bodyInfo.MagneticFeild = (float)GMath.RNG_NextDoubleRange(system.RNG, GalaxyFactory.Settings.PlanetMagneticFieldByType[bodyInfo.Type]);
            if (bodyInfo.Tectonics == TectonicActivity.Dead)
                bodyInfo.MagneticFeild *= 0.1F; // reduce magnetic field of a dead world.

            // No radiation by default.
            bodyInfo.RadiationLevel = 0;
        }

        /// <summary>
        /// Generate plate techtonics taking into consideration the mass of the planet and its age (via Star.Age).
        /// </summary>
        private static TectonicActivity GenerateTectonicActivity(StarSystem system, StarInfoDB starInfo, MassVolumeDB bodyMass)
        {
            if (system.RNG.NextDouble() > GalaxyFactory.Settings.TerrestrialBodyTectonicActiviyChance)
            {
                return TectonicActivity.Dead;
            }

            // this planet has some plate tectonics:
            // the following should give us a number between 0 and 1 for most bodies. Earth has a number of 0.217...
            // we conver age in billion years instead of years (otherwise we get tiny numbers).
            double tectonicsChance = bodyMass.Mass / GameSettings.Units.EarthMassInKG / starInfo.Age * 100000000;
            tectonicsChance = GMath.Clamp(tectonicsChance, 0, 1);

            TectonicActivity t = TectonicActivity.NA;

            // step down the thresholds to get the correct activity:
            if (tectonicsChance < GalaxyFactory.Settings.BodyTectonicsThresholds[TectonicActivity.Major])
                t = TectonicActivity.Major;
            if (tectonicsChance < GalaxyFactory.Settings.BodyTectonicsThresholds[TectonicActivity.EarthLike])
                t = TectonicActivity.EarthLike;
            if (tectonicsChance < GalaxyFactory.Settings.BodyTectonicsThresholds[TectonicActivity.Minor])
                t = TectonicActivity.Minor;
            if (tectonicsChance < GalaxyFactory.Settings.BodyTectonicsThresholds[TectonicActivity.Dead])
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
    }
}
