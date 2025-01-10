using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using ImGuiNET;

namespace Pulsar4X.Client.Interface.Widgets;

/// <summary>
/// The Window widget acts as a wrapper for the underlying GUI library (In this case ImGui).
/// The reasoning was to provide an easier way to handle any debugging code like GetWindowTitle.
/// You don't have to worry about remembering to call it anywhere else in the UI.
/// </summary>
public static class Window
{
#if DEBUG
    private static ConcurrentDictionary<string, string> _cachedPrefixs = new();
    
    // Track whether we're inside a Begin/End block
    private static int _beginCount = 0; 
    private static string _currentWindowTitle = "";
#endif
    
    public static bool Begin(string title, [CallerFilePath] string callerFilePath = "")
    {
#if DEBUG
        ValidateBeginCall(title);
#endif
        return ImGui.Begin(GetWindowTitle(title, callerFilePath));
    }
    
    public static bool Begin(string title, ref bool isActive, [CallerFilePath] string callerFilePath = "")
    {
#if DEBUG
        ValidateBeginCall(title);
#endif
        return ImGui.Begin(GetWindowTitle(title, callerFilePath), ref isActive);
    }

    public static bool Begin(string title, ref bool isActive, ImGuiWindowFlags flags, [CallerFilePath] string callerFilePath = "")
    {
#if DEBUG
        ValidateBeginCall(title);
#endif
        return ImGui.Begin(GetWindowTitle(title, callerFilePath), ref isActive, flags);
    }

    public static bool Begin(string title, ImGuiWindowFlags flags, [CallerFilePath] string callerFilePath = "")
    {
#if DEBUG
        ValidateBeginCall(title);
#endif
        return ImGui.Begin(GetWindowTitle(title, callerFilePath), flags);
    }

    public static void End()
    {
#if DEBUG
        if (_beginCount == 0)
        {
            throw new InvalidOperationException("End() called without a matching Begin()");
        }
        _beginCount--;
        if (_beginCount == 0)
        {
            _currentWindowTitle = "";
        }
#endif
        ImGui.End();
    }
    
#if DEBUG
    private static void ValidateBeginCall(string title)
    {
        if (_beginCount > 0)
        {
            throw new InvalidOperationException(
                $"Begin(\"{title}\") called while already inside window \"{_currentWindowTitle}\". " +
                "Must call End() before starting a new window.");
        }
        _beginCount++;
        _currentWindowTitle = title;
    }
#endif
    
    /// <summary>
    /// Prepends the calling class path and name to the provided title string when in debug mode.
    /// </summary>
    /// <param name="title">The original window title</param>
    /// <param name="callerFilePath">Automatically populated with caller's file path</param>
    /// <returns>Modified title string with class path in debug mode, original string in release mode</returns>
    private static string GetWindowTitle(string title, string callerFilePath)
    {
#if DEBUG
        if (!_cachedPrefixs.ContainsKey(callerFilePath))
        {
            // Get the full path and convert to project relative path
            string projectPath = GetProjectPath();
            string relativePath = "";

            if (!string.IsNullOrEmpty(projectPath) && callerFilePath.StartsWith(projectPath))
            {
                // Remove project path and leading slash to get relative path
                relativePath = callerFilePath.Substring(projectPath.Length).TrimStart('\\', '/');
                // Remove the file extension
                relativePath = Path.ChangeExtension(relativePath, null);
                _cachedPrefixs.TryAdd(callerFilePath, relativePath);
            }
            else
            {
                // Fallback to just filename if we can't get relative path
                string className = Path.GetFileNameWithoutExtension(callerFilePath);
                _cachedPrefixs.TryAdd(callerFilePath, className);
            }
        }

        return $"[{_cachedPrefixs[callerFilePath]}] {title}";
#else
        return title;
#endif
    }
    
    /// <summary>
    /// Attempts to find the project root directory by looking for the .csproj file
    /// </summary>
    private static string GetProjectPath()
    {
        try
        {
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (!string.IsNullOrEmpty(currentDirectory))
            {
                if (Directory.GetFiles(currentDirectory, "*.csproj").Length > 0)
                {
                    return currentDirectory;
                }
                currentDirectory = Path.GetDirectoryName(currentDirectory);
            }
        }
        catch
        {
            // Silently fail and let the calling method handle the empty result
        }
        return string.Empty;
    }
}