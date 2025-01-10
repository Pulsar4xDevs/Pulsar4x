using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Orbital;
using Pulsar4X.Blueprints;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Extensions;
using Pulsar4X.Modding;
using Pulsar4X.Industry;
using Pulsar4X.Names;
using Pulsar4X.Orbits;
using Pulsar4X.Sensors;
using Pulsar4X.Engine;
using Pulsar4X.Movement;
using System.IO;
using Newtonsoft.Json.Linq;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Colonies;

namespace Pulsar4X.Galaxy
{
    public class SystemBodyFactory
    {
        private GalaxyFactory _galaxyGen;

        public SystemBodyFactory(GalaxyFactory galaxyGen)
        {
            _galaxyGen = galaxyGen;
        }

        public static Entity CreateFromBlueprint(Game game, StarSystem system, Entity sun, DateTime epoch, SensorProfileDB sensorProfileDB, SystemBodyBlueprint systemBodyBlueprint)
        {
            var blobsToAdd = new List<BaseDataBlob>();
            var sunMassVolumeDB = sun.GetDataBlob<MassVolumeDB>();

            var nameDb = new NameDB(systemBodyBlueprint.Name);
            blobsToAdd.Add(nameDb);

            var systemBodyInfoDB = new SystemBodyInfoDB()
            {
                Gravity = systemBodyBlueprint.Info.Gravity ?? 0,
                BodyType = GetBodyType(systemBodyBlueprint.Info.Type),
                Tectonics = GetTectonicsType(systemBodyBlueprint.Info.Tectonics),
                Albedo = systemBodyBlueprint.Info.Albedo != null ? new PercentValue(systemBodyBlueprint.Info.Albedo.Value) : 0,
                AxialTilt = systemBodyBlueprint.Info.AxialTilt ?? 0,
                MagneticField = systemBodyBlueprint.Info.MagneticField ?? 0,
                BaseTemperature = systemBodyBlueprint.Info.BaseTemperature ?? 0,
                RadiationLevel = systemBodyBlueprint.Info.RadiationLevel ?? 0,
                AtmosphericDust = systemBodyBlueprint.Info.AtmosphericDust ?? 0,
                LengthOfDay = systemBodyBlueprint.Info.LengthOfDay != null ? systemBodyBlueprint.Info.LengthOfDay.Value : TimeSpan.Zero
            };
            blobsToAdd.Add(systemBodyInfoDB);

            var massVolumeDB = MassVolumeDB.NewFromMassAndRadius_AU(
                systemBodyBlueprint.Info.Mass ?? 1,
                Distance.KmToAU(systemBodyBlueprint.Info.Radius ?? 1)
            );
            blobsToAdd.Add(massVolumeDB);

            double semiMajorAxis_m = systemBodyBlueprint.Orbit.SemiMajorAxis * 1000.0 ??
                                    systemBodyBlueprint.Orbit.SemiMajorAxis_m ??
                                    systemBodyBlueprint.Orbit.SemiMajorAxis_km * 1000.0 ??
                                    systemBodyBlueprint.Orbit.SemiMajorAxis_au * UniversalConstants.Units.MetersPerAu ??
                                    0;

            double eccentricity = systemBodyBlueprint.Orbit.Eccentricity ?? 0;

            double eclipticInclination = systemBodyBlueprint.Orbit.EclipticInclination_r ??
                                        systemBodyBlueprint.Orbit.EclipticInclination_d * Math.PI/180 ??
                                        systemBodyBlueprint.Orbit.EclipticInclination * Math.PI/180 ??
                                        0;

            //flatten the inclination, we're only using inclination to define prograde vs retrograde orbits.
            //if we go to 3d orbits this section will need to be removed.
            //this is currently also flattened in OrbitEllipseBaseClass for drawing.
            eclipticInclination = Angle.NormaliseRadiansPositive(eclipticInclination);
            if (eclipticInclination > 0.5 * Math.PI && eclipticInclination < 1.5 * Math.PI)
                eclipticInclination = Math.PI;
            else
                eclipticInclination = 0;


            double loAN = systemBodyBlueprint.Orbit.LoAN_r ??
                        systemBodyBlueprint.Orbit.LoAN_d * Math.PI/180 ??
                        systemBodyBlueprint.Orbit.LoAN * Math.PI/180 ??
                        0;

            double AoP = systemBodyBlueprint.Orbit.AoP_r ??
                        systemBodyBlueprint.Orbit.AoP_d * Math.PI/180 ??
                        systemBodyBlueprint.Orbit.AoP * Math.PI/180 ??
                        0;

            double meanAnomaly = systemBodyBlueprint.Orbit.MeanAnomaly_r ??
                                systemBodyBlueprint.Orbit.MeanAnomaly_d * Math.PI/180 ??
                                systemBodyBlueprint.Orbit.MeanAnomaly * Math.PI/180 ??
                                0;

            OrbitDB orbitDB;
            var parentBody = sun;
            var parentMassVolumeDB = sunMassVolumeDB;

            if(systemBodyBlueprint.Parent != null)
            {
                parentBody = NameLookup.GetFirstEntityWithName(system, systemBodyBlueprint.Parent);
                parentMassVolumeDB = parentBody.GetDataBlob<MassVolumeDB>();
            }

            switch(systemBodyInfoDB.BodyType)
            {
                case BodyType.Comet:
                case BodyType.Asteroid:
                case BodyType.Moon:
                    orbitDB = OrbitDB.FromAsteroidFormat_r(
                        parentBody,
                        parentMassVolumeDB.MassDry,
                        massVolumeDB.MassDry,
                        semiMajorAxis_m,
                        eccentricity,
                        eclipticInclination,
                        loAN,
                        AoP,
                        meanAnomaly,
                        epoch);
                    break;
                default:
                    orbitDB = OrbitDB.FromMajorPlanetFormat_r(
                        parentBody,
                        parentMassVolumeDB.MassDry,
                        massVolumeDB.MassDry,
                        semiMajorAxis_m,
                        eccentricity,
                        eclipticInclination,
                        loAN,
                        AoP,
                        meanAnomaly,
                        epoch);
                break;
            }

            systemBodyInfoDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, orbitDB);

            var positionDB = new PositionDB(
                orbitDB.GetPosition(game.TimePulse.GameGlobalDateTime),
                parentBody);
            positionDB.MoveType = PositionDB.MoveTypes.Orbit;
            blobsToAdd.Add(positionDB);
            blobsToAdd.Add(orbitDB); // orbit needs to be added after position

            if(systemBodyBlueprint.Atmosphere != null)
            {
                SystemBodyBlueprint.AtmosphereBlueprint atmosphere = systemBodyBlueprint.Atmosphere.Value;
                var pressure = atmosphere.Pressure ?? 0;
                var gases = new Dictionary<string, float>();
                foreach(var gas in atmosphere.Gases)
                {
                    gases.Add(
                        game.GetGasBySymbol(gas.Symbol).UniqueID,
                        gas.Percent * pressure
                    );
                }

                var atmosphereDB = new AtmosphereDB(
                    pressure,
                    atmosphere.Hydrosphere ?? false,
                    atmosphere.HydroExtent ?? 0,
                    atmosphere.GreenhouseFactor ?? 0,
                    atmosphere.GreenhousePressure ?? 0,
                    atmosphere.SurfaceTemperature ?? 0,
                    gases
                );
                blobsToAdd.Add(atmosphereDB);
            }

            if(!string.IsNullOrEmpty(systemBodyBlueprint.GenerateMinerals))
            {
                MineralsDB? mineralsDB = null;
                switch(systemBodyBlueprint.GenerateMinerals)
                {
                    case "randomHW":
                        mineralsDB = MineralDepositFactory.GenerateRandomHW(game.GalaxyGen.Settings, game.StartingGameData.Minerals.Values.ToList(), system, systemBodyInfoDB, massVolumeDB);
                        break;
                    default:
                        mineralsDB = MineralDepositFactory.GenerateRandom(game.GalaxyGen.Settings, game.StartingGameData.Minerals.Values.ToList(), system, systemBodyInfoDB, massVolumeDB);
                        break;
                }

                if(mineralsDB != null) blobsToAdd.Add(mineralsDB);
            }
            else if(systemBodyBlueprint.Minerals != null)
            {
                var mineralList = new List<(int, double, double)>();
                foreach(var mineral in systemBodyBlueprint.Minerals)
                {
                    if(!game.StartingGameData.Minerals.ContainsKey(mineral.Id)) continue;

                    var mineralBlueprint = game.StartingGameData.Minerals[mineral.Id];
                    mineralList.Add((mineralBlueprint.ID, mineral.Abundance, mineral.Accessibility));
                }
                blobsToAdd.Add(MineralDepositFactory.Generate(game, mineralList, systemBodyInfoDB.BodyType));
            }

            if(systemBodyBlueprint.GeoSurveyPointsRequired != null)
            {
                var geoSurveyableDB = new GeoSurveyableDB()
                {
                    PointsRequired = systemBodyBlueprint.GeoSurveyPointsRequired ?? 1000
                };

                blobsToAdd.Add(geoSurveyableDB);
            }

            if(systemBodyBlueprint.Colonizable != null)
            {
                bool isColonizeable = systemBodyBlueprint.Colonizable ?? false;
                if(isColonizeable)
                    blobsToAdd.Add(new ColonizeableDB());
            }

            if(systemBodyInfoDB.BodyType == BodyType.Comet)
            {
                blobsToAdd.Add(sensorProfileDB);
                SensorTools.PlanetEmmisionSig(sensorProfileDB, systemBodyInfoDB, massVolumeDB);
            }
            else
            {
                blobsToAdd.Add(new VisibleByDefaultDB());
            }

            Entity body = Entity.Create();
            system.AddEntity(body, blobsToAdd);
            return body;
        }

        public static Entity Create(Game game, StarSystem system, Entity sun, DateTime epoch, SensorProfileDB sensorProfileDB, string filePath)
        {
            string fileContents = File.ReadAllText(filePath);
            var rootJson = JObject.Parse(fileContents);
            var info = rootJson["info"];

            var blobsToAdd = new List<BaseDataBlob>();
            var sunMassVolumeDB = sun.GetDataBlob<MassVolumeDB>();

            var nameDb = new NameDB(rootJson["name"].ToString());
            blobsToAdd.Add(nameDb);

            var systemBodyInfoDB = new SystemBodyInfoDB()
            {
                Gravity = (double?)info["gravity"] ?? 0,
                BodyType = info["type"] != null ? GetBodyType(info["type"].ToString()) : BodyType.Unknown,
                Tectonics = info["tectonics"] != null ? GetTectonicsType(info["tectonics"].ToString()) : TectonicActivity.Unknown,
                Albedo = info["albedo"] != null ? new PercentValue(float.Parse(info["albedo"].ToString())) : 0,
                AxialTilt = (float?)info["axialTilt"] ?? 0,
                MagneticField = (float?)info["magneticField"] ?? 0,
                BaseTemperature = (float?)info["baseTemperature"] ?? 0,
                RadiationLevel = (float?)info["radiationLevel"] ?? 0,
                AtmosphericDust = (float?)info["atmosphericDust"] ?? 0,
                LengthOfDay = info["lengthOfDay"] != null ? TimeSpan.Parse(info["lengthOfDay"].ToString()) : TimeSpan.Zero
            };
            blobsToAdd.Add(systemBodyInfoDB);

            var massVolumeDB = MassVolumeDB.NewFromMassAndRadius_AU(
                (double?)info["mass"] ?? 0,
                Distance.KmToAU((double?)info["radius"] ?? 0)
            );
            blobsToAdd.Add(massVolumeDB);

            var orbit = rootJson["orbit"];

            //double semiMajorAxis_AU = Distance.KmToAU((double?)orbit["semiMajorAxis_km"] ?? 0);

            double semiMajorAxis_m = (double?)orbit["semiMajorAxis"] * 1000.0 ??
                                    (double?)orbit["semiMajorAxis_m"] ??
                                    (double?)orbit["semiMajorAxis_km"] * 1000.0 ??
                                    (double?)orbit["semiMajorAxis_au"] * UniversalConstants.Units.MetersPerAu ??
                                    0;

            double eccentricity = (double?)orbit["eccentricity"] ?? 0;

            double eclipticInclination = (double?)orbit["eclipticInclination_r"] ??
                                        (double?)orbit["eclipticInclination_d"] * Math.PI/180 ??
                                        (double?)orbit["eclipticInclination"] * Math.PI/180 ??
                                        0;

            //flatten the inclination, we're only using inclination to define prograde vs retrograde orbits.
            //if we go to 3d orbits this section will need to be removed.
            //this is currently also flattened in OrbitEllipseBaseClass for drawing.
            eclipticInclination = Angle.NormaliseRadiansPositive(eclipticInclination);
            if (eclipticInclination > 0.5 * Math.PI && eclipticInclination < 1.5 * Math.PI)
                eclipticInclination = Math.PI;
            else
                eclipticInclination = 0;


            double loAN = (double?)orbit["LoAN_r"] ??
                        (double?)orbit["LoAN_d"] * Math.PI/180 ??
                        (double?)orbit["LoAN"] * Math.PI/180 ??
                        0;

            double AoP = (double?)orbit["AoP_r"] ??
                        (double?)orbit["AoP_d"] * Math.PI/180 ??
                        (double?)orbit["AoP"] * Math.PI/180 ??
                        0;

            double meanAnomaly = (double?)orbit["meanAnomaly_r"] ??
                                (double?)orbit["meanAnomaly_d"] * Math.PI/180 ??
                                (double?)orbit["meanAnomaly"] * Math.PI/180 ??
                                0;

            OrbitDB orbitDB;
            var parentBody = sun;
            var parentMassVolumeDB = sunMassVolumeDB;

            if(rootJson["parent"] != null)
            {
                parentBody = NameLookup.GetFirstEntityWithName(system, rootJson["parent"].ToString());
                parentMassVolumeDB = parentBody.GetDataBlob<MassVolumeDB>();
            }

            switch(systemBodyInfoDB.BodyType)
            {
                case BodyType.Comet:
                case BodyType.Asteroid:
                case BodyType.Moon:
                    orbitDB = OrbitDB.FromAsteroidFormat_r(
                        parentBody,
                        parentMassVolumeDB.MassDry,
                        massVolumeDB.MassDry,
                        semiMajorAxis_m,
                        eccentricity,
                        eclipticInclination,
                        loAN,
                        AoP,
                        meanAnomaly,
                        epoch);
                    break;
                default:
                    orbitDB = OrbitDB.FromMajorPlanetFormat_r(
                        parentBody,
                        parentMassVolumeDB.MassDry,
                        massVolumeDB.MassDry,
                        semiMajorAxis_m,
                        eccentricity,
                        eclipticInclination,
                        loAN,
                        AoP,
                        meanAnomaly,
                        epoch);
                break;
            }

            systemBodyInfoDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, orbitDB);

            var positionDB = new PositionDB(
                orbitDB.GetPosition(game.TimePulse.GameGlobalDateTime),
                parentBody);
            positionDB.MoveType = PositionDB.MoveTypes.Orbit;
            blobsToAdd.Add(positionDB);
            blobsToAdd.Add(orbitDB); // orbit needs to be added after position

            if(rootJson["atmosphere"] != null)
            {
                var atmosphere = rootJson["atmosphere"];
                var pressure = (float?)atmosphere["pressure"] ?? 0;
                var gasesJson = (JArray?)atmosphere["gases"];
                var gases = new Dictionary<string, float>();
                foreach(var gas in gasesJson)
                {
                    string symbol = gas["symbol"].ToString();
                    float percent = (float?)gas["percent"] ?? 0;
                    gases.Add(
                        game.GetGasBySymbol(symbol).UniqueID,
                        percent * pressure
                    );
                }

                var atmosphereDB = new AtmosphereDB(
                    pressure,
                    (bool?)atmosphere["hydrosphere"] ?? false,
                    (decimal?)atmosphere["hydroExtent"] ?? 0,
                    (float?)atmosphere["greenhouseFactor"] ?? 0,
                    (float?)atmosphere["greenhousePressure"] ?? 0,
                    (float?)atmosphere["surfaceTemperature"] ?? 0,
                    gases
                );
                blobsToAdd.Add(atmosphereDB);
            }

            if(rootJson["minerals"] != null)
            {
                MineralsDB? mineralsDb = null;
                JToken? mineralToken = rootJson["minerals"];
                if(mineralToken.Type == JTokenType.String)
                {
                    var value = (string?)rootJson["minerals"] ?? "";

                    if(value.Equals("random"))
                    {
                        mineralsDb = MineralDepositFactory.GenerateRandom(game.GalaxyGen.Settings, game.StartingGameData.Minerals.Values.ToList(), system, systemBodyInfoDB, massVolumeDB);
                    }
                    if(value.Equals("randomHW"))
                    {
                        mineralsDb = MineralDepositFactory.GenerateRandomHW(game.GalaxyGen.Settings, game.StartingGameData.Minerals.Values.ToList(), system, systemBodyInfoDB, massVolumeDB);
                    }
                }
                else if(mineralToken.Type == JTokenType.Array)
                {
                    var mineralList = new List<(int, double, double)>();
                    var minerals = (JArray?)rootJson["minerals"];
                    foreach(var mineral in minerals)
                    {
                        var id = (string?)mineral["id"] ?? "";
                        var abundance = (double?)mineral["abundance"] ?? 0.1;
                        var accessibility = (double?)mineral["accessibility"] ?? 0.1;

                        if(!game.StartingGameData.Minerals.ContainsKey(id)) continue;

                        var mineralBlueprint = game.StartingGameData.Minerals[id];
                        mineralList.Add((mineralBlueprint.ID, abundance, accessibility));
                    }

                    mineralsDb = MineralDepositFactory.Generate(game, mineralList, systemBodyInfoDB.BodyType);
                }

                if(mineralsDb != null)
                {
                    blobsToAdd.Add(mineralsDb);
                }
            }

            if(rootJson["geoSurvey"] != null)
            {
                var geoSurveyableDB = new GeoSurveyableDB()
                {
                    PointsRequired = (uint?)rootJson["geoSurvey"]["pointsRequired"] ?? 1000
                };

                blobsToAdd.Add(geoSurveyableDB);
            }

            if(rootJson["colonizeable"] != null)
            {
                bool isColonizeable = (bool?)rootJson["colonizeable"] ?? false;
                if(isColonizeable)
                    blobsToAdd.Add(new ColonizeableDB());
            }

            if(systemBodyInfoDB.BodyType == BodyType.Comet)
            {
                blobsToAdd.Add(sensorProfileDB);
                SensorTools.PlanetEmmisionSig(sensorProfileDB, systemBodyInfoDB, massVolumeDB);
            }
            else
            {
                blobsToAdd.Add(new VisibleByDefaultDB());
            }

            Entity body = Entity.Create();
            system.AddEntity(body, blobsToAdd);
            return body;
        }

        private static BodyType GetBodyType(string? bodyType)
        {
            return bodyType switch
            {
                "terrestrial" => BodyType.Terrestrial,
                "gas-giant" => BodyType.GasGiant,
                "ice-giant" => BodyType.IceGiant,
                "dwarf-planet" => BodyType.DwarfPlanet,
                "gas-dwarf" => BodyType.GasDwarf,
                "moon" => BodyType.Moon,
                "asteroid" => BodyType.Asteroid,
                "comet" => BodyType.Comet,
                _ => BodyType.Unknown
            };
        }

        private static TectonicActivity GetTectonicsType(string? tectonics)
        {
            return tectonics switch
            {
                "earth-like" => TectonicActivity.EarthLike,
                "dead" => TectonicActivity.Dead,
                "minor" => TectonicActivity.Minor,
                "major" => TectonicActivity.Major,
                "na" => TectonicActivity.NA,
                _ => TectonicActivity.Unknown
            };
        }

        /// <summary>
        /// Creates an uninitialized body in the specified system.
        /// </summary>
        public static ProtoEntity CreateBaseBody()
        {
            var position = new PositionDB(Vector3.Zero);
            position.MoveType = PositionDB.MoveTypes.Orbit;
            var massVolume = new MassVolumeDB();
            var planetInfo = new SystemBodyInfoDB();
            var minerals = new MineralsDB();
            var name = new NameDB("ProtoBody");
            //var orbit = new OrbitDB();
            var atmo = new AtmosphereDB();
            var ruins = new RuinsDB();
            var emEmtor = new SensorProfileDB();
            var planetDBs = new List<BaseDataBlob>
            {
                position,
                massVolume,
                planetInfo,
                minerals,
                name,
                //orbit,
                atmo,
                ruins,
                emEmtor
            };

            return new ProtoEntity(planetDBs);
        }

        /// <summary>
        /// Calculates the number of bodies this star will have.
        /// </summary>
        private int CalcNumBodiesForStar(StarSystem system, MassVolumeDB starMassInfo, StarInfoDB starInfo)
        {
            var rngVal = system.RNGNextDouble();
            if (rngVal > _galaxyGen.Settings.PlanetGenerationChance)
            {
                // Star will not have planets.
                return 0;
            }

            // Mass Multiplier
            double starMassRatio = GeneralMath.GetPercentage(starMassInfo.MassDry, _galaxyGen.Settings.StarMassBySpectralType[starInfo.SpectralType]);

            // Star type Multiplier.
            double starSpectralTypeRatio = _galaxyGen.Settings.StarSpectralTypePlanetGenerationRatio[starInfo.SpectralType];

            // Random value.
            double randomMultiplier = system.RNGNextDouble();

            double percentOfMax = GeneralMath.Clamp(starMassRatio * starSpectralTypeRatio * randomMultiplier, 0, 1);

            return (int)Math.Round(percentOfMax * _galaxyGen.Settings.MaxNoOfPlanets);
        }

        /// <summary>
        /// Generate all bodies for the specified star.
        /// </summary>
        internal void GenerateSystemBodiesForStar(ModDataStore dataStore, StarSystem system, Entity star, DateTime currentDateTime)
        {
            // Get required info from the star.
            MassVolumeDB starMassInfo = star.GetDataBlob<MassVolumeDB>();
            StarInfoDB starInfo = star.GetDataBlob<StarInfoDB>();

            // Calculate number of system bodies to generate.
            int numberOfBodies = CalcNumBodiesForStar(system, starMassInfo, starInfo);

            if (numberOfBodies == 0)
                return;

            // Now calculate the "Bands."
            // MinMaxStruct innerZone_m;
            // MinMaxStruct habitableZone_m;
            // MinMaxStruct outerZone_m;

            var zones = HabitibleZones(_galaxyGen.Settings, starInfo);
            bool skipHabitableZone = !zones.hasHabitible;

            // Now generate planet numbers.
            int numInnerZoneBodies = 0;
            int numHabitableZoneBodies = 0;
            int numOuterZoneBodies = 0;

            while (numberOfBodies > 0)
            {
                // Select a band to add a body to.
                var rngVal = system.RNGNextDouble();
                SystemBand selectedBand = _galaxyGen.Settings.BandBodyWeight.Select(rngVal);
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

            systemBodies.AddRange(GenerateBodiesForBand(system, star, SystemBand.HabitableBand, zones.habitible, numHabitableZoneBodies, systemBodies, currentDateTime));
            systemBodies.AddRange(GenerateBodiesForBand(system, star, SystemBand.InnerBand, zones.inner, numInnerZoneBodies, systemBodies, currentDateTime));
            systemBodies.AddRange(GenerateBodiesForBand(system, star, SystemBand.OuterBand, zones.outer, numOuterZoneBodies, systemBodies, currentDateTime));

            // Finalize all bodies that were actually added to the star.
            int bodyCount = 1;
            foreach (ProtoEntity protoBody in systemBodies)
            {
                Entity body = system.CreateAndAddEntity(protoBody);
                FinalizeBodies(dataStore, system, body, bodyCount, currentDateTime);
                bodyCount++;
            }

            // Finally, comets!
            GenerateComets(dataStore, system, star, currentDateTime);
        }

        public static (MinMaxStruct inner, MinMaxStruct habitible, MinMaxStruct outer, bool hasHabitible) HabitibleZones(SystemGenSettingsBlueprint settings, StarInfoDB starInfo)
        {

            MinMaxStruct innerZone_m;
            MinMaxStruct habitableZone_m;
            MinMaxStruct outerZone_m;
            var zoneMin_m = settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Min;
            var zoneMax_m = settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType].Max;
            bool skipHabitableZone = false;
            if (starInfo.MinHabitableRadius_m > zoneMax_m ||
                starInfo.MaxHabitableRadius_m < zoneMin_m)
            {
                // Habitable zone either too close or too far from star.
                // Only generating inner and outer zones.
                skipHabitableZone = true;

                innerZone_m = new MinMaxStruct(zoneMin_m, zoneMax_m * 0.5);
                habitableZone_m = new MinMaxStruct(starInfo.MinHabitableRadius_m, starInfo.MaxHabitableRadius_m); // Still need this for later.
                outerZone_m = new MinMaxStruct(zoneMax_m * 0.5, zoneMax_m);
            }
            else
            {
                innerZone_m = new MinMaxStruct(zoneMin_m, starInfo.MinHabitableRadius_m);
                habitableZone_m = new MinMaxStruct(starInfo.MinHabitableRadius_m, starInfo.MaxHabitableRadius_m);
                outerZone_m = new MinMaxStruct(starInfo.MaxHabitableRadius_m, zoneMax_m);
            }

            return (innerZone_m, habitableZone_m, outerZone_m, !skipHabitableZone);
        }

        /// <summary>
        /// Generates a random number of comets for a given star. The number of generated will
        /// be at least GalaxyGen.MiniumCometsPerSystem and never more then GalaxyGen.MaxNoOfComets.
        /// </summary>
        private void GenerateComets(ModDataStore dataStore, StarSystem system, Entity star, DateTime currentDateTime)
        {
            // first lets get a random number between our minium nad maximum number of comets:
            int min = _galaxyGen.Settings.MiniumCometsPerSystem;
            if (min > _galaxyGen.Settings.MaxNoOfComets)
                min = _galaxyGen.Settings.MaxNoOfComets;

            int noOfComets = system.RNGNext(min, _galaxyGen.Settings.MaxNoOfComets + 1);

            // now lets create the comets:
            for (int i = 0; i < noOfComets; ++i)
            {
                NameDB starName = star.GetDataBlob<NameDB>();

                ProtoEntity newCometProto = CreateBaseBody();
                NameDB cometName = newCometProto.GetDataBlob<NameDB>();
                cometName.SetName(-1, starName.DefaultName + " - Comet " + (i + 1));

                SystemBodyInfoDB cometBodyDB = newCometProto.GetDataBlob<SystemBodyInfoDB>();
                cometBodyDB.BodyType = BodyType.Comet;

                MassVolumeDB cometMVDB = MassVolumeDB.NewFromMassAndDensity(
                    GeneralMath.Lerp(_galaxyGen.Settings.SystemBodyMassByType[BodyType.Comet], system.RNGNextDouble()),
                    GeneralMath.Lerp(_galaxyGen.Settings.SystemBodyDensityByType[BodyType.Comet], system.RNGNextDouble()));
                newCometProto.DataBlobs.Add(cometMVDB);

                GenerateCometOrbit(system, star, newCometProto, currentDateTime);

                FinalizeSystemBodyDB(dataStore, system, newCometProto);

                var comet = system.CreateAndAddEntity(newCometProto);
                var pos = comet.GetDataBlob<PositionDB>();
                pos.SetParent(comet.GetDataBlob<OrbitDB>().Parent);
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

            double semiMajorAxis = GeneralMath.Lerp(_galaxyGen.Settings.OrbitalDistanceByStarSpectralType[starInfo.SpectralType], system.RNGNextDouble());
            double eccentricity = GeneralMath.Lerp(_galaxyGen.Settings.BodyEccentricityByType[BodyType.Comet], system.RNGNextDouble());
            double inclination = system.RNGNextDouble() * _galaxyGen.Settings.MaxBodyInclination;
            double longitudeOfAscendingNode = system.RNGNextDouble() * 2 * Math.PI;
            double argumentOfPeriapsis = system.RNGNextDouble() * 2 * Math.PI;
            double meanAnomaly = system.RNGNextDouble() * 2 * Math.PI;

            var orbitDB = new OrbitDB(star, starMVDB.MassDry, cometMVDB.MassDry, semiMajorAxis, eccentricity, inclination, longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, currentDateTime);
            comet.SetDataBlob(orbitDB);
        }

        /// <summary>
        /// Generates the bodies for the specified SystemBand.
        /// This allows us to tweak how many habitable/inner/outer bodies there are.
        /// </summary>
        /// <param name="system">System we're working with.</param>
        /// <param name="star">Star we're generating for.</param>
        /// <param name="systemBand">Enum specifying which band we're working in.</param>
        /// <param name="bandLimits_m">MinMax structure representing the distance this band represents. in meters.</param>
        /// <param name="numBodies">Number of bodies to try to generate in this band.</param>
        /// <param name="systemBodies">List of systemBodies already present. Required for Orbit generation.</param>
        private List<ProtoEntity> GenerateBodiesForBand(StarSystem system, Entity star, SystemBand systemBand, MinMaxStruct bandLimits_m, int numBodies, List<ProtoEntity> systemBodies, DateTime currentDateTime)
        {
            List<ProtoEntity> bodies = new List<ProtoEntity>(numBodies);

            int numAsteroidBelts = 0;

            // Generate basic bodies with types and masses.
            while (numBodies > 0)
            {
                ProtoEntity newBody = CreateBaseBody();

                double massMultiplyer = 1; // Later we do some multiplication.
                SystemBodyInfoDB newBodyBodyDB = newBody.GetDataBlob<SystemBodyInfoDB>();

                newBodyBodyDB.BodyType = _galaxyGen.Settings.GetBandBodyTypeWeight(systemBand).Select(system.RNGNextDouble());


                if (newBodyBodyDB.BodyType == BodyType.Asteroid)
                {
                    if (numAsteroidBelts == _galaxyGen.Settings.MaxNoOfAsteroidBelts)
                    {
                        // Max number of belts reach. Reroll until we've got... not an asteroid belt.
                        while (newBodyBodyDB.BodyType == BodyType.Asteroid)
                        {
                            newBodyBodyDB.BodyType = _galaxyGen.Settings.GetBandBodyTypeWeight(systemBand).Select(system.RNGNextDouble());
                        }
                    }
                    else
                    {
                        // We calculate the entire mass of the asteroid belt here.
                        // Note, this "numOfAsteroids" is not the final number. When we
                        // finalize this asteroid belt, we'll generate asteroids until we run out of mass.
                        double noOfAsteroids = system.RNGNextDouble() * _galaxyGen.Settings.MaxNoOfAsteroidsPerBelt;
                        massMultiplyer = noOfAsteroids;
                    }
                }

                // generate Mass volume DB in full here, to avoid problems later:
                double density;
                if (newBodyBodyDB.BodyType == BodyType.Asteroid)
                {
                    // Mass multiplication here. This allows us to set the mass to the correct value for both asteroid belts and other bodies.
                    massMultiplyer *= GeneralMath.Lerp(_galaxyGen.Settings.SystemBodyMassByType[newBodyBodyDB.BodyType], system.RNGNextDouble());  // cache final mass in massMultiplyer.
                    var minMaxDensity = _galaxyGen.Settings.SystemBodyDensityByType[newBodyBodyDB.BodyType];
                    density = (minMaxDensity.Min + minMaxDensity.Max) / 2.0;
                }
                else
                {
                    massMultiplyer *= GeneralMath.Lerp(_galaxyGen.Settings.SystemBodyMassByType[newBodyBodyDB.BodyType], Math.Pow(system.RNGNextDouble(), 3)); // cache mass, alos cube random nuber to make smaller bodies more likly.
                    density = GeneralMath.Lerp(_galaxyGen.Settings.SystemBodyDensityByType[newBodyBodyDB.BodyType], system.RNGNextDouble());
                }

                var mvDB = MassVolumeDB.NewFromMassAndDensity(massMultiplyer, density);
                newBody.SetDataBlob(mvDB);

                bodies.Add(newBody);
                numBodies--;
            }

            // Generate the orbits for the bodies.
            // bodies list may be modified.
            // If a body cannot be put into a sane orbit, it is removed.
            GenerateOrbitsForBodies(system, star, ref bodies, bandLimits_m, systemBodies, currentDateTime);

            return bodies;
        }

        public static Entity GenerateSingleBody(SystemGenSettingsBlueprint settings, StarSystem system, Entity parent, BodyType type, double radius )
        {
            var parentstar = parent;
            var starInfo = parent.GetDataBlob<StarInfoDB>();
            int heirarchyDepth = 0;
            double bandRadius = radius;
            while (starInfo == null)
            {
                heirarchyDepth++;
                parentstar = parent.GetSOIParentEntity();
                starInfo = parentstar.GetDataBlob<StarInfoDB>();

            }
            //if we're orbiting something, then the parents position from the sun is going tobe the average distance from the sun
            //this kinda breaks in multi star systems...
            if(heirarchyDepth > 0)
                bandRadius = MoveMath.GetAbsoluteFuturePosition(parent, system.StarSysDateTime).Length();

            var zones = HabitibleZones(settings, starInfo);
            MinMaxStruct zone;
            SystemBand band;
            if (zones.hasHabitible && bandRadius > zones.habitible.Min && bandRadius < zones.habitible.Max)
            {
                zone = zones.habitible;
                band = SystemBand.HabitableBand;
            }
            else if (bandRadius < zones.inner.Max && bandRadius > zones.inner.Min)
            {
                zone = zones.inner;
                band = SystemBand.InnerBand;
            }
            else if (bandRadius > zones.outer.Min && bandRadius < zones.outer.Max)
            {
                zone = zones.outer;
                band = SystemBand.OuterBand;
            }
            else throw new Exception("bad radius");

            var bodyType = settings.GetBandBodyTypeWeight(band).Select(system.RNGNextDouble());
            var newBody = CreateBaseBody();
            SystemBodyInfoDB newBodyBodyDB = newBody.GetDataBlob<SystemBodyInfoDB>();


            // generate Mass volume DB in full here, to avoid problems later:
            double density;
            double mass;
            if (newBodyBodyDB.BodyType == BodyType.Asteroid)
            {
                // Mass multiplication here. This allows us to set the mass to the correct value for both asteroid belts and other bodies.
                mass = GeneralMath.Lerp(settings.SystemBodyMassByType[newBodyBodyDB.BodyType], system.RNGNextDouble());  // cache final mass in massMultiplyer.
                var minMaxDensity = settings.SystemBodyDensityByType[newBodyBodyDB.BodyType];
                density = (minMaxDensity.Min + minMaxDensity.Max) / 2.0;
            }
            else
            {
                mass = GeneralMath.Lerp(settings.SystemBodyMassByType[newBodyBodyDB.BodyType], Math.Pow(system.RNGNextDouble(), 3)); // cache mass, alos cube random nuber to make smaller bodies more likly.
                density = GeneralMath.Lerp(settings.SystemBodyDensityByType[newBodyBodyDB.BodyType], system.RNGNextDouble());
            }

            var mvDB = MassVolumeDB.NewFromMassAndDensity(mass, density);
            newBody.SetDataBlob(mvDB);
            Entity body = system.CreateAndAddEntity(newBody);

            var positionDB = body.GetDataBlob<PositionDB>();
            positionDB.SetParent(body.GetDataBlob<OrbitDB>().Parent);
            positionDB.AbsolutePosition = body.GetDataBlob<OrbitDB>().GetPosition(parent.StarSysDateTime);

            return body;


        }

        /// <summary>
        /// Places passed bodies into a sane orbit around the parent.
        /// </summary>
        /// <param name="system">System we're working with.</param>
        /// <param name="parent">Parent entity we're working with.</param>
        /// <param name="bodies">List of bodies to place into orbit. May be modified if bodies cannot be placed in a sane orbit.</param>
        /// <param name="bandLimits_m">MinMax structure representing the distance limits for the orbit.</param>
        /// <param name="systemBodies">List of bodies already orbiting this parent. We work around these.</param>
        private void GenerateOrbitsForBodies(StarSystem system, Entity parent, ref List<ProtoEntity> bodies, MinMaxStruct bandLimits_m, List<ProtoEntity> systemBodies, DateTime currentDateTime)
        {
            double totalBandMass = 0;

            // Calculate the total mass of bodies we must place into orbit.
            foreach (ProtoEntity body in bodies)
            {
                MassVolumeDB bodyMVDB = body.GetDataBlob<MassVolumeDB>();
                totalBandMass += bodyMVDB.MassDry;
            }

            // Prepare the loop variables.
            double remainingBandMass = totalBandMass;

            double minDistance_m = bandLimits_m.Min; // The minimum distance we can place a body.
            double remainingDistance_m = bandLimits_m.Max - minDistance_m; // distance remaining that we can place bodies into.

            double insideApoapsis_m = double.MinValue; // Apoapsis of the orbit that is inside of the next body.
            double outsidePeriapsis_m = double.MaxValue; // Periapsis of the orbit that is outside of the next body.
            double insideMass = 0; // Mass of the object that is inside of the next body.
            double outsideMass = 0; // Mass of the object that is outside of the next body.

            // Find the inside and outside bodies.
            foreach (ProtoEntity systemBody in systemBodies)
            {
                OrbitDB bodyOrbit = systemBody.GetDataBlob<OrbitDB>();
                MassVolumeDB bodyMass = systemBody.GetDataBlob<MassVolumeDB>();

                // Find if the current systemBody is within the bandLimit
                // and is in a higher orbit than the previous insideOrbit.
                if (bodyOrbit.Apoapsis <= bandLimits_m.Min && bodyOrbit.Apoapsis > insideApoapsis_m)
                {
                    insideApoapsis_m = bodyOrbit.Apoapsis;
                    insideMass = bodyMass.MassDry;
                }
                // Otherwise, find if the current systemBody is within the bandLimit
                // and is in a lower orbit than the previous outsideOrbit.
                else if (bodyOrbit.Periapsis >= bandLimits_m.Max && bodyOrbit.Periapsis < outsidePeriapsis_m)
                {
                    outsidePeriapsis_m = bodyOrbit.Periapsis;
                    outsideMass = bodyMass.MassDry;
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
                double massRatio = currentMVDB.MassDry / remainingBandMass;
                double maxDistance_m = remainingDistance_m * massRatio + minDistance_m;
                // We'll either find this orbit, or eject it, so this body is no longer part of remaining mass.
                remainingBandMass -= currentMVDB.MassDry;

                // Do the heavy math to find the orbit.
                OrbitDB? currentOrbit = FindClearOrbit(system, parent, currentBody, insideApoapsis_m, outsidePeriapsis_m, insideMass, outsideMass, minDistance_m, maxDistance_m, currentDateTime);

                if (currentOrbit == null)
                {
                    // Failed to find a clear orbit. This body is "ejected."
                    bodies.RemoveAt(i);
                    i--; // Keep i at the right spot.
                    continue;
                }
                currentBody.SetDataBlob(currentOrbit);

                insideMass = currentMVDB.MassDry;
                insideApoapsis_m = Distance.MToAU(currentOrbit.Apoapsis);
            }
        }

        /// <summary>
        /// Finds a gravitationally stable orbit between the insideApoapsis and the outsidePeriapsis for a body.
        /// </summary>
        private OrbitDB? FindClearOrbit(StarSystem system, Entity parent, ProtoEntity body, double insideApoapsis, double outsidePeriapsis, double insideMass, double outsideMass, double minDistance, double maxDistance, DateTime currentDateTime)
        {
            MassVolumeDB parentMVDB = parent.GetDataBlob<MassVolumeDB>();
            double parentMass = parentMVDB.MassDry;

            MassVolumeDB myMVDB = body.GetDataBlob<MassVolumeDB>();
            double myMass = myMVDB.MassDry;

            // Adjust minDistance
            double gravAttractionInsiderNumerator = UniversalConstants.Science.GravitationalConstant * myMass * insideMass;
            double gravAttractionOutsideNumerator = UniversalConstants.Science.GravitationalConstant * myMass * outsideMass;
            double gravAttractionParentNumerator = UniversalConstants.Science.GravitationalConstant * myMass * parentMass;
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

            double sma_m =  GeneralMath.Lerp(minDistance, maxDistance, system.RNGNextDouble());

            // Calculate max eccentricity.
            // First calc max eccentricity for the apoapsis.
            double maxApoEccentricity = (maxDistance - sma_m) / sma_m;
            // Now calc max eccentricity for periapsis.
            double minPeriEccentricity = -((minDistance - sma_m) / sma_m);

            // Use the smaller value.
            if (minPeriEccentricity < maxApoEccentricity)
            {
                // We use maxApoEccentricity in next calc.
                maxApoEccentricity = minPeriEccentricity;
            }

            // Enforce GalaxyFactory settings.
            MinMaxStruct eccentricityMinMax = _galaxyGen.Settings.BodyEccentricityByType[body.GetDataBlob<SystemBodyInfoDB>().BodyType];
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
            double eccentricity = GeneralMath.Lerp(eccentricityMinMax, system.RNGNextDouble());

            return new OrbitDB(parent, parentMass, myMass, sma_m, eccentricity, system.RNGNextDouble() * _galaxyGen.Settings.MaxBodyInclination, system.RNGNextDouble() * 2 * Math.PI, system.RNGNextDouble() * 2 * Math.PI, system.RNGNextDouble() * 2 * Math.PI, currentDateTime);
        }

        private void FinalizeBodies(ModDataStore dataStore, StarSystem system, Entity body, int bodyCount, DateTime currentDateTime)
        {
            OrbitDB bodyOrbit = body.GetDataBlob<OrbitDB>();
            SystemBodyInfoDB systemBodyDB = body.GetDataBlob<SystemBodyInfoDB>();

            if (systemBodyDB.BodyType == BodyType.Asteroid)
            {
                FinalizeAsteroidBelt(dataStore, system, body, bodyCount);
                return;
            }

            FinalizeSystemBodyDB(dataStore, system, body);
            FinalizeNameDB(body, bodyOrbit.Parent, bodyCount);

            // Finalize Orbit
            var positionDB = body.GetDataBlob<PositionDB>();
            positionDB.SetParent(body.GetDataBlob<OrbitDB>().Parent);
            positionDB.AbsolutePosition = body.GetDataBlob<OrbitDB>().GetPosition(currentDateTime);

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
                    FinalizeBodies(dataStore, system, child, recursiveBodyCount, currentDateTime);
                    recursiveBodyCount++;
                }
            }
        }

        private static void FinalizeNameDB(IHasDataBlobs body, Entity? parent, int bodyCount, string suffix = "")
        {
            // Set this body's name.
            string parentName = parent.GetDataBlob<NameDB>().DefaultName;
            string bodyName = parentName + " - " + bodyCount + suffix;
            body.GetDataBlob<NameDB>().SetName(-1, bodyName);
        }

        private void GenerateMoons(StarSystem system, Entity parent, DateTime currentDateTime)
        {
            // BUG: Moons are currently taking a large ratio of mass compared to parents, and when formed on GasGiants can be extremely large.
            SystemBodyInfoDB parentBodyDB = parent.GetDataBlob<SystemBodyInfoDB>();

            // first lets see if this planet gets moons:
            if (system.RNGNextDouble() > _galaxyGen.Settings.MoonGenerationChanceByPlanetType[parentBodyDB.BodyType])
                return; // no moons for you :(

            // Okay lets work out the number of moons based on:
            // The mass of the parent in proportion to the maximum mass for a planet of that type.
            // The MaxNoOfMoonsByPlanetType
            // And a random number for randomness.
            MassVolumeDB parentMVDB = parent.GetDataBlob<MassVolumeDB>();
            double massRatioOfParent = parentMVDB.MassDry / _galaxyGen.Settings.SystemBodyMassByType[parentBodyDB.BodyType].Max;
            double moonGenChance = massRatioOfParent * system.RNGNextDouble() * _galaxyGen.Settings.MaxNoOfMoonsByPlanetType[parentBodyDB.BodyType];
            moonGenChance = GeneralMath.Clamp(moonGenChance, 1, _galaxyGen.Settings.MaxNoOfMoonsByPlanetType[parentBodyDB.BodyType]);
            int numMoons = (int)Math.Round(moonGenChance);

            // first pass to gen mass etc:
            var moons = new List<ProtoEntity>(numMoons);
            while (numMoons > 0)
            {
                ProtoEntity newMoon = CreateBaseBody();
                SystemBodyInfoDB newMoonBodyDB = newMoon.GetDataBlob<SystemBodyInfoDB>();

                newMoonBodyDB.BodyType = BodyType.Moon;

                // Enforce GalaxyFactory mass limits.
                MinMaxStruct moonMassMinMax = _galaxyGen.Settings.SystemBodyMassByType[newMoonBodyDB.BodyType];
                double maxRelativeMass = parentMVDB.MassDry * _galaxyGen.Settings.MaxMoonMassRelativeToParentBody;
                if (maxRelativeMass < moonMassMinMax.Max)
                {
                    moonMassMinMax.Max = maxRelativeMass;
                }

                MassVolumeDB newMoonMVDB = MassVolumeDB.NewFromMassAndDensity(
                    GeneralMath.Lerp(moonMassMinMax, system.RNGNextDouble()),
                    GeneralMath.Lerp(_galaxyGen.Settings.SystemBodyDensityByType[BodyType.Moon], system.RNGNextDouble()));
                newMoon.SetDataBlob(newMoonMVDB);

                moons.Add(newMoon);
                numMoons--;
            }

            double minMoonOrbitDist = parentMVDB.RadiusInM * _galaxyGen.Settings.MinMoonOrbitMultiplier;
            double maxMoonDistance = _galaxyGen.Settings.MaxMoonOrbitDistanceByPlanetType[parentBodyDB.BodyType] * massRatioOfParent;

            GenerateOrbitsForBodies(system, parent, ref moons, new MinMaxStruct(minMoonOrbitDist, maxMoonDistance), new List<ProtoEntity>(), currentDateTime);

            // create proper entities:
            foreach (var moon in moons)
            {
                var realMoon = system.CreateAndAddEntity(moon);
                var pos = realMoon.GetDataBlob<PositionDB>();
                pos.SetParent(realMoon.GetDataBlob<OrbitDB>().Parent);
            }
        }

        private void FinalizeAsteroidBelt(ModDataStore dataStore, StarSystem system, Entity body, int bodyCount)
        {
            MassVolumeDB beltMVDB = body.GetDataBlob<MassVolumeDB>();
            OrbitDB referenceOrbit = body.GetDataBlob<OrbitDB>();

            int asteroidCount = 1;
            while (beltMVDB.MassDry > 0)
            {
                ProtoEntity newProtoBody = CreateBaseBody();
                Entity newBody = system.CreateAndAddEntity(newProtoBody);
                SystemBodyInfoDB newBodyDB = newBody.GetDataBlob<SystemBodyInfoDB>();

                if (system.RNGNextDouble() > (1.0 / _galaxyGen.Settings.NumberOfAsteroidsPerDwarfPlanet))
                {
                    newBodyDB.BodyType = BodyType.Asteroid;
                }
                else
                {
                    newBodyDB.BodyType = BodyType.DwarfPlanet;
                }

                MassVolumeDB mvDB = MassVolumeDB.NewFromMassAndDensity(
                    GeneralMath.Lerp(_galaxyGen.Settings.SystemBodyMassByType[newBodyDB.BodyType], system.RNGNextDouble()),
                    GeneralMath.Lerp(_galaxyGen.Settings.SystemBodyDensityByType[newBodyDB.BodyType], system.RNGNextDouble()));
                newBody.SetDataBlob(mvDB);

                FinalizeAsteroidOrbit(system, newBody, referenceOrbit);
                FinalizeSystemBodyDB(dataStore, system, newBody);
                FinalizeNameDB(newBody, referenceOrbit.Parent, bodyCount, "-A" + asteroidCount.ToString());

                beltMVDB.MassDry -= mvDB.MassDry;
                asteroidCount++;
            }
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
            double deviation = _galaxyGen.Settings.MaxAsteroidOrbitDeviation;

			// Creates orbital parameters by multiplying referenceOrbit
            // parameters by a value between +/- MaxAsteroidOrbitDeviation
            // of the reference parameter
            double semiMajorAxis = Distance.MToAU(referenceOrbit.SemiMajorAxis) *
                (1 + GeneralMath.Lerp(-deviation, deviation, system.RNGNextDouble()));  // don't need to raise to power, reference orbit already did that.
            double eccentricity = referenceOrbit.Eccentricity *
                (1 + GeneralMath.Lerp(-deviation, deviation, system.RNGNextDouble())); // get random eccentricity needs better distribution.
            double inclination = referenceOrbit.Inclination *
                (1 + GeneralMath.Lerp(-deviation, deviation, system.RNGNextDouble())); // doesn't do much at the moment but may as well be there. Need better Dist.
            double argumentOfPeriapsis = referenceOrbit.ArgumentOfPeriapsis *
                (1 + GeneralMath.Lerp(-deviation, deviation, system.RNGNextDouble()));
            double longitudeOfAscendingNode = referenceOrbit.LongitudeOfAscendingNode *
				(1 + GeneralMath.Lerp(-deviation, deviation, system.RNGNextDouble()));

            // Keep the starting point of the orbit completely random.
            double meanAnomaly = system.RNGNextDouble() * 360;

            // now Create the orbit:
            MassVolumeDB parentMVDB = referenceOrbit.Parent.GetDataBlob<MassVolumeDB>();
            MassVolumeDB myMVDB = newBody.GetDataBlob<MassVolumeDB>();
            OrbitDB newOrbit = OrbitDB.FromAsteroidFormat(referenceOrbit.Parent, parentMVDB.MassDry, myMVDB.MassDry, semiMajorAxis, eccentricity, inclination,
                                                    longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, _galaxyGen.Settings.J2000);
            newBody.SetDataBlob(newOrbit);
            newBody.GetDataBlob<PositionDB>().SetParent(newOrbit.Parent);
        }

        /// <summary>
        /// This function puts all the finishing touiches on a system body data blob.
        /// </summary>
        private void FinalizeSystemBodyDB(ModDataStore dataStore, StarSystem system, IHasDataBlobs body)
        {
            SystemBodyInfoDB bodyInfo = body.GetDataBlob<SystemBodyInfoDB>();
            OrbitDB bodyOrbit = body.GetDataBlob<OrbitDB>();
            MassVolumeDB bodyMVDB = body.GetDataBlob<MassVolumeDB>();

            Entity? parent = bodyOrbit.Parent;
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

            switch (bodyInfo.BodyType)
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
            bodyInfo.AxialTilt = (float)(system.RNGNextDouble() * _galaxyGen.Settings.MaxBodyInclination);

            // generate the planets day length:
            //< @todo Should we do Tidally Locked bodies??? iirc bodies trend toward being tidally locked over time...
            bodyInfo.LengthOfDay = new TimeSpan((int)Math.Round(GeneralMath.Lerp(0, bodyOrbit.OrbitalPeriod.TotalDays, system.RNGNextDouble())), system.RNGNext(0, 24), system.RNGNext(0, 60), 0);
            // just a basic sanity check to make sure we don't end up with a planet rotating once every 3 minutes, It'd pull itself apart!!
            if (bodyInfo.LengthOfDay < TimeSpan.FromHours(_galaxyGen.Settings.MiniumPossibleDayLength))
                bodyInfo.LengthOfDay += TimeSpan.FromHours(_galaxyGen.Settings.MiniumPossibleDayLength);

            // Note that base temp does not take into account albedo or atmosphere.
            bodyInfo.BaseTemperature = (float)CalculateBaseTemperatureOfBody(star, starInfo, bodyOrbit.SemiMajorAxis + parentSMA);

            // generate Plate tectonics
            if (bodyInfo.BodyType == BodyType.Terrestrial)
                bodyInfo.Tectonics = GenerateTectonicActivity(system, starInfo, bodyMVDB);
            else
                bodyInfo.Tectonics = TectonicActivity.NA;  // We are not a Terrestrial body, we have no Tectonics!!!

            // Generate Magnetic field, must be done before atmosphere:
            bodyInfo.MagneticField = (float)GeneralMath.Lerp(_galaxyGen.Settings.PlanetMagneticFieldByType[bodyInfo.BodyType], system.RNGNextDouble());
            if (bodyInfo.Tectonics == TectonicActivity.Dead)
                bodyInfo.MagneticField *= 0.1F; // reduce magnetic field of a dead world.

            // Generate atmosphere:
            GenerateAtmosphere(system, body, dataStore);

            // No radiation by default.
            bodyInfo.RadiationLevel = 0;

            // generat Minerals:
            MineralGeneration(dataStore.Minerals.Values.ToList(), system, body);

            // generate ruins:
            GenerateRuins(system, body);

            var profile = body.GetDataBlob<SensorProfileDB>();
            var atmo = body.GetDataBlob<AtmosphereDB>();
            SensorTools.PlanetEmmisionSig(profile, bodyInfo, bodyMVDB);
        }

        /// <summary>
        /// Generate plate techtonics taking into consideration the mass of the planet and its age (via Star.Age).
        /// </summary>
        private TectonicActivity GenerateTectonicActivity(StarSystem system, StarInfoDB starInfo, MassVolumeDB bodyMass)
        {
            if (system.RNGNextDouble() > _galaxyGen.Settings.TerrestrialBodyTectonicActivityChance)
            {
                return TectonicActivity.Dead;
            }

            // this planet has some plate tectonics:
            // the following should give us a number between 0 and 1 for most bodies. Earth has a number of 0.217...
            // we converge in billion years instead of years (otherwise we get tiny numbers).
            double tectonicsChance = bodyMass.MassDry / UniversalConstants.Units.EarthMassInKG / starInfo.Age * 100000000;
            tectonicsChance = GeneralMath.Clamp(tectonicsChance, 0, 1);

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
        /// <param name="distanceFromStar"> in meters</param>
        /// <returns>Temperature in Degrees C</returns>
        public static double CalculateBaseTemperatureOfBody(Entity star, StarInfoDB starInfo, double distanceFromStar)
        {
            MassVolumeDB starMVDB = star.GetDataBlob<MassVolumeDB>();
            double temp = Temperature.ToKelvin(starInfo.Temperature);
            temp = temp * Math.Sqrt(starMVDB.RadiusInM / (2 * distanceFromStar));
            return Temperature.ToCelsius(temp);
        }

        /// <summary>
        /// calculates the temprature of a planet using a time averaged distance from the star. NOTE! orbitDB needs to be a body to sun orbit!
        /// </summary>
        /// <param name="star"></param>
        /// <param name="orbit">must be a body to sun orbit, ie for the moon, use planet's orbitDB</param>
        /// <returns>temprature in degrees C</returns>
        public static double CalculateBaseTemperatureOfBody(Entity star, OrbitDB orbit)
        {
            StarInfoDB starInfoDB = star.GetDataBlob<StarInfoDB>();
            //https://cosmicreflections.skythisweek.info/2017/11/15/average-orbital-distance/
            //time averaged distance = r = a(1+ e^2/2)
            double averageDistanceFromStar = orbit.SemiMajorAxis * (1 + Math.Pow(orbit.Eccentricity, 2) / 2);
            return CalculateBaseTemperatureOfBody(star, starInfoDB, averageDistanceFromStar);

        }

        /// <summary>
        /// This function generate ruins for the specified system Body.
        /// @todo Make Ruins Generation take star age/type into consideration??
        /// </summary>
        private void GenerateRuins(StarSystem system, IHasDataBlobs body)
        {
            // cache some DBs:
            var atmo = body.GetDataBlob<AtmosphereDB>();
            var bodyType = body.GetDataBlob<SystemBodyInfoDB>().BodyType;
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
            else if (system.RNGNextDouble() > _galaxyGen.Settings.RuinsGenerationChance)
            {
                return; // that's right... lucked out on this one.
            }

            // now if we have survived the gauntlet lets gen some Ruins!!
            ruins.RuinSize = _galaxyGen.Settings.RuinsSizeDistribution.Select(system.RNGNext(0, 100));

            int quality = system.RNGNext(0, 100);
            ruins.RuinQuality = _galaxyGen.Settings.RuinsQualityDistribution.Select(quality);
            if (ruins.RuinSize == RuinsDB.RSize.City && quality >= 95)
                ruins.RuinQuality = RuinsDB.RQuality.MultipleIntact;  // special case!!

            // Ruins count:
            ruins.RuinCount = (uint)GeneralMath.Lerp(_galaxyGen.Settings.RuinsCountRangeBySize[ruins.RuinSize], system.RNGNextDouble());
            ruins.RuinCount = (uint)Math.Round(_galaxyGen.Settings.RuinsQualityAdjustment[ruins.RuinQuality] * ruins.RuinCount);
        }

        /// <summary>
        /// This function randomly generats minerals for a given system body.
        /// Generation take into consideration the abundance of the mineral
        /// and the bodies ratio of mass vs planet.
        /// </summary>
        public void MineralGeneration(List<Mineral> minerals, StarSystem system, IHasDataBlobs body)
        {
            var bodyInfo = body.GetDataBlob<SystemBodyInfoDB>();
            var bodyMass = body.GetDataBlob<MassVolumeDB>();
            body.TryGetDatablob<MineralsDB>(out var mineralInfo);

            // get the mass ratio for this body to planet:
            double massRatio = bodyMass.MassDry / UniversalConstants.Units.EarthMassInKG;
            double genChance = massRatio * system.RNGNextDouble();
            double genChanceThreshold = _galaxyGen.Settings.MineralGenerationChanceByBodyType[bodyInfo.BodyType];

            // now lets see if this body has minerals
            if (BodyType.Comet != bodyInfo.BodyType // comets always have minerals.
                && genChance < genChanceThreshold)
            {
                // check failed return:
                return;
            }

            if(mineralInfo == null)
            {
                body.SetDataBlob(new MineralsDB());
                mineralInfo = body.GetDataBlob<MineralsDB>();
            }

            // this body has at least some minerals, lets generate them:
            foreach (var min in minerals)
            {
                // create a MineralDepositInfo
                MineralDeposit mdi = new MineralDeposit();

                // get a genChance:
                double abundance = min.Abundance[bodyInfo.BodyType];
                genChance = massRatio * system.RNGNextDouble() * abundance;

                if (genChance >= genChanceThreshold)
                {
                    mdi.Accessibility = GeneralMath.Clamp(_galaxyGen.Settings.MinMineralAccessibility + genChance, 0, 1);
                    mdi.Amount = (long)Math.Round(_galaxyGen.Settings.MaxMineralAmmountByBodyType[bodyInfo.BodyType] * genChance);
                    mdi.HalfOriginalAmount = mdi.Amount / 2;

                    if (!mineralInfo.Minerals.ContainsKey(min.ID))
                    {
                        mineralInfo.Minerals.Add(min.ID, mdi);
                    }
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
        public void HomeworldMineralGeneration(List<Mineral> minerals, StarSystem system, Entity body)
        {
            var bodyInfo = body.GetDataBlob<SystemBodyInfoDB>();
            body.TryGetDatablob<MineralsDB>(out var mineralInfo);

            if(mineralInfo == null)
            {
                body.SetDataBlob<MineralsDB>(new());
                mineralInfo = body.GetDataBlob<MineralsDB>();
            }

            mineralInfo.Minerals.Clear();  // because this function can be called on existing bodies we need to clear any existing minerals.

            foreach (var min in minerals)
            {
                // create a MineralDepositInfo
                MineralDeposit mdi = new MineralDeposit
                {
                    Accessibility = GeneralMath.Clamp(_galaxyGen.Settings.MinHomeworldMineralAccessibility + system.RNGNextDouble() * min.Abundance[bodyInfo.BodyType], 0, 1),
                    Amount = (long)Math.Round(_galaxyGen.Settings.MinHomeworldMineralAmmount + _galaxyGen.Settings.HomeworldMineralAmmount * system.RNGNextDouble() * min.Abundance[bodyInfo.BodyType])
                };
                mdi.HalfOriginalAmount = mdi.Amount / 2;

                mineralInfo.Minerals.Add(min.ID, mdi);
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
        public void GenerateAtmosphere(StarSystem system, IHasDataBlobs body, ModDataStore dataStore)
        {
            var atmoDB = body.GetDataBlob<AtmosphereDB>();
            if (atmoDB == null)
                return; // no atmosphere for this body.

            SystemBodyInfoDB bodyDB = body.GetDataBlob<SystemBodyInfoDB>();
            MassVolumeDB mvDB = body.GetDataBlob<MassVolumeDB>();
            OrbitDB orbit = body.GetDataBlob<OrbitDB>();

            // Set Albeado (all bodies have an albedo):
            bodyDB.Albedo = (float)GeneralMath.Lerp(_galaxyGen.Settings.PlanetAlbedoByType[bodyDB.BodyType], system.RNGNextDouble());

            // Atmo modifer is used to determine how thick the atmosphere should be, higher = thicker.
            double atmoModifer = _galaxyGen.Settings.AtmosphereGenerationModifier[bodyDB.BodyType] * (mvDB.MassDry / UniversalConstants.Units.EarthMassInKG);
            double atmoGenChance = GeneralMath.Clamp(atmoModifer, 0, 1); // used to detmine if we should haver an atmosphere at all.

            if (atmoGenChance > system.RNGNextDouble())
            {
                // we can generate an atmosphere!
                // first we should decide how thick it should be:
                double newATM = GenAtmosphereThickness(mvDB.MassDry, bodyDB, orbit, atmoModifer, system.RNGNextDouble());

                // set an initial surface temp to the base temp, adjusted for albedo:
                atmoDB.SurfaceTemperature = atmoDB.SurfaceTemperature = bodyDB.BaseTemperature * (1 - bodyDB.Albedo);

                // now we will want to select gasses for the atmosphere:
                SelectGases(newATM, atmoModifer, bodyDB, mvDB, atmoDB, system, dataStore);
            }

            // finally Run the atmo processor over it to create the greenhous factors and descriptions etc.
            // We want to run this even for bodies without an atmosphere.
            AtmosphereProcessor.UpdateAtmosphere(atmoDB, bodyDB);

            // Add hydrospher if terra like world that has an atmosphere:
            if ((bodyDB.BodyType == BodyType.Terrestrial || bodyDB.BodyType == BodyType.Terrestrial) && atmoDB.Exists)
            {
                if (system.RNGNextDouble() > 0.75)
                {
                    atmoDB.Hydrosphere = true;
                    atmoDB.HydrosphereExtent = (short)(system.RNGNextDouble() * 100);
                }
            }
        }

        /// <summary>
        /// Works out how thick the atmosphere for the body should be, returns the value in atm.
        /// </summary>
        double GenAtmosphereThickness(double bodyMass, SystemBodyInfoDB body, OrbitDB orbit,  double atmoModifer, double randomModifer)
        {
            switch (body.BodyType)
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
                    // this will produce 1 atm for planet like planets, less for smaller planets, more for larger:
                    double massRatio = (bodyMass / UniversalConstants.Units.EarthMassInKG);
                    double atm = massRatio * massRatio * atmoModifer;

                    // now we have a nice starting atm, lets modify it:
                    // first we will reduce it if the planet is closer to the star, increase it if it is further away using the ewchosphere of the star:
                    StarInfoDB starInfo;
                    double ecosphereRatio = 1;
                    if (body.BodyType == BodyType.Moon)
                    {
                        // if moon get planet orbit, then star
                        var parentOrbitDB = orbit.ParentDB as OrbitDB;
                        starInfo = parentOrbitDB.Parent.GetDataBlob<StarInfoDB>();
                        ecosphereRatio = (parentOrbitDB.SemiMajorAxis / starInfo.EcoSphereRadius_m);
                    }
                    else
                    {
                        // if planet get star:
                        starInfo = orbit.Parent.GetDataBlob<StarInfoDB>();
                        ecosphereRatio = GeneralMath.Clamp(orbit.SemiMajorAxis / starInfo.EcoSphereRadius_m, 0.1, 2);
                    }

                    atm = atm * ecosphereRatio;  // if inside eco sphere this will reduce atmo, increase it if outside.

                    // now we will see if this planet should be venus like pressure cooker:
                    // if the planet is very close it will
                    double inverseEchoshpereRatio = 1 - (GeneralMath.Clamp(ecosphereRatio, 0, 1));
                    if (randomModifer < _galaxyGen.Settings.RunawayGreenhouseEffectChance * inverseEchoshpereRatio)
                    {
                        atm *= _galaxyGen.Settings.RunawayGreenhouseEffectMultiplyer;
                    }
                    else
                    {
                        // if we arn't a pressure cooker planet, then lets modify the atmosphere pressure according to the magnetic feild:
                        double magneticFieldRatio = body.MagneticField / _galaxyGen.Settings.PlanetMagneticFieldByType[body.BodyType].Max;
                        atm *= magneticFieldRatio;
                    }

                    // finally clamp the atmosphere to a resonable value:
                    return GeneralMath.Clamp(atm, _galaxyGen.Settings.MinMaxAtmosphericPressure.Min, _galaxyGen.Settings.MinMaxAtmosphericPressure.Max);
            }
        }

        /// <summary>
        /// Selects suitable gases to make up an atmosphere.
        /// </summary>
        void SelectGases(double atm, double atmoModifer, SystemBodyInfoDB body, MassVolumeDB bodyMassDB, AtmosphereDB atmoDB, StarSystem system, ModDataStore dataStore)
        {
            // get the gas list:
            var gases = new WeightedList<GasBlueprint>();

            // creat a gas list for this planet, it should not include any gases that are too light or which would boil away due to too high temp.
            foreach (var possibleGas in dataStore.AtmosphericGas)
            {
                if (Temperature.ToKelvin(possibleGas.Value.BoilingPoint) * 5 > Temperature.ToKelvin(body.BaseTemperature))
                {
                    // then it is too cold for this gas to boil off
                    if (possibleGas.Value.MinGravity < bodyMassDB.SurfaceGravity)
                    {
                        gases.Add(possibleGas.Value.Weight, possibleGas.Value);
                    }
                }
            }

            // do a quick safty check:
            if (gases.Count() < 2)
                return; // bail on selecting gases, we don't have enough!!

            // get the primary gass:
            double percentage = 0.6 + 0.3 * system.RNGNextDouble();
            var gas = gases.Select(system.RNGNextDouble());
            atmoDB.Composition.Add(gas.UniqueID, (float)(percentage * atm));
            gases.Remove(gas);

            // get the secondary gas:
            percentage = 0.98 - percentage;
            gas = gases.Select(system.RNGNextDouble());
            atmoDB.Composition.Add(gas.UniqueID, (float)(percentage * atm));
            gases.Remove(gas);

            // get the trace gases, note that we will not care so much about
            int noOfTraceGases = (int)GeneralMath.Clamp(Math.Round(5 * atmoModifer), 1, 5);

            // do another quick safty check:
            if (gases.Count() < noOfTraceGases + 1)
                return; // bail, not enough gases left to select trace gases.

            double remainingPercentage = 0.02;
            percentage = 0;
            for (int i = 0; i < noOfTraceGases + 1; ++i)
            {
                percentage = (remainingPercentage - percentage) * system.RNGNextDouble();  // just use random numbers, it will be close enough.
                gas = gases.Select(system.RNGNextDouble());
                atmoDB.Composition.Add(gas.UniqueID, (float)(percentage * atm));
                gases.Remove(gas);
            }
        }
    }
}