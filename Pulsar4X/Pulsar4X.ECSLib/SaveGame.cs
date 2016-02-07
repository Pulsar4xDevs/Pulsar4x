using System;
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Text;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This class is responsible for saving a game to/from disk.
    /// </summary>
    // use: http://www.newtonsoft.com/json/help/html/SerializationAttributes.htm
    public static class SaveGame
    {
        /// <summary>
        /// Game class of the game that is currently saving/loading. It is garunteed to be the loading/saving game from
        /// the time the operation starts, until AFTER any events are fired.
        /// </summary>
        internal static Game CurrentGame { get; private set; }
        internal static IProgress<double> Progress { get; private set; }
        internal static int ManagersProcessed { get; set; }
        private static readonly object SyncRoot = new object();

        /// <summary>
        /// Saves the game to a file defined by filePath using the default serializer.
        /// </summary>
        [PublicAPI]
        public static void Save([NotNull] Game game, [NotNull] string filePath, IProgress<double> progress = null, bool compress = false)
        {
            CheckFile(filePath, FileAccess.Write);

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                Save(game, fileStream, progress, compress);
            }
        }

        /// <summary>
        /// Saves the game to the provided stream using the default serializer.
        /// </summary>
        [PublicAPI]
        public static void Save([NotNull] Game game, [NotNull] Stream outputStream,  IProgress<double> progress = null, bool compress = false)
        {
            JsonSerializer DefaultSerializer = new JsonSerializer {NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented, ContractResolver = new ForceUseISerializable(), PreserveReferencesHandling = PreserveReferencesHandling.None};
            if (game == null)
            {
                throw new ArgumentNullException("game");
            }

            DefaultSerializer.Formatting = compress ? Formatting.None : Formatting.Indented;

            lock (SyncRoot)
            {
                Progress = progress;
                ManagersProcessed = 0;
                game.NumSystems = game.StarSystems.Count;

                // Wrap the outputStream in a BufferedStream.
                // This will improves performance if the outputStream does not have an internal buffer. (E.G. NetworkStream)
                using (BufferedStream outputBuffer = new BufferedStream(outputStream))
                {
                    using (MemoryStream intermediateStream = new MemoryStream())
                    {
                        using (StreamWriter streamWriter = new StreamWriter(intermediateStream, Encoding.UTF8, 1024, true))
                        {
                            using (JsonWriter writer = new JsonTextWriter(streamWriter))
                            {
                                CurrentGame = game;
                                DefaultSerializer.Serialize(writer, game);
                                CurrentGame = null;
                            }
                        }

                        // Reset the MemoryStream's position to 0. CopyTo copies from Position to the end.
                        intermediateStream.Position = 0;

                        if (compress)
                        {
                            using (GZipStream compressionStream = new GZipStream(outputBuffer, CompressionLevel.Optimal))
                            {
                                intermediateStream.CopyTo(compressionStream);
                            }
                        }
                        else
                        {
                            intermediateStream.CopyTo(outputBuffer);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Loads the game from the file at the provided filePath using the default serializer.
        /// </summary>
        [PublicAPI]
        public static Game Load([NotNull] string filePath, IProgress<double> progress = null)
        {
            CheckFile(filePath, FileAccess.Read);

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                return Load(fileStream, progress);
            }
        }

        /// <summary>
        /// Loads the game from the provided stream using the default serializer.
        /// </summary>
        [PublicAPI]
        private static Game Load(Stream inputStream, IProgress<double> progress = null)
        {
            var game = new Game();

            lock (SyncRoot)
            {
                Progress = progress;
                ManagersProcessed = 0;
                CurrentGame = game;
                // Use a BufferedStream to allow reading and seeking from any stream.
                // Example: If inputStream is a NetworkStream, then we can only read once.
                using (BufferedStream inputBuffer = new BufferedStream(inputStream))
                {
                    // Check if our stream is compressed.
                    if (HasGZipHeader(inputBuffer))
                    {
                        // File is compressed. Decompress using GZip.
                        using (GZipStream compressionStream = new GZipStream(inputStream, CompressionMode.Decompress))
                        {
                            // Decompress into a MemoryStream.
                            using (MemoryStream intermediateStream = new MemoryStream())
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

        [PublicAPI]
        public static void ExportEntity(Game game, [NotNull] Entity entity, [NotNull] Stream outputStream, bool compress = false)
        {
            if (outputStream == null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }

            if (!entity.IsValid)
            {
                throw new InvalidOperationException("Cannot serialize invalid entities.");
            }

            var defaultSerializer = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented, ContractResolver = new ForceUseISerializable(), PreserveReferencesHandling = PreserveReferencesHandling.None };
            defaultSerializer.Formatting = compress ? Formatting.None : Formatting.Indented;

            using (var intermediateStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(intermediateStream, Encoding.UTF8, 1024, true))
                {
                    using (JsonWriter writer = new JsonTextWriter(streamWriter))
                    {
                        defaultSerializer.Serialize(writer, entity.Clone());
                    }
                }

                // Reset the MemoryStream's position to 0. CopyTo copies from Position to the end.
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
        }

        public static Entity ImportEntity(Game game, EntityManager manager, MemoryStream inputMemoryStream)
        {
            ProtoEntity entity;

            // Check if our stream is compressed.
            using (var bufferedStream = new BufferedStream(inputMemoryStream))
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
            return Entity.Create(manager, entity);
        }

        private static ProtoEntity PopulateEntity(Game game, Stream stream)
        {
            var defaultSerializer = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented, ContractResolver = new ForceUseISerializable(), PreserveReferencesHandling = PreserveReferencesHandling.Objects };
            ProtoEntity entity;

            using (var sr = new StreamReader(stream))
            {
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    lock (SyncRoot)
                    {
                        CurrentGame = game;
                        entity = defaultSerializer.Deserialize<ProtoEntity>(reader);
                        CurrentGame.PostGameLoad();
                        CurrentGame = null;
                    }
                }
            }
            return entity;
        }

        /// <summary>
        /// Check if we have a valid file.
        /// </summary>
        /// <param name="filePath">Path to the file to check.</param>
        /// <param name="fileAccess">Type of access to check for.</param>
        private static void CheckFile(string filePath, FileAccess fileAccess)
        {
            // Test writing the file. If there's any issues with the file, this will cause them to throw.
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath", "No valid file path provided.");
            }

            if ((fileAccess & FileAccess.Write) == FileAccess.Write)
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    byte[] bytes = Encoding.ASCII.GetBytes("Pulsar4X write text.");
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
            if ((fileAccess & FileAccess.Read) == FileAccess.Read)
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.ReadByte();
                }
            }
        }

        /// <summary>
        /// Checks the stream for compression by looking for GZip header numbers.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Populates the currently loading game from the passed uncompressed inputStream.
        /// </summary>
        /// <param name="inputStream">Uncompressed stream containing the game data.</param>
        private static void PopulateGame(Stream inputStream)
        {
            JsonSerializer DefaultSerializer = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented, ContractResolver = new ForceUseISerializable(), PreserveReferencesHandling = PreserveReferencesHandling.Objects };

            using (StreamReader sr = new StreamReader(inputStream))
            {
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    DefaultSerializer.Populate(reader, CurrentGame);
                }
            }
        }
    }
}
