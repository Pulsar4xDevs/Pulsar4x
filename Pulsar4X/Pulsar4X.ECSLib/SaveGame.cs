using System;
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This class is responsible for saving a game to/from disk.
    /// </summary>
    // use: http://www.newtonsoft.com/json/help/html/SerializationAttributes.htm
    public static class SaveGame
    {
        private static readonly JsonSerializer DefaultSerializer = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented, ContractResolver = new ForceUseISerializable() };

        internal static Game CurrentGame { get; private set; }
        private static readonly object SyncRoot = new object();

        /// <summary>
        /// Saves the game to a file defined by filePath using the default serializer.
        /// </summary>
        [PublicAPI]
        public static void Save([NotNull] Game game, [NotNull] string filePath, bool compress = false)
        {
            CheckFile(filePath, FileAccess.Write);

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                Save(game, fileStream, compress);
            }
        }

        /// <summary>
        /// Saves the game to the provided stream using the default serializer.
        /// </summary>
        [PublicAPI]
        public static void Save([NotNull] Game game, [NotNull] Stream outputStream, bool compress = false)
        {
            CompressionLevel compressionLevel = compress ? CompressionLevel.Optimal : CompressionLevel.NoCompression;

            lock (SyncRoot)
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
                    using (GZipStream compressionStream = new GZipStream(outputStream, compressionLevel))
                    {
                        // Reset the MemoryStream's position to 0. CopyTo copies from Position to the end.
                        intermediateStream.Position = 0;
                        intermediateStream.CopyTo(compressionStream);
                    }
                }
            }
        }

        /// <summary>
        /// Loads the game from the file at the provided filePath using the default serializer.
        /// </summary>
        [PublicAPI]
        public static Game Load([NotNull] string filePath)
        {
            CheckFile(filePath, FileAccess.Read);

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                return Load(fileStream);
            }
        }

        /// <summary>
        /// Loads the game from the provided stream using the default serializer.
        /// </summary>
        [PublicAPI]
        private static Game Load(Stream inputStream)
        {
            Game game = new Game();

            lock (SyncRoot)
            {
                CurrentGame = game;
                using (GZipStream compressionStream = new GZipStream(inputStream, CompressionMode.Decompress))
                {
                    using (MemoryStream intermediateStream = new MemoryStream())
                    {
                        compressionStream.CopyTo(intermediateStream);

                        intermediateStream.Position = 0;

                        using (StreamReader sr = new StreamReader(intermediateStream))
                        {
                            using (JsonReader reader = new JsonTextReader(sr))
                            {
                                DefaultSerializer.Populate(reader, game);
                            }
                        }
                    }
                }

                // check the version info:
                if (game.Version.IsCompatibleWith(VersionInfo.PulsarVersionInfo) == false)
                {
                    string e = string.Format("The save file is not supported. the save is from version {0}, the game only supports versions: {1}", game.Version.VersionString, VersionInfo.PulsarVersionInfo.CompatibleVersions);

                    throw new NotSupportedException(e);
                }

                // get the game to do its post load stuff
                game.PostGameLoad();
                CurrentGame = null;
            }
            return game;
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
    }
}
