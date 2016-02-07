using System;
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

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
        private static readonly JsonSerializer DefaultSerializer = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented, ContractResolver = new ForceUseISerializable(), PreserveReferencesHandling = PreserveReferencesHandling.None };

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

                DefaultSerializer.Formatting = compress ? Formatting.None : Formatting.Indented;
                Progress = progress;
                ManagersProcessed = 0;
                game.NumSystems = game.StarSystems.Count;

                // Wrap the outputStream in a BufferedStream.
                // This will improves performance if the outputStream does not have an internal buffer. (E.G. NetworkStream)
                using (var outputBuffer = new BufferedStream(outputStream))
                {
                    using (var intermediateStream = new MemoryStream())
                    {
                        using (var streamWriter = new StreamWriter(intermediateStream, Encoding.UTF8, 1024, true))
                        {
                            using (var writer = new JsonTextWriter(streamWriter))
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
                            using (var compressionStream = new GZipStream(outputBuffer, CompressionLevel.Optimal))
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
        private static Game ImportGame([NotNull] Stream inputStream, IProgress<double> progress = null)
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

        #endregion

        #region Entity Serialization/Deserialization

        [PublicAPI]
        public static string ExportEntity([NotNull] Game game, [NotNull] Entity entity, bool compress = false)
        {
            using (var stream = new MemoryStream())
            {
                ExportEntity(game, entity, stream, compress);

                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }

        }

        [PublicAPI]
        public static void ExportEntity([NotNull] Game game, [NotNull] Entity entity, [NotNull] Stream outputStream, bool compress = false)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }
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
                            DefaultSerializer.Formatting = compress ? Formatting.None : Formatting.Indented;
                            DefaultSerializer.Serialize(writer, entity.Clone());
                        }
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

        [PublicAPI]
        public static Entity ImportEntity([NotNull] Game game, [NotNull] EntityManager manager, [NotNull] string jsonString)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }
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
                }
                stream.Position = 0;

                return ImportEntity(game, manager, stream);
            }
        }

        [PublicAPI]
        public static Entity ImportEntity([NotNull] Game game, [NotNull] EntityManager manager, [NotNull] MemoryStream inputMemoryStream)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }
            if (inputMemoryStream == null)
            {
                throw new ArgumentNullException(nameof(inputMemoryStream));
            }

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

        #endregion

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
                    DefaultSerializer.Populate(reader, CurrentGame);
                }
            }
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
                        entity = DefaultSerializer.Deserialize<ProtoEntity>(reader);
                        CurrentGame.PostGameLoad();
                        CurrentGame = null;
                    }
                }
            }
            return entity;
        }
    }
}
