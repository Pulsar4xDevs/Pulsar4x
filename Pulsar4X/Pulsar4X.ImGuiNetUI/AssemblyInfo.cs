    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

namespace Pulsar4X.SDL2UI
{
    public static class AssemblyInfo
    {
        private static bool _hasRun = false;
        private static string _githash = "unknown";
        /// <summary> Gets the git hash value from the assembly
        /// or null if it cannot be found. </summary>
        /// 
        public static string GetGitHash()
        {
            if (!_hasRun)
            {
                /*
                var asm = typeof(AssemblyInfo).Assembly;
                var attrs = asm.GetCustomAttributes<AssemblyMetadataAttribute>();
                var atb = attrs.FirstOrDefault(a => a.Key == "GitHash")?.Value;
                */
                try
                {
                    string curDir = Directory.GetCurrentDirectory();
                    string gitlogPath = Path.GetFullPath(Path.Combine(curDir, @"..", "..", "..", "..", "..", ".git/logs/HEAD"));
                    if(File.Exists(gitlogPath))
                    {
                        string[] lines = File.ReadAllLines(gitlogPath);
                        string line = lines[0];
                        _githash = line.Split(' ', StringSplitOptions.None)[0];
                    }
                }
                catch (Exception e)
                {

                }

            }

            return _githash;

        }
    }
}