using System;
using System.IO;

namespace Pulsar4X.SDL2UI;
public static class AssemblyInfo
{
    private static bool _hasRun = false;
    private static string _githash = "unknown";

    public static string GetGitHash()
    {
        if(_hasRun) return _githash;

        try
        {
            var directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());

            while(directoryInfo != null && !Directory.Exists(Path.Combine(directoryInfo.FullName, ".git")))
            {
                directoryInfo = directoryInfo.Parent;
            }

            if(directoryInfo == null)
            {
                _githash = "Could not find .git directory";
                _hasRun = true;
                return _githash;
            }

            string gitHeadLogPath = Path.Combine(directoryInfo.FullName, ".git", "logs", "HEAD");

            if(File.Exists(gitHeadLogPath))
            {
                string[] lines = File.ReadAllLines(gitHeadLogPath);
                if (lines.Length > 0)
                {
                    string lastLine = lines[lines.Length - 1];
                    string[] parts = lastLine.Split(' ');
                    if (parts.Length > 1)
                    {
                        // Git the hash
                        return parts[1].Substring(0, 7);
                    }
                }
            }
        }
        catch (Exception e)
        {
            _githash = e.Message;
        }

        _hasRun = true;
        return _githash;
    }
}