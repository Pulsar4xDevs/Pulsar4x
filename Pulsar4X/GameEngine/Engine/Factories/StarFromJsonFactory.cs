using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pulsar4X.Blueprints;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Sensors;

namespace Pulsar4X.Engine.Factories;

public static class StarFromJsonFactory
{
    public static Entity Create(StarSystem system, SystemGenSettingsBlueprint genSettings, string filePath)
    {
        string fileContents = File.ReadAllText(filePath);
        var rootJson = JObject.Parse(fileContents);
        var info = rootJson["info"];

        var blobsToAdd = new List<BaseDataBlob>();

        var starName = rootJson["name"].ToString();
        var spectralType = (SpectralType)Enum.Parse(typeof(SpectralType), info["spectralType"].ToString(), true);
        var luminosityClass = (LuminosityClass)Enum.Parse(typeof(LuminosityClass), info["luminosityClass"].ToString(), true);
        var luminosity = (double?)info["luminosity"] ?? 0;
        var temperature = (double?)info["temperature"] ?? 0;
        var mass = (double?)info["mass"] ?? 0;
        var radius = (double?)info["radius"] ?? 1;
        var age = (double?)info["age"] ?? 0;
        var starClass = (string?)info["class"] ?? "";

        var tempRange = temperature / genSettings.StarTemperatureBySpectralType[spectralType].Max;
        ushort subDivision = (ushort)Math.Round((1 - tempRange) * 10);
        int starIndex = system.GetAllEntitiesWithDataBlob<StarInfoDB>().Count;

        // Setup the name
        starName += " " + (char)('A' + starIndex) + " " + spectralType + subDivision + luminosityClass;

        var massVolumeDb = MassVolumeDB.NewFromMassAndRadius_m(mass, radius * 1000);
        var starInfoDb = new StarInfoDB()
        {
            Age = age,
            Class = starClass,
            Luminosity = luminosity,
            LuminosityClass = luminosityClass,
            SpectralType = spectralType,
            SpectralSubDivision = subDivision,
            Temperature = temperature
        };

        blobsToAdd.Add(new NameDB(starName));
        blobsToAdd.Add(massVolumeDb);
        blobsToAdd.Add(starInfoDb);
        blobsToAdd.Add(new PositionDB(Orbital.Vector3.Zero, system.Guid));
        blobsToAdd.Add(new OrbitDB());
        //blobsToAdd.Add(SensorTools.SetStarEmmisionSig(starInfoDb, massVolumeDb));
        blobsToAdd.Add(new VisibleByDefaultDB());


        var star = Entity.Create();
        system.AddEntity(star, blobsToAdd);
        return star;
    }
}