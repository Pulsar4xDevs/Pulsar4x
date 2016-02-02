using System.Reflection;
using Newtonsoft.Json;

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
        public readonly string Name;

        /// <summary>
        /// The version string. For example 1.1 or Major.Minor
        /// The version number should only contain numeric digits with periods separating the different sections.
        /// </summary>
        public readonly string VersionString;

        // Integer versions of each section of the build number:
        public readonly int MajorVersion;
        public readonly int MinorVersion;
        
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
                AssemblyName assName = Assembly.GetAssembly(typeof(VersionInfo)).GetName();
                string versionString = assName.Version.Major + "." + assName.Version.Minor;
                int majorVersion = assName.Version.Major;
                int minorVersion = assName.Version.Minor;
     
                VersionInfo gameVersionInfo = new VersionInfo("Pulsar4X Alpha", versionString, majorVersion, minorVersion, versionString, versionString);
                return gameVersionInfo;
            }
        }

        [JsonConstructor]
        public VersionInfo(string Name, string VersionString, int MajorVersion, int MinorVersion, string CompatibleVersions, string CompatibleLibVersions)
        {
            this.Name = Name;
            this.VersionString = VersionString;
            this.MajorVersion = MajorVersion;
            this.MinorVersion = MinorVersion;
            this.CompatibleVersions = CompatibleVersions;
            this.CompatibleLibVersions = CompatibleLibVersions;
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

        public override bool Equals(object obj)
        {
            if (!(obj is VersionInfo))
            {
                return false;
            }

            return Equals((VersionInfo)obj);
        }

        public bool Equals(VersionInfo other)
        {
            return string.Equals(Name, other.Name) && string.Equals(VersionString, other.VersionString) && MajorVersion == other.MajorVersion && MinorVersion == other.MinorVersion;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Name?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (VersionString?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ MajorVersion;
                hashCode = (hashCode * 397) ^ MinorVersion;
                return hashCode;
            }
        }
    }
}