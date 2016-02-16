using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json.Serialization;
using Formatting = Newtonsoft.Json.Formatting;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This class is responsible for saving a game to/from disk.
    /// </summary>
    // use: http://www.newtonsoft.com/json/help/html/SerializationAttributes.htm
    public static class SerializationManager
    {
        /// <summary>
        /// Game class of the game that is currently saving/loading. It is garunteed to be the loading/saving game from
        /// the time the operation starts, until AFTER any events are fired.
        /// </summary>
        internal static Game CurrentGame { get; private set; }
        internal static IProgress<double> Progress { get; private set; }
        internal static int ManagersProcessed { get; set; }
        private static readonly object SyncRoot = new object();
        private static readonly JsonSerializer PersistenceSerializer = new JsonSerializer { Context = new StreamingContext(StreamingContextStates.Persistence), NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented, ContractResolver = new ForceUseISerializable(), PreserveReferencesHandling = PreserveReferencesHandling.None };
        private static readonly JsonSerializer RemoteSerializer = new JsonSerializer {Context = new StreamingContext(StreamingContextStates.Remoting), NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.None, ContractResolver = new ForceUseISerializable(), PreserveReferencesHandling = PreserveReferencesHandling.None};

        #region Game Serialization/Deserialization

        /// <summary>
        /// Saves the game to a file defined by filePath using the default serializer.
        /// </summary>
        [PublicAPI]
        public static void ExportGame([NotNull] Game game, [NotNull] string filePath, IProgress<double> progress = null, bool compress = false)
        {
            CheckFile(filePath, FileAccess.Write);

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                ExportGame(game, fileStream, progress, compress);
            }
        }

        /// <summary>
        /// Saves the game to the provided stream using the default serializer.
        /// </summary>
        [PublicAPI]
        public static void ExportGame([NotNull] Game game, [NotNull] Stream outputStream, IProgress<double> progress = null, bool compress = false)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            lock (SyncRoot)
            {
                PersistenceSerializer.Formatting = compress ? Formatting.None : Formatting.Indented;
                Progress = progress;
                ManagersProcessed = 0;
                game.NumSystems = game.StarSystems.Count;

                using (var intermediateStream = new MemoryStream())
                {
                    using (var streamWriter = new StreamWriter(intermediateStream, Encoding.UTF8, 1024, true))
                    {
                        using (var writer = new JsonTextWriter(streamWriter))
                        {
                            CurrentGame = game;
                            PersistenceSerializer.Serialize(writer, game);
                            CurrentGame = null;
                        }
                    }

                    FinalizeOutput(outputStream, intermediateStream, compress);
                }
            }
        }

        /// <summary>
        /// Loads the game from the file at the provided filePath using the default serializer.
        /// </summary>
        [PublicAPI]
        public static Game ImportGame([NotNull] string filePath, IProgress<double> progress = null)
        {
            CheckFile(filePath, FileAccess.Read);

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                return ImportGame(fileStream, progress);
            }
        }

        /// <summary>
        /// Loads the game from the provided stream using the default serializer.
        /// </summary>
        [PublicAPI]
        public static Game ImportGame([NotNull] Stream inputStream, IProgress<double> progress = null)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }
            var game = new Game();

            lock (SyncRoot)
            {
                Progress = progress;
                ManagersProcessed = 0;
                CurrentGame = game;
                // Use a BufferedStream to allow reading and seeking from any stream.
                // Example: If inputStream is a NetworkStream, then we can only read once.
                using (var inputBuffer = new BufferedStream(inputStream))
                {
                    // Check if our stream is compressed.
                    if (HasGZipHeader(inputBuffer))
                    {
                        // File is compressed. Decompress using GZip.
                        using (var compressionStream = new GZipStream(inputStream, CompressionMode.Decompress))
                        {
                            // Decompress into a MemoryStream.
                            using (var intermediateStream = new MemoryStream())
                            {
                                // Decompress the file into an intermediate MemoryStream.
                                compressionStream.CopyTo(intermediateStream);

                                // Reset the position of the MemoryStream so it can be read from the beginning.
                                intermediateStream.Position = 0;

                                // Populate the game from the uncompressed MemoryStream.
                                PopulateGame(intermediateStream);
                            }
                        }
                    }
                    else
                    {
                        // Populate the game from the uncompressed inputStream.
                        PopulateGame(inputBuffer);
                    }
                }

                // get the game to do its post load stuff
                game.PostGameLoad();
                CurrentGame = null;
            }
            return game;
        }

        /// <summary>
        /// Populates the currently loading game from the passed uncompressed inputStream.
        /// </summary>
        /// <param name="inputStream">Uncompressed stream containing the game data.</param>
        private static void PopulateGame(Stream inputStream)
        {
            using (var sr = new StreamReader(inputStream))
            {
                using (var reader = new JsonTextReader(sr))
                {
                    PersistenceSerializer.Populate(reader, CurrentGame);
                }
            }
        }

        #endregion

        #region Entity Serialization/Deserialization

        /// <summary>
        /// Exports an entity to a JsonString
        /// </summary>
        /// <param name="entity">Entity to export</param>
        /// <param name="compress">Determines if the JsonString should be compressed using GZip</param>
        /// <returns>JsonString representation of the entity.</returns>
        [PublicAPI]
        public static string ExportEntity([NotNull] Entity entity, bool compress = false)
        {
            using (var stream = new MemoryStream())
            {
                ExportEntity(entity, stream, compress);

                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Exports an entity to a provided outputStream.
        /// </summary>
        /// <param name="entity">Entity to export</param>
        /// <param name="outputStream">Output stream to use.</param>
        /// <param name="compress">Determines if the output should be compressed using GZip</param>
        [PublicAPI]
        public static void ExportEntity([NotNull] Entity entity, [NotNull] Stream outputStream, bool compress = false)
        {
            if (outputStream == null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }

            if (!entity.IsValid)
            {
                throw new InvalidOperationException("Cannot serialize invalid entities.");
            }

            using (var intermediateStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(intermediateStream, Encoding.UTF8, 1024, true))
                {
                    using (var writer = new JsonTextWriter(streamWriter))
                    {
                        lock (SyncRoot)
                        {
                            PersistenceSerializer.Formatting = compress ? Formatting.None : Formatting.Indented;
                            PersistenceSerializer.Serialize(writer, entity.Clone());
                        }
                    }
                }

                // Reset the MemoryStream's position to 0. CopyTo copies from Position to the end.
                FinalizeOutput(outputStream, intermediateStream, compress);
            }
        }

        /// <summary>
        /// Imports an entity into a specified manager.
        /// </summary>
        /// <param name="game">Game object we're importing into.</param>
        /// <param name="entityManager">EntityManager to import the entity into.</param>
        /// <param name="jsonString">JsonString representation of the Entity.</param>
        /// <returns>Valid and fully formed Entity</returns>
        [PublicAPI]
        public static Entity ImportEntity([NotNull] Game game, [NotNull] EntityManager entityManager, [NotNull] string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                throw new ArgumentException("Argument is null or empty", nameof(jsonString));
            }

            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(jsonString);
                    writer.Flush();

                    stream.Position = 0;
                    return ImportEntity(game, entityManager, stream);
                }
            }
        }

        /// <summary>
        /// Imports an entity into the specified manager from a Stream.
        /// </summary>
        /// <param name="game">Game object we're importing into.</param>
        /// <param name="entityManager">EntityManager to import the entity into.</param>
        /// <param name="inputStream">Stream that will contain the entity.</param>
        /// <returns></returns>
        [PublicAPI]
        public static Entity ImportEntity([NotNull] Game game, [NotNull] EntityManager entityManager, [NotNull]Stream inputStream)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }
            if (entityManager == null)
            {
                throw new ArgumentNullException(nameof(entityManager));
            }
            if (inputStream == null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }

            ProtoEntity entity;

            // Check if our stream is compressed.
            using (var bufferedStream = new BufferedStream(inputStream))
            {
                if (HasGZipHeader(bufferedStream))
                {
                    // File is compressed. Decompress using GZip.
                    using (var compressionStream = new GZipStream(bufferedStream, CompressionMode.Decompress))
                    {
                        // Decompress into a MemoryStream.
                        using (var intermediateStream = new MemoryStream())
                        {
                            // Decompress the file into an intermediate MemoryStream.
                            compressionStream.CopyTo(intermediateStream);

                            // Reset the position of the MemoryStream so it can be read from the beginning.
                            intermediateStream.Position = 0;

                            entity = PopulateEntity(game, intermediateStream);
                        }
                    }
                }
                else
                {
                    entity = PopulateEntity(game, bufferedStream);
                }
            }
            return Entity.Create(entityManager, entity);
        }

        private static ProtoEntity PopulateEntity(Game game, Stream stream)
        {
            ProtoEntity entity;

            using (var sr = new StreamReader(stream))
            {
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    lock (SyncRoot)
                    {
                        CurrentGame = game;
                        entity = PersistenceSerializer.Deserialize<ProtoEntity>(reader);
                        CurrentGame.PostGameLoad();
                        CurrentGame = null;
                    }
                }
            }
            return entity;
        }

        #endregion

        #region StarSystem Serialization/Deserialization

        public static string ExportStarSystem(StarSystem system, bool compress = false)
        {
            using (var stream = new MemoryStream())
            {
                ExportStarSystem(system, stream, compress);

                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static void ExportStarSystem(StarSystem system, Stream outputStream, bool compress = false)
        {
            if (outputStream == null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }

            using (var intermediateStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(intermediateStream, Encoding.UTF8, 1024, true))
                {
                    using (var writer = new JsonTextWriter(streamWriter))
                    {
                        lock (SyncRoot)
                        {
                            PersistenceSerializer.Formatting = compress ? Formatting.None : Formatting.Indented;
                            PersistenceSerializer.Serialize(writer, system);
                        }
                    }
                }

                // Reset the MemoryStream's position to 0. CopyTo copies from Position to the end.
                FinalizeOutput(outputStream, intermediateStream, compress);
            }
        }

        public static void ExportStarSystemsToXML(Game game)
        {
            var ser = new XmlSerializer(typeof(XmlNode));
            var writer = new StreamWriter(".\\SystemsExport.xml");

            var xmlDoc = new XmlDocument();
            XmlNode toplevelNode = xmlDoc.CreateNode(XmlNodeType.Element, "Systems", "NS");

            foreach (KeyValuePair<Guid, StarSystem> kvp in game.Systems)
            {
                StarSystem system = kvp.Value;
                var rootStar = system.SystemManager.GetFirstEntityWithDataBlob<OrbitDB>();

                // get root star:
                var orbitDB = rootStar.GetDataBlob<OrbitDB>();
                rootStar = orbitDB.Root;

                XmlNode systemNode = xmlDoc.CreateNode(XmlNodeType.Element, "System", "NS");

                // the following we serialize the body to xml, and will do the same for all child bodies:
                SerializeBodyToXML(xmlDoc, systemNode, rootStar, orbitDB);

                // add xml to to level node:
                toplevelNode.AppendChild(systemNode);
            }

            // save xml to file:
            ser.Serialize(writer, toplevelNode);
            writer.Close();
        }

        private static void SerializeBodyToXML(XmlDocument xmlDoc, XmlNode systemNode, Entity systemBody, OrbitDB orbit)
        {
            // get the datablobs:
            var systemBodyDB = systemBody.GetDataBlob<SystemBodyDB>();
            var starIfnoDB = systemBody.GetDataBlob<StarInfoDB>();
            var positionDB = systemBody.GetDataBlob<PositionDB>();
            var massVolumeDB = systemBody.GetDataBlob<MassVolumeDB>();
            var nameDB = systemBody.GetDataBlob<NameDB>();
            var atmosphereDB = systemBody.GetDataBlob<AtmosphereDB>();
            var ruinsDB = systemBody.GetDataBlob<RuinsDB>();

            // create the body node:
            XmlNode bodyNode = xmlDoc.CreateNode(XmlNodeType.Element, "Body", "NS");

            // save parent id first:
            XmlNode varNode = xmlDoc.CreateNode(XmlNodeType.Element, "ParentID", "NS");
            if (orbit.Parent != null)
                varNode.InnerText = orbit.Parent.Guid.ToString();
            else
                varNode.InnerText = Guid.Empty.ToString();
            bodyNode.AppendChild(varNode);

            // then add our ID to at the end:
            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "ID", "NS");
            varNode.InnerText = systemBody.Guid.ToString();
            bodyNode.AppendChild(varNode);

            if (nameDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Name", "NS");
                varNode.InnerText = nameDB.DefaultName;
                bodyNode.AppendChild(varNode);
            }

            if (starIfnoDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Type", "NS");
                varNode.InnerText = starIfnoDB.SpectralType.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Class", "NS");
                varNode.InnerText = starIfnoDB.Class;
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Age", "NS");
                varNode.InnerText = starIfnoDB.Age.ToString("N0");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "AverageEcoSphereRadius", "NS");
                varNode.InnerText = starIfnoDB.EcoSphereRadius.ToString("N3");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "MinEcoSphereRadius", "NS");
                varNode.InnerText = starIfnoDB.MinHabitableRadius.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "MaxEcoSphereRadius", "NS");
                varNode.InnerText = starIfnoDB.MaxHabitableRadius.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Luminosity", "NS");
                varNode.InnerText = starIfnoDB.Luminosity.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Temperature", "NS");
                varNode.InnerText = starIfnoDB.Temperature.ToString("N0");
                bodyNode.AppendChild(varNode);
            }

            if (massVolumeDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "MassInKG", "NS");
                varNode.InnerText = massVolumeDB.Mass.ToString("N0");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "MassInEarthMasses", "NS");
                varNode.InnerText = (massVolumeDB.Mass / GameConstants.Units.EarthMassInKG).ToString("N2");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Density", "NS");
                varNode.InnerText = massVolumeDB.Density.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Radius", "NS");
                varNode.InnerText = massVolumeDB.RadiusInKM.ToString("N0");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Volume", "NS");
                varNode.InnerText = massVolumeDB.Volume.ToString("N0");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "SurfaceGravity", "NS");
                varNode.InnerText = massVolumeDB.SurfaceGravity.ToString("N4");
                bodyNode.AppendChild(varNode);
            }

            // add orbit details:
            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "SemiMajorAxis", "NS");
            varNode.InnerText = orbit.SemiMajorAxis.ToString("N3");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Apoapsis", "NS");
            varNode.InnerText = orbit.Apoapsis.ToString("N3");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Periapsis", "NS");
            varNode.InnerText = orbit.Periapsis.ToString("N3");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Year", "NS");
            varNode.InnerText = orbit.OrbitalPeriod.ToString("dd\\:hh\\:mm");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Eccentricity", "NS");
            varNode.InnerText = orbit.Eccentricity.ToString("N3");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Inclination", "NS");
            varNode.InnerText = orbit.Inclination.ToString("N2");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Children", "NS");
            varNode.InnerText = orbit.Children.Count.ToString();
            bodyNode.AppendChild(varNode);

            if (positionDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "PositionInAU", "NS");
                varNode.InnerText = "(" + positionDB.X.ToString("N3") + ", " + positionDB.Y.ToString("N3") + ", " + positionDB.Z.ToString("N3") + ")";
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "PositionInKm", "NS");
                varNode.InnerText = "(" + positionDB.XInKm.ToString("N3") + ", " + positionDB.YInKm.ToString("N3") + ", " + positionDB.ZInKm.ToString("N3") + ")";
                bodyNode.AppendChild(varNode);
            }

            if (systemBodyDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Type", "NS");
                varNode.InnerText = systemBodyDB.Type.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "AxialTilt", "NS");
                varNode.InnerText = systemBodyDB.AxialTilt.ToString("N1");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Temperature", "NS");
                varNode.InnerText = systemBodyDB.BaseTemperature.ToString("N1");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "LengthOfDay", "NS");
                varNode.InnerText = systemBodyDB.LengthOfDay.ToString("g");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "MagneticField", "NS");
                varNode.InnerText = systemBodyDB.MagneticField.ToString("N2");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Tectonics", "NS");
                varNode.InnerText = systemBodyDB.Tectonics.ToString();
                bodyNode.AppendChild(varNode);
            }

            if (atmosphereDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Atmosphere", "NS");
                varNode.InnerText = atmosphereDB.AtomsphereDescriptionInPercent;
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "AtmosphereInATM", "NS");
                varNode.InnerText = atmosphereDB.AtomsphereDescriptionAtm;
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Pressure", "NS");
                varNode.InnerText = atmosphereDB.Pressure.ToString("N2");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Albedo", "NS");
                varNode.InnerText = atmosphereDB.Albedo.ToString("p1");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "SurfaceTemperature", "NS");
                varNode.InnerText = atmosphereDB.SurfaceTemperature.ToString("N1");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "GreenhouseFactor", "NS");
                varNode.InnerText = atmosphereDB.GreenhouseFactor.ToString("N2");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "GreenhousePressure", "NS");
                varNode.InnerText = atmosphereDB.GreenhousePressure.ToString("N2");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "HasHydrosphere", "NS");
                varNode.InnerText = atmosphereDB.Hydrosphere.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "HydrosphereExtent", "NS");
                varNode.InnerText = atmosphereDB.HydrosphereExtent.ToString();
                bodyNode.AppendChild(varNode);
            }

            if (ruinsDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "RuinCount", "NS");
                varNode.InnerText = ruinsDB.RuinCount.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "RuinQuality", "NS");
                varNode.InnerText = ruinsDB.RuinQuality.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "RuinSize", "NS");
                varNode.InnerText = ruinsDB.RuinSize.ToString();
                bodyNode.AppendChild(varNode);
            }

            // add body node to system node:
            systemNode.AppendChild(bodyNode);

            // call recursively for children:
            foreach (var child in orbit.Children)
            {
                OrbitDB o = child.GetDataBlob<OrbitDB>();
                if (o != null)
                    SerializeBodyToXML(xmlDoc, systemNode, o.OwningEntity, o);
            }
        }

        public static StarSystem ImportStarSystem(Game game, string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                throw new ArgumentException("Argument is null or empty", nameof(jsonString));
            }

            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(jsonString);
                    writer.Flush();

                    stream.Position = 0;
                    return ImportStarSystem(game, stream);
                }
            }
        }

        public static StarSystem ImportStarSystem(Game game, Stream inputStream)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }
            if (inputStream == null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }

            StarSystem system;

            // Check if our stream is compressed.
            using (var bufferedStream = new BufferedStream(inputStream))
            {
                if (HasGZipHeader(bufferedStream))
                {
                    // File is compressed. Decompress using GZip.
                    using (var compressionStream = new GZipStream(bufferedStream, CompressionMode.Decompress))
                    {
                        // Decompress into a MemoryStream.
                        using (var intermediateStream = new MemoryStream())
                        {
                            // Decompress the file into an intermediate MemoryStream.
                            compressionStream.CopyTo(intermediateStream);

                            // Reset the position of the MemoryStream so it can be read from the beginning.
                            intermediateStream.Position = 0;

                            system = PopulateStarSystem(game, intermediateStream);
                        }
                    }
                }
                else
                {
                    system = PopulateStarSystem(game, bufferedStream);
                }
            }
            return system;
        }

        private static StarSystem PopulateStarSystem(Game game, Stream bufferedStream)
        {
            StarSystem system;
            using (var streamReader = new StreamReader(bufferedStream))
            {
                using (var reader = new JsonTextReader(streamReader))
                {
                    lock (SyncRoot)
                    {
                        CurrentGame = game;
                        system = PersistenceSerializer.Deserialize<StarSystem>(reader);
                        CurrentGame.StarSystems.Add(system.Guid, system);
                        CurrentGame.PostGameLoad();
                        CurrentGame = null;
                    }
                }
            }

            return system;
        }

        #endregion

        /// <summary>
        /// Finalizes the outputStream by applying compression.
        /// </summary>
        private static void FinalizeOutput(Stream outputStream, Stream intermediateStream, bool compress)
        {
            intermediateStream.Position = 0;

            if (compress)
            {
                using (var compressionStream = new GZipStream(outputStream, CompressionLevel.Optimal))
                {
                    intermediateStream.CopyTo(compressionStream);
                }
            }
            else
            {
                intermediateStream.CopyTo(outputStream);
            }
        }

        /// <summary>
        /// Check if we have a valid file. Will throw exceptions if there's issues with the file.
        /// </summary>
        /// <param name="filePath">Path to the file to check.</param>
        /// <param name="fileAccess">Type of access to check for.</param>
        private static void CheckFile(string filePath, FileAccess fileAccess)
        {
            // Test writing the file. If there's any issues with the file, this will cause them to throw.
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath), "No valid file path provided.");
            }

            if ((fileAccess & FileAccess.Write) == FileAccess.Write)
            {
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    byte[] bytes = Encoding.ASCII.GetBytes("Pulsar4X write text.");
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
            if ((fileAccess & FileAccess.Read) == FileAccess.Read)
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.ReadByte();
                }
            }
        }

        /// <summary>
        /// Checks the stream for compression by looking for GZip header numbers.
        /// </summary>
        [PublicAPI]
        public static bool HasGZipHeader(BufferedStream inputStream)
        {
            var headerBytes = new byte[2];

            int numBytes = inputStream.Read(headerBytes, 0, 2);
            inputStream.Position = 0;
            if (numBytes != 2)
            {
                return false;
            }
            // First two bytes should be 31 and 139 according to the GZip file format.
            // http://www.gzip.org/zlib/rfc-gzip.html#header-trailer
            return headerBytes[0] == 31 && headerBytes[1] == 139;
        }
    }
}
