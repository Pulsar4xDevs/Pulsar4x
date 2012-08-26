using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using Dapper;
using Pulsar4X.Entities;

namespace Pulsar4X.Storage
{
    public class CreateDatabase
    {
        private readonly string _saveDirectoryPath;
        private readonly string _dbFileName;
        private readonly string _fullFilePathName;
        private readonly string _connectionString;

        private const string CONNECTION_STRING = "Data Source={0};Version=3;";

        public CreateDatabase(string path, string dbFileName)
        {
            _saveDirectoryPath = path;
            _dbFileName = dbFileName;
            _fullFilePathName = Path.Combine(_saveDirectoryPath, _dbFileName);
            _connectionString = string.Format(CONNECTION_STRING, _fullFilePathName);
        }
        public void Save(GameState gameState)
        {
            CreateSaveFile();
            CreateTables();
            InsertData(gameState);
        }

        private void InsertData(GameState gameState)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                //save the races
                foreach (var race in gameState.Factions)
                {
                    if (race == null) continue;

                    conn.Execute("insert into Factions " +
                                 "(Id, Name, Title, SpeciesId, ThemeId) " +
                                 "values " +
                                 "(@Id, @Name, @Title, @SpeciesId, @ThemeId)", race);
                }

                foreach (var starSystem in gameState.StarSystems)
                {
                    if (starSystem == null) continue;

                    conn.Execute("insert into StarSystems " +
                                 "(Id, Name) " +
                                 "values " +
                                 "(@Id, @Name)", starSystem);
                }

                foreach (var star in gameState.Stars)
                {
                    if (star == null) continue;

                    conn.Execute("insert into Stars " +
                                 "(Id, Name, Age, Life, Luminosity, EcoSphereRadius, Mass, " +
                                 "XGalactic, YGalactic, XSystem, YSystem, StarSystemId) " +
                                 "values " +
                                 "(@Id, @Name, @Age, @Life, @Luminosity, @EcoSphereRadius, @Mass, " +
                                 "@XGalactic, @YGalactic, @XSystem, @YSystem, @StarSystemId)", star);
                }

                foreach (var planet in gameState.Planets)
                {
                    if (planet == null) continue;

                    conn.Execute("insert into Planets " +
                                 "(Id, Name, PrimaryId, XSystem, YSystem, SemiMajorAxis, Eccentricity, AxialTilt, " +
                                 "OrbitZone, OrbitalPeriod, LengthOfDay, Mass, MassOfDust, MassOfGas, RadiusOfCore, " +
                                 "Radius, Density, SurfaceArea, EscapeVelocity, SurfaceAcceleration, SurfaceGravity, " +
                                 "RootMeanSquaredVelocity, MolecularWeightRetained, VolatileGasInventory, SurfacePressure, " +
                                 "HasGreenhouseEffect, BoilingPoint, Albedo, ExoSphericTemperature, EstimatedTemperature, " +
                                 "EstimatedTerrestrialTemperature, SurfaceTemperature, RiseInTemperatureDueToGreenhouse, " +
                                 "HighTemperature, LowTemperature, MaxTemperature, MinTemperature, HydrosphereCover, " +
                                 "CloudCover, IceCover, IsGasGiant, IsMoon) " +
                                 "values " +
                                 "(@Id, @Name, @PrimaryId, @XSystem, @YSystem, @SemiMajorAxis, @Eccentricity, @AxialTilt, " +
                                 "@OrbitZone, @OrbitalPeriod, @LengthOfDay, @Mass, @MassOfDust, @MassOfGas, @RadiusOfCore, " +
                                 "@Radius, @Density, @SurfaceArea, @EscapeVelocity, @SurfaceAcceleration, @SurfaceGravity, " +
                                 "@RootMeanSquaredVelocity, @MolecularWeightRetained, @VolatileGasInventory, @SurfacePressure, " +
                                 "@HasGreenhouseEffect, @BoilingPoint, @Albedo, @ExoSphericTemperature, @EstimatedTemperature, " +
                                 "@EstimatedTerrestrialTemperature, @SurfaceTemperature, @RiseInTemperatureDueToGreenhouse, " +
                                 "@HighTemperature, @LowTemperature, @MaxTemperature, @MinTemperature, @HydrosphereCover, " +
                                 "@CloudCover, @IceCover, @IsGasGiant, @IsMoon)", planet);
                }
            }
        }

        /// <summary>
        /// Made public for testing purposes only, please use Save(GameState gameState)
        /// </summary>
        public void CreateSaveFile()
        {
            if (Directory.Exists(_saveDirectoryPath) == false)
                Directory.CreateDirectory(_saveDirectoryPath);

            //remove the old file if there is one
            if (File.Exists(_fullFilePathName))
            {
                File.Delete(_fullFilePathName);
            }

            //create our new db file
            SQLiteConnection.CreateFile(_fullFilePathName);
        }

        /// <summary>
        /// Made public for testing purposes only, please use Save(GameState gameState)
        /// </summary>
        public void CreateTables()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                //create tables for saved game
                using (var command = new SQLiteCommand(conn))
                {
                    command.CommandType = CommandType.Text;

                    foreach (var sqlStatement in CreateTableSqlStatements)
                    {
                        command.CommandText = sqlStatement;
                        command.ExecuteNonQuery();
                    }
                }
            }
        }


        private static readonly List<string> CreateTableSqlStatements = new List<string>
        {
            "CREATE TABLE Factions(" +
                "Id GUID NULL, " +
                "Name Text NULL, " +
                "Title Text NULL," +
                "SpeciesId GUID NULL, " +
                "ThemeId GUID NULL)",

            "CREATE TABLE StarSystems(" +
                "Id GUID NULL, " +
                "Name Text NULL)",

            "CREATE TABLE Stars(" +
                "Id GUID NULL, " +
                "StarSystemId GUID NULL, " +
                "Name Text NULL, " +
                "XGalactic Integer NULL, " +
                "YGalactic Integer NULL, " +
                "XSystem Integer NULL, " +
                "YSystem Integer NULL, " +
                "Luminosity real NULL, " +
                "Mass real NULL, " +
                "Life real NULL, " +
                "Age real NULL, " +
                "EcoSphereRadius real NULL)",

            "CREATE TABLE Planets(" +
                "Id GUID NULL, " +
                "PrimaryId GUID NULL, " +
                "XSystem real NULL, " +
                "YSystem real NULL, " +
                "Name Text NULL, " +
                "SemiMajorAxis real NULL, " +
                "Eccentricity real NULL, " +
                "AxialTilt real NULL, " +
                "OrbitZone int NULL, " +
                "OrbitalPeriod real NULL, " +
                "LengthOfDay real NULL, " +
                "Mass real NULL, " +
                "MassOfDust real NULL, " +
                "MassOfGas real NULL, " +
                "RadiusOfCore real NULL, " +
                "Radius real NULL, " +
                "Density real NULL, " +
                "SurfaceArea real NULL, " +
                "EscapeVelocity real NULL, " +
                "SurfaceAcceleration real NULL, " +
                "SurfaceGravity real NULL, " +
                "RootMeanSquaredVelocity real NULL, " +
                "MolecularWeightRetained real NULL, " +
                "VolatileGasInventory real NULL, " +
                "SurfacePressure real NULL, " +
                "HasGreenhouseEffect INTEGER NULL, " +
                "BoilingPoint real NULL, " +
                "Albedo real NULL, " +
                "ExoSphericTemperature real NULL, " +
                "EstimatedTemperature real NULL, " +
                "EstimatedTerrestrialTemperature real NULL, " +
                "SurfaceTemperature real NULL, " +
                "RiseInTemperatureDueToGreenhouse real NULL, " +
                "HighTemperature real NULL, " +
                "LowTemperature real NULL, " +
                "MaxTemperature real NULL, " +
                "MinTemperature real NULL, " +
                "HydrosphereCover real NULL, " +
                "CloudCover real NULL, " +
                "IceCover real NULL, " +
                "IsGasGiant INTEGER NULL, " +
                "IsMoon INTEGER NULL)"
        };


    }
}
