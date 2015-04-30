using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.ECSLib.DataBlobs;
using Pulsar4X.ECSLib.Helpers;

namespace Pulsar4X.ECSLib.Factories
{
    internal static class SystemBodyFactory
    {
        /// <summary>
        /// A small class to hold a system body type and mass before we have generated its orbit.
        /// </summary>
        private class ProtoSystemBody
        {
            public double Mass;
            public BodyType Type;
            public OrbitDB Orbit;
            public List<ProtoSystemBody> Children;
        }

        /// <summary>
        /// This function intigates each stage of System Body generation for a given star. 
        /// Note that most of the actual wortk is done by other functions. 
        /// </summary>
        /// <remarks>
        /// While most of the actual work for each stage is done by other functions this
        /// function is responsable for determining the nubmer of stare a given star should have
        /// and the number of asteroid belts it should have, if any at all.
        /// 
        /// Ideally the bigger the star the more planets we should generate. So we use a mass ratio 
        /// in addition to a Second Tweakable ratio to get a number we can mulitply against the 
        /// maximum number of planets to get the final number to generate. The tweakable ration is 
        /// defined in GalaxyFactory.StarSpecralTypePlanetGenerationRatio.
        /// 
        /// The generated list of protoplanets is sorted to push Terrestrial planets to the top of the list.
        /// This is done to ensure that their orbits are in the inner system.
        /// 
        /// Deciding on the number of Asteroid belts is simply done by randomly choosing a number 
        /// from 0 to GalaxyFactory.MaxNoOfAsteroidBelts. The result is the number of asteriod belts to generate.
        /// 
        /// Most of the work for stage 2, i.e. Orbits, is done by GenerateStarSystemOrbits.
        /// 
        /// Most of the work for stage 3, i.e. fleshing out the Planet details, is done by GenerateSystemBody
        /// or GenerateAsteroidBelt, depending on the SystemBody type.
        /// </remarks>
        internal static List<int> GenerateSystemBodiesForStar(Random RNG, StarSystem system, int star)
        {
            List<int> generatedBodies = new List<int>();
            EntityManager currentManager = system.SystemManager;

            // Start by determining how many SystemBodies we're going to create.
            // At this stage, an AsteroidField is considered a SystemBody, not an individual asteroid.
            if (RNG.NextDouble() > GalaxyFactory.PlanetGenerationChance)
                return generatedBodies;  // This star has no bodies. That was easy.

            MassVolumeDB starMassInfo = currentManager.GetDataBlob<MassVolumeDB>(star);
            StarInfoDB starInfo = currentManager.GetDataBlob<StarInfoDB>(star);
            NameDB starName = currentManager.GetDataBlob<NameDB>(star);

            double starMassRatio = GMath.Clamp01(starMassInfo.Mass / GameSettings.Units.SolarMassInKG);   // heavy star = more material, in relation to Sol.
            double starSpecralTypeRatio = GalaxyFactory.StarSpecralTypePlanetGenerationRatio[starInfo.SpectralType];    // tweakble

            // final 'chance' for number of planets generated. take into consideration star mass and balance decisions for star class.
            double finalGenerationChance = GMath.Clamp01(starMassRatio * starSpecralTypeRatio);

            // using the planet generation chance we will calculate the number of additional planets over and above the minium of 1. 
            int noOfBodiesToGenerate = (int)GMath.Clamp(finalGenerationChance * GalaxyFactory.MaxNoOfPlanets, 1, GalaxyFactory.MaxNoOfPlanets);

            // Now calculate the "Bands."
            // {InnerZone, HabitableZone, OuterZone}
            MinMaxStruct innerZone = new MinMaxStruct(GalaxyFactory.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Min, starInfo.MinHabitableRadius);
            MinMaxStruct habitableZone = new MinMaxStruct(starInfo.MinHabitableRadius, starInfo.MaxHabitableRadius);
            MinMaxStruct outerZone = new MinMaxStruct(starInfo.MaxHabitableRadius, GalaxyFactory.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Max);

            // Now generate planet numbers.
            int numBodiesInInnerZone = (int)GalaxyFactory.BandBodyWeight[SystemBand.InnerBand] * noOfBodiesToGenerate;
            int numBodiesInHabZone = (int)GalaxyFactory.BandBodyWeight[SystemBand.HabitableBand] * noOfBodiesToGenerate;
            int numBodiesInOuterZone = (int)GalaxyFactory.BandBodyWeight[SystemBand.OuterBand] * noOfBodiesToGenerate;

            // Generate ProtoBodies for each band.
            List<ProtoSystemBody> systemBodies = new List<ProtoSystemBody>();
            systemBodies.AddRange(GenerateProtoBodiesForBand(RNG, starMassInfo.Mass, SystemBand.HabitableBand, habitableZone, numBodiesInHabZone, systemBodies));
            systemBodies.AddRange(GenerateProtoBodiesForBand(RNG, starMassInfo.Mass, SystemBand.InnerBand, innerZone, numBodiesInInnerZone, systemBodies));
            systemBodies.AddRange(GenerateProtoBodiesForBand(RNG, starMassInfo.Mass, SystemBand.OuterBand, outerZone, numBodiesInOuterZone, systemBodies));

            int bodyNumber = 0;
            int beltNumber = 0;

            foreach (ProtoSystemBody body in systemBodies)
            {
                if (body.Type == BodyType.Asteroid)
                {
                    // Generate a whole asteroid belt.
                    generatedBodies.AddRange(GenerateAsteroidBelt(RNG, currentManager, starName, starMassInfo, starInfo, body, beltNumber));
                }
                else
                {
                    generatedBodies.AddRange(GenerateSystemBody(star, body, bodyNumber));
                }
            }

            generatedBodies.AddRange(GenerateComets(RNG, star));

            return generatedBodies;
        }

        private static List<ProtoSystemBody> GenerateProtoBodiesForBand(Random RNG, double starMass, SystemBand band, MinMaxStruct bandLimits, int numBodiesInBand, List<ProtoSystemBody> systemBodies)
        {
            // Generate masses for everything.
            double totalBandMass = 0;
            List<ProtoSystemBody> protoBodies = new List<ProtoSystemBody>();

            while (numBodiesInBand > 0)
            {
                ProtoSystemBody newBody = new ProtoSystemBody();

                newBody.Type = GalaxyFactory.BandBodyTypeWeight[band].Select(RNG.Next());
                newBody.Mass = GMath.RNG_NextDoubleRange(RNG, GalaxyFactory.SystemBodyMassByType[newBody.Type]);

                double planetarySystemMass = newBody.Mass;

                if (newBody.Type != BodyType.Asteroid)
                {
                    List<ProtoSystemBody> moons = GenerateMoonsForProtoBody(RNG, newBody);

                    foreach (ProtoSystemBody moon in moons)
                    {
                        planetarySystemMass += moon.Mass;
                        newBody.Children.Add(moon);
                    }
                }
                else
                {
                    int numAsteroids =  (int)Math.Round(RNG.NextDouble() * GalaxyFactory.MaxNoOfAsteroidsPerBelt);
                    newBody.Mass = newBody.Mass * numAsteroids;
                }

                totalBandMass += planetarySystemMass;

                protoBodies.Add(newBody);

                numBodiesInBand--;
            }

            protoBodies = GenerateBandOrbits(RNG, starMass, protoBodies, totalBandMass, bandLimits, systemBodies);

            return protoBodies;
        }

        private static List<ProtoSystemBody> GenerateMoonsForProtoBody(Random RNG, ProtoSystemBody parent)
        {
            List<ProtoSystemBody> Moons = new List<ProtoSystemBody>();

            // first lets see if this planet gets moons:
            if (RNG.NextDouble() > GalaxyFactory.MoonGenerationChanceByPlanetType[parent.Type])
                return Moons; // no moons for you :(

            // Okay lets work out the number of moons based on:
            // The mass of the parent in proportion to the maximum mass for a planet of that type.
            // The MaxNoOfMoonsByPlanetType
            // And a random number for randomness.
            double massRatioOfParent = parent.Mass / GalaxyFactory.SystemBodyMassByType[parent.Type].Max;
            double moonGenChance = massRatioOfParent * RNG.NextDouble() * GalaxyFactory.MaxNoOfMoonsByPlanetType[parent.Type];
            moonGenChance = GMath.Clamp(moonGenChance, 1, GalaxyFactory.MaxNoOfMoonsByPlanetType[parent.Type]);
            int noOfMoons = (int)Math.Round(moonGenChance);

            BodyType pt = BodyType.Moon;

            // first pass to gen mass etc:
            List<ProtoSystemBody> protoMoons = new List<ProtoSystemBody>();
            double totalMoonMass = 0;
            for (int i = 0; i < noOfMoons; ++i)
            {
                ProtoSystemBody protoMoon = new ProtoSystemBody();                      // create the proto moon

                protoMoon.Mass = GenerateMoonMass(RNG, parent, protoMoon.Type);            // Generate Mass
                totalMoonMass += protoMoon.Mass;                                       // add mass to total mass.
                protoMoons.Add(protoMoon);
            }

            double parentMaxRadius = CalculateRadiusOfBody(parent.Mass, GalaxyFactory.SystemBodyDensityByType[parent.Type].Min);
            MinMaxStruct orbitDistLimits = new MinMaxStruct();
            orbitDistLimits.Min = parentMaxRadius * GalaxyFactory.MinMoonOrbitMultiplier;
            orbitDistLimits.Max = GalaxyFactory.MaxMoonOrbitDistanceByPlanetType[parent.Type] * massRatioOfParent;

            Moons = GenerateBandOrbits(RNG, parent.Mass, protoMoons, totalMoonMass, orbitDistLimits, new List<ProtoSystemBody>());

            return Moons;
        }

        private static List<ProtoSystemBody> GenerateBandOrbits(Random RNG, double parentMass, List<ProtoSystemBody> protoBodies, double totalBandMass, MinMaxStruct bandLimits, List<ProtoSystemBody> systemBodies)
        {
            double remainingBandMass = totalBandMass;

            double minDistance = bandLimits.Min;
            double remainingDistance = bandLimits.Max - minDistance;

            double insideApoapsis = double.MinValue;
            double outsidePeriapsis = double.MaxValue;
            double insideMass = 0;
            double outsideMass = 0;

            foreach (ProtoSystemBody systemBody in systemBodies)
            {
                if (systemBody.Orbit.Apoapsis > insideApoapsis && systemBody.Orbit.Apoapsis <= bandLimits.Min)
                {
                    insideApoapsis = systemBody.Orbit.Apoapsis;
                    insideMass = systemBody.Mass;
                }
                else if (systemBody.Orbit.Periapsis < outsidePeriapsis && systemBody.Orbit.Periapsis >= bandLimits.Max)
                {
                    outsidePeriapsis = systemBody.Orbit.Periapsis;
                    outsideMass = systemBody.Mass;
                }
            }

            List<ProtoSystemBody> generatedBodies = new List<ProtoSystemBody>();

            for (int i = 0; i < protoBodies.Count; i++)
            {
                ProtoSystemBody currentProto = protoBodies[i];
                
                double massRatio = currentProto.Mass / remainingBandMass;
                double maxDistance = remainingDistance * massRatio + minDistance;

                currentProto.Orbit = FindClearOrbit(RNG, parentMass, currentProto.Mass, insideApoapsis, outsidePeriapsis, insideMass, outsideMass, minDistance, maxDistance);

                remainingBandMass -= currentProto.Mass;

                if (currentProto.Orbit == null)
                {
                    // Failed to find a clear orbit. This body is "ejected."
                    continue;
                }

                insideMass = currentProto.Mass;
                insideApoapsis = currentProto.Orbit.Apoapsis;

                generatedBodies.Add(currentProto);
            }

            return generatedBodies;
        }

        private static OrbitDB FindClearOrbit(Random RNG, double parentMass, double mass, double insideApoapsis, double outsidePeriapsis, double insideMass, double outsideMass, double minDistance, double maxDistance)
        {
            // Adjust minDistance
            double gravAttractionInsiderNumerator = GameSettings.Science.GravitationalConstant * mass * insideMass;
            double gravAttractionOutsideNumerator = GameSettings.Science.GravitationalConstant * mass * outsideMass;
            double gravAttractionParentNumerator = GameSettings.Science.GravitationalConstant * mass * parentMass;
            double gravAttractionToInsideOrbit = gravAttractionInsiderNumerator / ((minDistance - insideApoapsis) * (minDistance - insideApoapsis));
            double gravAttractionToOutisdeOrbit = gravAttractionOutsideNumerator / ((outsidePeriapsis - maxDistance) * (outsidePeriapsis - maxDistance));
            double gravAttractionToParent = gravAttractionParentNumerator / (minDistance * minDistance);

            // Make sure we're 20x more attracted to our Parent, then our inside neighbor.
            while (gravAttractionToInsideOrbit * GalaxyFactory.OrbitGravityFactor > gravAttractionToParent)
            {
                // We're too attracted to our inside neighbor, increase minDistance by 1%.
                // Assuming our parent is more massive than our inside neightbor, then this will "tip" us to be more attracted to parent.
                minDistance += minDistance * 0.01;

                // Reevaluate our gravitational attractions with new minDistance.
                gravAttractionToInsideOrbit = gravAttractionInsiderNumerator / ((minDistance - insideApoapsis) * (minDistance - insideApoapsis));
                gravAttractionToOutisdeOrbit = gravAttractionOutsideNumerator / ((outsidePeriapsis - maxDistance) * (outsidePeriapsis - maxDistance));
                gravAttractionToParent = gravAttractionParentNumerator / (minDistance * minDistance);
            }

            if (gravAttractionToOutisdeOrbit * GalaxyFactory.OrbitGravityFactor > gravAttractionToParent || minDistance > maxDistance)
            {
                // Unable to find suitable orbit. This body is rejected.
                return null;
            }

            double sma = GMath.RNG_NextDoubleRange(RNG, minDistance, maxDistance);
            double eccentricity;

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
            eccentricity = RNG.NextDouble() * maxApoEccentricity;

            OrbitDB clearOrbit = OrbitDB.FromAsteroidFormat(-1, parentMass, mass, sma, eccentricity, RNG.NextDouble() * GalaxyFactory.MaxPlanetInclination, RNG.NextDouble() * 360, RNG.NextDouble() * 360, RNG.NextDouble() * 360, Game.Instance.CurrentDateTime);

            return clearOrbit;
        }

        /// <summary>
        /// Creates planet orbits for the given system (star + proto-planets). 
        /// Not all proto-planets are guaranteed to remian in the system. 
        /// Also creates orbits for asteroid belts cause they have to happen at the same time for it all to shake out right.
        /// </summary>
        /// <param name="parent">The Parent star of the system.</param>
        /// <param name="protoPlanets">List of Proto planets, i.e. a list of planets and their type.</param>
        /// <param name="totalSystemMass">The total mass of all planets in the system.</param>
        /// <returns>List of all the planets (SystemBody) in the system. the list should be sorted from nearst to the star to farthest away.</returns>
        private static List<ProtoSystemBody> GenerateStarSystemOrbits(Random RNG, StarSystem system, int parent, List<ProtoSystemBody> protoPlanets, double totalSystemMass)
        {
            double remainingSystemMass = totalSystemMass;

            StarInfoDB parentStarInfo = system.SystemManager.GetDataBlob<StarInfoDB>(parent);
            MassVolumeDB parentBodyDB = system.SystemManager.GetDataBlob<MassVolumeDB>(parent);
            OrbitDB parentOrbit = system.SystemManager.GetDataBlob<OrbitDB>(parent);

            double minDistance = GalaxyFactory.OrbitalDistanceByStarSpectralType[parentStarInfo.SpectralType].Min;
            double remainingDistance = GalaxyFactory.OrbitalDistanceByStarSpectralType[parentStarInfo.SpectralType].Max - minDistance;
            double insideOrbitApoapsis = 0;
            double insideOrbitMass = 0;

            for (int i = 0; i < protoPlanets.Count; i++)
            {
                ProtoSystemBody currentProto = protoPlanets[i];

                double massRatio = currentProto.Mass / remainingSystemMass;
                double maxDistance = remainingDistance * massRatio + minDistance;

                currentProto.Orbit = FindClearOrbit(RNG, parent, parentBodyDB.Mass, currentProto.Mass, insideOrbitMass, insideOrbitApoapsis, minDistance, maxDistance);

                if (currentProto.Orbit.Apoapsis > GalaxyFactory.OrbitalDistanceByStarSpectralType[parentStarInfo.SpectralType].Max)
                {
                    // Planet could not fit in the system.
                    remainingSystemMass -= currentProto.Mass;
                    protoPlanets.Remove(currentProto);
                    continue;
                }

                // Prep for next loop pass.
                insideOrbitApoapsis = currentProto.Orbit.Apoapsis;
                minDistance = currentProto.Orbit.Apoapsis;
                remainingDistance = GalaxyFactory.OrbitalDistanceByStarSpectralType[parentStarInfo.SpectralType].Max - currentProto.Orbit.Apoapsis;

                insideOrbitMass = currentProto.Mass;
                remainingSystemMass -= currentProto.Mass;
            }

            return protoPlanets;
        }

        private static List<int> GenerateSystemBody(Random RNG, EntityManager currentManager, NameDB starName, MassVolumeDB starMassVolume, StarInfoDB starInfo, ProtoSystemBody body, int bodyNumber)
        {
            string bodyString = starName.Name;

            int moonNumber = 1;
            foreach (ProtoSystemBody moon in body.Children)
            {
                // Make sure to deal with all the kids.
                NameDB nameInfo = new NameDB(bodyString + "Moon " + bodyNumber + " - " + moonNumber);
                moonNumber++;
                
                double density = GMath.RNG_NextRange(RNG, GalaxyFactory.SystemBodyDensityByType[moon.Type]);
                MassVolumeDB massInfo = new MassVolumeDB(moon.Mass, density);

                SystemBodyDB bodyInfo = FinalizeSystemBody(RNG, moon, starInfo, starMassVolume.Radius, moon.Orbit, massInfo, body.Orbit.SemiMajorAxis);
                AtmosphereDB atmoInfo = GenerateAtmosphere(moon);

                List<BaseDataBlob> entityDBs = new List<BaseDataBlob>();
                entityDBs.Add(nameInfo);
                entityDBs.Add(new PositionDB(0, 0));
                entityDBs.Add(massInfo);
                entityDBs.Add(moon.Orbit);
                entityDBs.Add(bodyInfo);
                entityDBs.Add(atmoInfo);
            }
        }

        private static List<int> GenerateAsteroidBelt(Random RNG, EntityManager currentManager, NameDB starName, MassVolumeDB starMassVolume, StarInfoDB starInfo, ProtoSystemBody body, int beltNumber)
        {
            string beltString = starName.Name;
            List<int> asteroids = new List<int>();

            int asteroidNumber = 1;
            int dwarfNumber = 1;

            while (body.Mass > 0)
            { 
                ProtoSystemBody newAsteroid = new ProtoSystemBody();
                NameDB nameInfo = new NameDB(beltString);
                if (RNG.NextDouble() < 1 / GalaxyFactory.NumberOfAsteroidsPerDwarfPlanet)
                {
                    newAsteroid.Type = BodyType.DwarfPlanet;
                    nameInfo.Name +=  "Dwarf Planet " + beltNumber + " - " + dwarfNumber;
                    dwarfNumber++;
                }
                else
                {
                    newAsteroid.Type = BodyType.Asteroid;
                    nameInfo.Name += " Asteroid " + beltNumber + " - " + asteroidNumber;
                    asteroidNumber++;
                }

                newAsteroid.Mass = GMath.RNG_NextDoubleRange(RNG, GalaxyFactory.SystemBodyMassByType[newAsteroid.Type]);
                newAsteroid.Orbit = GenerateAsteroidBeltBodyOrbit(RNG, starInfo.Entity, starMassVolume.Mass, newAsteroid.Mass, body.Orbit);

                double density = GMath.RNG_NextRange(RNG, GalaxyFactory.SystemBodyDensityByType[newAsteroid.Type]);
                MassVolumeDB massVolumeInfo = new MassVolumeDB(newAsteroid.Mass, density);

                SystemBodyDB bodyInfo = FinalizeSystemBody(RNG, newAsteroid, starInfo, starMassVolume.Radius, newAsteroid.Orbit, massVolumeInfo);

                List<BaseDataBlob> entityDBs = new List<BaseDataBlob>();
                entityDBs.Add(nameInfo);
                entityDBs.Add(new PositionDB(0, 0));
                entityDBs.Add(massVolumeInfo);
                entityDBs.Add(newAsteroid.Orbit);
                entityDBs.Add(bodyInfo);

                asteroids.Add(currentManager.CreateEntity(entityDBs));

                body.Mass -= newAsteroid.Mass;
            }

            return asteroids;
        }

        /// <summary>
        /// Generates an orbit for an Asteroid or Dwarf SystemBody. The orbit will be a slight deviation of the reference orbit provided.
        /// </summary>
        private static OrbitDB GenerateAsteroidBeltBodyOrbit(Random RNG, int star, double parentMass, double mass, OrbitDB referenceOrbit)
        {
            // we will use the reference orbit + MaxAsteriodOrbitDeviation to constrain the orbit values:

            // Create smeiMajorAxis:
            double min, max, deviation;
            deviation = referenceOrbit.SemiMajorAxis * GalaxyFactory.MaxAsteroidOrbitDeviation;
            min = referenceOrbit.SemiMajorAxis - deviation;
            max = referenceOrbit.SemiMajorAxis + deviation;
            double smeiMajorAxis = GMath.RNG_NextDoubleRange(RNG, min, max);  // dont need to raise to power, reference orbit already did that.

            deviation = referenceOrbit.Eccentricity * Math.Pow(GalaxyFactory.MaxAsteroidOrbitDeviation, 2);
            min = referenceOrbit.Eccentricity - deviation;
            max = referenceOrbit.Eccentricity + deviation;
            double eccentricity = GMath.RNG_NextDoubleRange(RNG, min, max); // get random eccentricity needs better distrubution.

            deviation = referenceOrbit.Inclination * GalaxyFactory.MaxAsteroidOrbitDeviation;
            min = referenceOrbit.Inclination - deviation;
            max = referenceOrbit.Inclination + deviation;
            double inclination = GMath.RNG_NextDoubleRange(RNG, min, max); // doesn't do much at the moment but may as well be there. Neet better Dist.

            deviation = referenceOrbit.ArgumentOfPeriapsis * GalaxyFactory.MaxAsteroidOrbitDeviation;
            min = referenceOrbit.ArgumentOfPeriapsis - deviation;
            max = referenceOrbit.ArgumentOfPeriapsis + deviation;
            double argumentOfPeriapsis = GMath.RNG_NextDoubleRange(RNG, min, max);

            deviation = referenceOrbit.LongitudeOfAscendingNode * GalaxyFactory.MaxAsteroidOrbitDeviation;
            min = referenceOrbit.LongitudeOfAscendingNode - deviation;
            max = referenceOrbit.LongitudeOfAscendingNode + deviation;
            double longitudeOfAscendingNode = GMath.RNG_NextDoubleRange(RNG, min, max);

            // Keep the starting point of the orbit completly random.
            double meanAnomaly = RNG.NextDouble() * 360;

            // now Create the orbit:
            return OrbitDB.FromAsteroidFormat(star, parentMass, mass, smeiMajorAxis, eccentricity, inclination,
                                                    longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, Game.Instance.CurrentDateTime);
        }

        /*      
            // now lets generate the atmosphere:
            bodyInfo.Atmosphere = GenerateAtmosphere(body);

            // Generate Ruins, note that it will only do so for suitable planets:
            GenerateRuins(star, body);

            ///< @todo Generate Minerials Properly instead of this ugly hack:
            bodyInfo.HomeworldMineralGeneration();

            // generate moons if required for this body type:
            if (IsPlanet(bodyInfo.Type))
                GenerateMoons(star, body); 
         * 
        */

        private static SystemBodyDB FinalizeSystemBody(Random RNG, ProtoSystemBody protoBody, StarInfoDB parentStarInfo, double starRadius, OrbitDB bodyOrbit, MassVolumeDB massVolumeInfo, double parentSMA = 0)
        {
            SystemBodyDB bodyInfo = new SystemBodyDB();
            bodyInfo.Type = protoBody.Type;

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
            bodyInfo.AxialTilt = (float)(RNG.NextDouble() * GalaxyFactory.MaxPlanetInclination);

            // generate the planets day length:
            ///< @todo Should we do Tidaly Locked bodies??? iirc bodies trend toward being tidaly locked over time...
            bodyInfo.LengthOfDay = new TimeSpan((int)Math.Round(GMath.RNG_NextDoubleRange(RNG ,0, bodyOrbit.OrbitalPeriod.TotalDays)), RNG.Next(0, 24), RNG.Next(0, 60), 0);
            // just a basic sainty check to make sure we dont end up with a planet rotating once every 3 minutes, It'd pull itself apart!!
            if (bodyInfo.LengthOfDay < TimeSpan.FromHours(GalaxyFactory.MiniumPossibleDayLength))
                bodyInfo.LengthOfDay += TimeSpan.FromHours(GalaxyFactory.MiniumPossibleDayLength);

            // Note that base temp does not take into account albedo or atmosphere.
            // TODO: FIXME: Check total parent SMA.
            if (bodyInfo.Type == BodyType.Moon)
            {
                bodyInfo.BaseTemperature = (float)CalculateBaseTemperatureOfBody(parentStarInfo, starRadius, parentSMA);
            }
            bodyInfo.BaseTemperature = (float)CalculateBaseTemperatureOfBody(parentStarInfo, starRadius, bodyOrbit.SemiMajorAxis);

            // generate Plate tectonics
            if (bodyInfo.Type != BodyType.Terrestrial && bodyInfo.Type != BodyType.Terrestrial)
            {
                bodyInfo.Tectonics = TectonicActivity.NA;  // We are not a Terrestrial body, we have no Tectonics!!!
            }
            else
            {
                bodyInfo.Tectonics = GenerateTectonicActivity(RNG, parentStarInfo, massVolumeInfo);
            }

            // Generate Magnetic field:
            bodyInfo.MagneticFeild = (float)GMath.RNG_NextDoubleRange(RNG, GalaxyFactory.PlanetMagneticFieldByType[bodyInfo.Type]);
            if (bodyInfo.Tectonics == TectonicActivity.Dead)
                bodyInfo.MagneticFeild *= 0.1F; // reduce magnetic field of a dead world.

            // No radiation by default.
            bodyInfo.RadiationLevel = 0;

            return bodyInfo;
        }

        /// <summary>
        /// Generates Mass for a Moon, it makes sure that the mass of the moon will never be more then that of it's parent body.
        /// </summary>
        private static double GenerateMoonMass(Random RNG, ProtoSystemBody parent, BodyType type)
        {
            // these bodies have special mass limits over and above whats in PlanetMassByType.
            double min, max;
            min = GalaxyFactory.SystemBodyMassByType[type].Min;
            max = GalaxyFactory.SystemBodyMassByType[type].Max;

            if (max > parent.Mass * GalaxyFactory.MaxMoonMassRelativeToParentBody)
                max = parent.Mass * GalaxyFactory.MaxMoonMassRelativeToParentBody;
            if (min > max)
                min = max;      // just to make sure we get sane values.

            return GMath.RNG_NextDoubleRange(RNG, min, max);
        }

        /// <summary>
        /// Generate plate techtonics taking into consideration the mass of the planet and its age (via Star.Age).
        /// </summary>
        private static TectonicActivity GenerateTectonicActivity(Random RNG, StarInfoDB starInfo, MassVolumeDB bodyMass)
        {
            if (RNG.NextDouble() < GalaxyFactory.TerrestrialBodyTectonicActiviyChance)
            {
                // this planet has some plate tectonics:
                // the following should give us a number between 0 and 1 for most bodies. Earth has a number of 0.217...
                // we conver age in billion years instead of years (otherwise we get tiny numbers).
                double tectonicsChance = bodyMass.Mass / GameSettings.Units.EarthMassInKG / starInfo.Age * 100000000;
                tectonicsChance = GMath.Clamp01(tectonicsChance);

                TectonicActivity t = TectonicActivity.NA;

                // step down the thresholds to get the correct activity:
                if (tectonicsChance < GalaxyFactory.BodyTectonicsThresholds[TectonicActivity.Major])
                    t = TectonicActivity.Major;
                if (tectonicsChance < GalaxyFactory.BodyTectonicsThresholds[TectonicActivity.EarthLike])
                    t = TectonicActivity.EarthLike;
                if (tectonicsChance < GalaxyFactory.BodyTectonicsThresholds[TectonicActivity.Minor])
                    t = TectonicActivity.Minor;
                if (tectonicsChance < GalaxyFactory.BodyTectonicsThresholds[TectonicActivity.Dead])
                    t = TectonicActivity.Dead;

                return t;
            }

            return TectonicActivity.Dead;
        }

        /// <summary>
        /// This function generate ruins for the specified system Body.
        /// @todo Make Ruins Generation take star age/type into consideration??
        /// </summary>
        private static void GenerateRuins(Random RNG, StarSystem system, int star, int body)
        {
            // Get the data we want.
            SystemBodyDB bodyInfo = system.SystemManager.GetDataBlob<SystemBodyDB>(body);

            // first we will check that this body type can have ruins on it:
            if (bodyInfo.Type != BodyType.Terrestrial
                || bodyInfo.Type != BodyType.Moon)
            {
                return; // wrong type.
            }
            else if (bodyInfo.Atmosphere.Exists == false && (body.Atmosphere.Pressure > 2.5 || body.Atmosphere.Pressure < 0.01))
            {
                return; // no valid atmosphere!
            }
            else if (RNG.NextDouble() > 0.5)
            {
                return; // thats right... lucked out on this one.
            }

            // now if we have survived the guantlet lets gen some Ruins!!
            RuinsDB ruins = new RuinsDB();

            ruins.RuinSize = GalaxyFactory.RuinsSizeDisrubution.Select(RNG.Next(0, 100));

            int quality = RNG.Next(0, 100);
            ruins.RuinQuality = GalaxyFactory.RuinsQuilityDisrubution.Select(quality);
            if (ruins.RuinSize == Ruins.RSize.City && quality >= 95)
                ruins.RuinQuality = Ruins.RQuality.MultipleIntact;  // special case!!

            // Ruins count:
            ruins.RuinCount = GMath.RNG_NextRange(RNG, GalaxyFactory.RuinsCountRangeBySize[ruins.RuinSize]);
            ruins.RuinCount = (uint)Math.Round(GalaxyFactory.RuinsQuilityAdjustment[ruins.RuinQuality] * ruins.RuinCount);

            body.PlanetaryRuins = ruins;
        }

        /// <summary>
        /// Generates a random number of comets for a given star. The number of gererated will 
        /// be at least GalaxyGen.MiniumCometsPerSystem and never more then GalaxyGen.MaxNoOfComets.
        /// </summary>
        private static List<int> GenerateComets(Random RNG, int star)
        {
            // first lets get a random number between our minium nad maximum number of comets:
            int min = GalaxyFactory.MiniumCometsPerSystem;
            if (min > GalaxyFactory.MaxNoOfComets)
                min = GalaxyFactory.MaxNoOfComets;

            int noOfComets = RNG.Next(min, GalaxyFactory.MaxNoOfComets + 1);

            // now lets create the comets:
            for (int i = 0; i < noOfComets; ++i)
            {
                SystemBodyDB newComet = new SystemBodyDB();
                newComet.Type = BodyType.Comet;

                FinalizeSystemBodyGeneration(star, newComet);
            }
        }

        /// <summary>
        /// Calculates the radius of a body from mass and densitiy using the formular: 
        /// <c>r = ((3M)/(4pD))^(1/3)</c>
        /// Where p = PI, D = Density, and M = Mass.
        /// </summary>
        /// <param name="mass">The mass of the body in Kg</param>
        /// <param name="density">The density in g/cm^2</param>
        /// <returns>The radius in AU</returns>
        public static double CalculateRadiusOfBody(double mass, double density)
        {
            double radius = Math.Pow((3 * mass) / (4 * Math.PI * (density / 1000)), 0.3333333333); // density / 1000 changes it from g/cm2 to Kg/cm3, needed because mass in is KG. 
            // 0.3333333333 should be 1/3 but 1/3 gives radius of 0.999999 for any mass/density pair, so i used 0.3333333333
            return Distance.ToAU(radius / 1000 / 100);     // convert from cm to AU.
        }

        /// <summary>
        /// Calculates the temperature of a body given its parent star and its distance from that star.
        /// @note For info on how the Temp. is calculated see: http://en.wikipedia.org/wiki/Stefan%E2%80%93Boltzmann_law
        /// </summary>
        /// <param name="parentStar">The star this body is orbiting.</param>
        /// <param name="distanceFromStar">the SemiMojorAxis of the body with regards to the star. (i.e. for moons it is the sma of its parent planet).</param>
        /// <returns>Temperature in Degrees C</returns>
        public static double CalculateBaseTemperatureOfBody(StarInfoDB parentStar, double starRadius, double distanceFromStar)
        {
            double temp = Temperature.ToKelvin(parentStar.Temperature);
            temp = temp * Math.Sqrt(starRadius / (2 * distanceFromStar));
            return Temperature.ToCelsius(temp);
        }
    }
}
