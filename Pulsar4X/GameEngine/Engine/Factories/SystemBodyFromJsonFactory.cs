using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Sensors;
using Pulsar4X.Extensions;
using Pulsar4X.Orbital;

namespace Pulsar4X.Engine.Factories;

public static class SystemBodyFromJsonFactory
{
    public static Entity Create(Game game, StarSystem system, Entity sun, DateTime epoch, SensorProfileDB sensorProfileDB, string filePath)
    {
        string fileContents = File.ReadAllText(filePath);
        var rootJson = JObject.Parse(fileContents);
        var info = rootJson["info"];

        var blobsToAdd = new List<BaseDataBlob>();
        var sunMassVolumeDB = sun.GetDataBlob<MassVolumeDB>();

        var nameDb = new NameDB(rootJson["name"].ToString());
        blobsToAdd.Add(nameDb);
        blobsToAdd.Add(sensorProfileDB);

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
        double semiMajorAxis_AU = Distance.KmToAU((double?)orbit["semiMajorAxis"] ?? 0);
        double eccentricity = (double?)orbit["eccentricity"] ?? 0;
        double eclipticInclination = (double?)orbit["eclipticInclination"] ?? 0;
        double loAN = (double?)orbit["LoAN"] ?? 0;
        double AoP = (double?)orbit["AoP"] ?? 0;
        double meanAnomaly = (double?)orbit["meanAnomaly"] ?? 0;

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
            case BodyType.Asteroid:
            case BodyType.Comet:
            case BodyType.Moon:
                orbitDB = OrbitDB.FromAsteroidFormat(
                    parentBody,
                    parentMassVolumeDB.MassDry,
                    massVolumeDB.MassDry,
                    semiMajorAxis_AU,
                    eccentricity,
                    eclipticInclination,
                    loAN,
                    AoP,
                    meanAnomaly,
                    epoch);
                break;
            default:
                orbitDB = OrbitDB.FromMajorPlanetFormat(
                    parentBody,
                    parentMassVolumeDB.MassDry,
                    massVolumeDB.MassDry,
                    semiMajorAxis_AU,
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
            system.Guid,
            parentBody);
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

        Entity body = Entity.Create();
        system.AddEntity(body, blobsToAdd);
        SensorTools.PlanetEmmisionSig(sensorProfileDB, systemBodyInfoDB, massVolumeDB);
        return body;
    }

    private static BodyType GetBodyType(string bodyType)
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

    private static TectonicActivity GetTectonicsType(string tectonics)
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
}