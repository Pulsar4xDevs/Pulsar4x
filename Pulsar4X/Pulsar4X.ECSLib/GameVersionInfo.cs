using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This class is used to prepend the game/mod version information in json files. This allows the game to
    /// make decisions on whether or not imported data is compatible.
    /// </summary>
    public class VersionInfo
    {
        /// <summary>
        /// The name of the product, for the main game it is Pulsar4x, mods should use the name of the mod.
        /// </summary>
        public readonly string Name;
        public readonly string LifeCycle;

        // Integer versions of each section of the build number:
        public readonly int MajorVersion;
        public readonly int MinorVersion;

        /// <summary>
        /// The version string. For example 1.1 or Major.Minor
        /// The version number should only contain numeric digits with periods separating the different sections.
        /// </summary>
        public string VersionString => $"{MajorVersion}.{MinorVersion}";
  
        public string FullVersion => $"{Name} {LifeCycle} {VersionString}";

        /// <summary>
        /// Returns a VersionInfo struct for the Game.
        /// </summary>
        public static VersionInfo PulsarVersionInfo
        {
            get
            {
                
                AssemblyName assName = Assembly.GetAssembly(typeof(VersionInfo)).GetName();
                int majorVersion = assName.Version.Major;
                int minorVersion = assName.Version.Minor;
     
                VersionInfo gameVersionInfo = new VersionInfo("Pulsar4X", "Alpha", majorVersion, minorVersion);
                return gameVersionInfo;
            }
        }

        [JsonConstructor]
        public VersionInfo(string Name, string LifeCycle, int MajorVersion, int MinorVersion)
        {
            this.Name = Name;
            this.LifeCycle = LifeCycle;
            this.MajorVersion = MajorVersion;
            this.MinorVersion = MinorVersion;
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
            return string.Equals(Name, other.Name) && string.Equals(LifeCycle, other.LifeCycle) && MajorVersion == other.MajorVersion && MinorVersion == other.MinorVersion;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Name?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (LifeCycle?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ MajorVersion;
                hashCode = (hashCode * 397) ^ MinorVersion;
                return hashCode;
            }
        }
    }

    public class DataVersionInfo : VersionInfo
    {
        public string Directory;

        public VersionInfo MinLibraryVersion;
        public VersionInfo MaxLibraryVersion;
        public List<VersionInfo> IncompatibleData;

        public DataVersionInfo(string Name, string LifeCycle, int MajorVersion, int MinorVersion, string Directory, VersionInfo MinLibraryVersion, VersionInfo MaxLibraryVersion, List<VersionInfo> IncompatibleData = null) 
            : base(Name, LifeCycle, MajorVersion, MinorVersion)
        {
            this.Directory = Directory;
            this.MinLibraryVersion = MinLibraryVersion;
            this.MaxLibraryVersion = MaxLibraryVersion;
            this.IncompatibleData = IncompatibleData ?? new List<VersionInfo>();
        }

        public bool IsCompatibleWithLibrary()
        {
            if (MinLibraryVersion != null)
            {
                if (MinLibraryVersion.MajorVersion > PulsarVersionInfo.MajorVersion || MinLibraryVersion.MinorVersion > PulsarVersionInfo.MinorVersion)
                {
                    return false;
                }
            }

            if (MaxLibraryVersion != null)
            {
                if (MaxLibraryVersion.MajorVersion < PulsarVersionInfo.MajorVersion || MaxLibraryVersion.MinorVersion < PulsarVersionInfo.MinorVersion)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsCompatibleWithDataset(DataVersionInfo dataSetVInfo)
        {
            return !IncompatibleData.Contains(dataSetVInfo) && !dataSetVInfo.IncompatibleData.Contains(this);
        }
    }
}