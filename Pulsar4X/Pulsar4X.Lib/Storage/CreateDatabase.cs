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
                foreach (var race in gameState.Races)
                {
                    if (race == null) continue;

                    conn.Execute("insert into Races " +
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
            "CREATE TABLE Races(" +
                "Id GUID NOT NULL, " +
                "Name Text NOT NULL, " +
                "Title Text NOT NULL," +
                "SpeciesId GUID NOT NULL, " +
                "ThemeId GUID NOT NULL)",

            "CREATE TABLE StarSystems(" +
                "Id GUID NOT NULL, " +
                "Name Text NOT NULL)",

            "CREATE TABLE Stars(" +
                "Id GUID NOT NULL, " +
                "StarSystemId GUID NOT NULL, " +
                "Name Text NOT NULL, " +
                "XGalactic Integer NOT NULL, " +
                "YGalactic Integer NOT NULL, " +
                "XSystem Integer NOT NULL, " +
                "YSystem Integer NOT NULL, " +
                "Luminosity real NULL, " +
                "Mass real NULL, " +
                "Life real NULL, " +
                "Age real NULL, " +
                "EcoSphereRadius real NULL)",

            "CREATE TABLE Planets(" +
                "Id GUID NOT NULL, " +
                "PrimaryId GUID NOT NULL, " +
                "XSystem real NOT NULL, " +
                "YSystem real NOT NULL, " +
                "Name Text NOT NULL, " +
                "SemiMajorAxis real NOT NULL, " +
                "Eccentricity real NOT NULL, " +
                "AxialTilt real NOT NULL, " +
                "OrbitZone int NOT NULL, " +
                "OrbitalPeriod real NOT NULL, " +
                "LengthOfDay real NOT NULL, " +
                "Mass real NOT NULL, " +
                "MassOfDust real NOT NULL, " +
                "MassOfGas real NOT NULL, " +
                "RadiusOfCore real NOT NULL, " +
                "Radius real NOT NULL, " +
                "Density real NOT NULL, " +
                "SurfaceArea real NOT NULL, " +
                "EscapeVelocity real NOT NULL, " +
                "SurfaceAcceleration real NOT NULL, " +
                "SurfaceGravity real NOT NULL, " +
                "RootMeanSquaredVelocity real NOT NULL, " +
                "MolecularWeightRetained real NOT NULL, " +
                "VolatileGasInventory real NOT NULL, " +
                "SurfacePressure real NOT NULL, " +
                "HasGreenhouseEffect INTEGER NOT NULL, " +
                "BoilingPoint real NOT NULL, " +
                "Albedo real NOT NULL, " +
                "ExoSphericTemperature real NOT NULL, " +
                "EstimatedTemperature real NOT NULL, " +
                "EstimatedTerrestrialTemperature real NOT NULL, " +
                "SurfaceTemperature real NOT NULL, " +
                "RiseInTemperatureDueToGreenhouse real NOT NULL, " +
                "HighTemperature real NOT NULL, " +
                "LowTemperature real NOT NULL, " +
                "MaxTemperature real NOT NULL, " +
                "MinTemperature real NOT NULL, " +
                "HydrosphereCover real NOT NULL, " +
                "CloudCover real NOT NULL, " +
                "IceCover real NOT NULL, " +
                "IsGasGiant INTEGER NOT NULL, " +
                "IsMoon INTEGER NOT NULL)"
        };


    }
}
