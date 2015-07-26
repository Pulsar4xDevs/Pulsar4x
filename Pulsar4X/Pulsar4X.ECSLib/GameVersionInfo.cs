using System.Reflection;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This struct is used to prepend the game/mod version information in json files. This allows the game to
    /// make decisions on whether or not imported data is compatible.
    /// </summary>
    public struct VersionInfo
    {
        /// <summary>
        /// The name of the product, for the main game it is Pulsar4x, mods should use the name of the mod.
        /// </summary>
        public string Name;

        /// <summary>
        /// The version string. For example 1.1 or Major.Minor
        /// The version number should only contain numeric digits with periods separating the different sections.
        /// </summary>
        public string VersionString;

        // Integer versions of each section of the build number:
        public int MajorVersion;
        public int MinorVersion;
        
        /// <summary>
        /// A comma seperate list of compatible version numbers, numbers in this list will be deem compatible with the current version in VersionString.
        /// For example: "0.8,0.7,0.9,0.10,0.11,1.0". Note that there is no spaces in the string.
        /// </summary>
        public string CompatibleVersions;

        /// <summary>
        /// A comma seperated list of game library versions the mod is compatible with. 
        /// </summary>
        public string CompatibleLibVersions;

        /// <summary>
        /// Returns a VersionInfo struct for the Game.
        /// @todo need a better way of doing this, the compatible versions string is a big problem.
        /// </summary>
        public static VersionInfo PulsarVersionInfo
        {
            get
            {
                VersionInfo gameVersionInfo = new VersionInfo {Name = "Pulsar4X Alpha"};
                AssemblyName assName = Assembly.GetAssembly(typeof(VersionInfo)).GetName();
                gameVersionInfo.VersionString = assName.Version.Major + "." + assName.Version.Minor;
                gameVersionInfo.MajorVersion = assName.Version.Major;
                gameVersionInfo.MinorVersion = assName.Version.Minor;
                gameVersionInfo.CompatibleVersions = gameVersionInfo.VersionString;
                gameVersionInfo.CompatibleLibVersions = gameVersionInfo.VersionString;
                return gameVersionInfo;
            }
        }


        /// <summary>
        /// Checks that this Version Info is compatible with the version info supplied.
        /// </summary>
        public bool IsCompatibleWith(VersionInfo info)
        {
            string[] versions = info.CompatibleVersions.Split(',');
            foreach (var ver in versions)
            {
                if (ver == VersionString)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks that this version info is compatible with the main game library currently running. 
        /// </summary>
        public bool IsCompatibleWithLib()
        {
            AssemblyName assName = Assembly.GetAssembly(typeof(VersionInfo)).GetName();
            string libVersion = assName.Version.Major + "." + assName.Version.Minor;

            string[] versions = CompatibleLibVersions.Split(',');
            foreach (var ver in versions)
            {
                if (ver == libVersion)
                {
                    return true;
                }
            }

            return false;
        }
    }
}