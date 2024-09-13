using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImGuiNET;
using Microsoft.Extensions.Configuration;
using SDL2;

namespace Pulsar4X.SDL2UI;

/// <summary>
/// This class acts as an editor window for the view preferences
/// but also loads and saves the preferences and holds the
/// currently selected views for various parts of the game.
/// </summary>
public class SystemViewPreferences : PulsarGuiWindow
{
    internal record View
    {
        public string FileName;
        public string DisplayName;
        public int Id;
        public Dictionary<UserOrbitSettings.OrbitBodyType, bool> FilterCheckmarks = new ();

        public View(string fileName, string displayName, int id)
        {
            FileName = fileName;
            DisplayName = displayName;
            Id = id;
        }
    }

    private const string DefaultFileName = "default.ini";

    readonly Dictionary<UserOrbitSettings.OrbitBodyType, string> FilterDisplayOptions = new ()
    {
        { UserOrbitSettings.OrbitBodyType.Asteroid, "Asteroids" },
        { UserOrbitSettings.OrbitBodyType.Colony, "Colonies" },
        { UserOrbitSettings.OrbitBodyType.Comet, "Comets" },
        { UserOrbitSettings.OrbitBodyType.Moon, "Moons" },
        { UserOrbitSettings.OrbitBodyType.Planet, "Planets" },
        { UserOrbitSettings.OrbitBodyType.Ship, "Ships" },
        { UserOrbitSettings.OrbitBodyType.Star, "Stars" },
        { UserOrbitSettings.OrbitBodyType.Unknown, "Unknown Objects" }
    };

    Dictionary<int, View> Views = new ();
    int _selectedEditorViewIndex = 0;
    string[]? _selectedEditorViewNames;
    string ViewsDirectory;

    public string[] ViewNames
    {
        get { return _selectedEditorViewNames ?? new string[1] { "Default" }; }
    }

    Dictionary<string, int> ViewIndexes { get; set; } = new ();

    public int GetViewIndex(string key)
    {
        if(!ViewIndexes.ContainsKey(key))
            ViewIndexes[key] = 0;

        return ViewIndexes[key];
    }

    public void SetViewIndex(string key, int value)
    {
        ViewIndexes[key] = value;
    }

    internal View GetViewByIndex(int index)
    {
        return Views[index];
    }

    internal bool ShouldDisplay(string key, UserOrbitSettings.OrbitBodyType orbitBodyType)
    {
        return ViewIndexes.ContainsKey(key) ? Views[ViewIndexes[key]].FilterCheckmarks[orbitBodyType] : true;
    }

    internal static SystemViewPreferences GetInstance()
    {
        if (!_uiState.LoadedWindows.ContainsKey(typeof(SystemViewPreferences)))
        {
            return new SystemViewPreferences();
        }

        return (SystemViewPreferences)_uiState.LoadedWindows[typeof(SystemViewPreferences)];
    }

    internal SystemViewPreferences()
    {
        // Read and apply any view preferences
        string baseDirectory = SDL.SDL_GetPrefPath(PulsarMainWindow.OrgName, PulsarMainWindow.AppName);
        ViewsDirectory = Path.Combine(baseDirectory, "Views");

        if(!Directory.Exists(ViewsDirectory))
        {
            Directory.CreateDirectory(ViewsDirectory);
        }

        var files = Directory.EnumerateFiles(ViewsDirectory, "*.ini");
        if(files.Count() > 0)
        {
            foreach(var fileName in files)
            {
                LoadViewIni(fileName);
            }
        }
        else
        {
            CreateDefaultIni(ViewsDirectory);
            LoadViewIni(Path.Combine(ViewsDirectory, DefaultFileName));
        }

        _selectedEditorViewNames = new string[Views.Count];
        foreach((var id, var view) in Views)
        {
            _selectedEditorViewNames[id] = view.DisplayName;
        }

        // Reset the view index
        _selectedEditorViewIndex = 0;
    }

    private void LoadViewIni(string fileName)
    {
        IConfiguration preferences = new ConfigurationBuilder().AddIniFile(fileName).Build();
        IConfigurationSection metaSection = preferences.GetSection("meta");
        IConfigurationSection viewsSection = preferences.GetSection("views");

        string name = metaSection["name"] ?? "Unable to load name";
        string? asteroids = viewsSection["asteroids"];
        string? colonies = viewsSection["colonies"];
        string? comets = viewsSection["comets"];
        string? moons = viewsSection["moons"];
        string? planets = viewsSection["planets"];
        string? ships = viewsSection["ships"];
        string? stars = viewsSection["stars"];
        string? unknown = viewsSection["unknown"];

        var values = new Dictionary<UserOrbitSettings.OrbitBodyType, bool>();

        // Asteroids
        if(asteroids != null && bool.TryParse(asteroids, out bool asteroidValue))
        {
            values.Add(UserOrbitSettings.OrbitBodyType.Asteroid, asteroidValue);
        }
        else
        {
            values.Add(UserOrbitSettings.OrbitBodyType.Asteroid, true);
        }

        // Colonies
        if (colonies != null && bool.TryParse(colonies, out bool coloniesValue))
        {
            values.Add(UserOrbitSettings.OrbitBodyType.Colony, coloniesValue);
        }
        else
        {
            values.Add(UserOrbitSettings.OrbitBodyType.Colony, false);  // Default value
        }

        // Comets
        if (comets != null && bool.TryParse(comets, out bool cometsValue))
        {
            values.Add(UserOrbitSettings.OrbitBodyType.Comet, cometsValue);
        }
        else
        {
            values.Add(UserOrbitSettings.OrbitBodyType.Comet, false);  // Default value
        }

        // Moons
        if (moons != null && bool.TryParse(moons, out bool moonsValue))
        {
            values.Add(UserOrbitSettings.OrbitBodyType.Moon, moonsValue);
        }
        else
        {
            values.Add(UserOrbitSettings.OrbitBodyType.Moon, false);  // Default value
        }

        // Planets
        if (planets != null && bool.TryParse(planets, out bool planetsValue))
        {
            values.Add(UserOrbitSettings.OrbitBodyType.Planet, planetsValue);
        }
        else
        {
            values.Add(UserOrbitSettings.OrbitBodyType.Planet, false);  // Default value
        }

        // Ships
        if (ships != null && bool.TryParse(ships, out bool shipsValue))
        {
            values.Add(UserOrbitSettings.OrbitBodyType.Ship, shipsValue);
        }
        else
        {
            values.Add(UserOrbitSettings.OrbitBodyType.Ship, false);  // Default value
        }

        // Stars
        if (stars != null && bool.TryParse(stars, out bool starsValue))
        {
            values.Add(UserOrbitSettings.OrbitBodyType.Star, starsValue);
        }
        else
        {
            values.Add(UserOrbitSettings.OrbitBodyType.Star, false);  // Default value
        }

        // Unknown
        if (unknown != null && bool.TryParse(unknown, out bool unknownValue))
        {
            values.Add(UserOrbitSettings.OrbitBodyType.Unknown, unknownValue);
        }
        else
        {
            values.Add(UserOrbitSettings.OrbitBodyType.Unknown, false);  // Default value
        }

        // Set values
        var view = new View(Path.GetFileName(fileName), name, _selectedEditorViewIndex)
        {
            FilterCheckmarks = values
        };
        Views.Add(_selectedEditorViewIndex, view);

        _selectedEditorViewIndex++;
    }

    private void SaveViewIni(View view)
    {
        string fullPath = Path.Combine(ViewsDirectory, view.FileName);

        // Open or create the ini file for writing
        using (var writer = new StreamWriter(fullPath))
        {
            // Write the [name] section
            writer.WriteLine("[meta]");
            writer.WriteLine($"name={view.DisplayName}");

            // Write the [views] section
            writer.WriteLine("[views]");
            writer.WriteLine($"asteroids={view.FilterCheckmarks[UserOrbitSettings.OrbitBodyType.Asteroid]}");
            writer.WriteLine($"colonies={view.FilterCheckmarks[UserOrbitSettings.OrbitBodyType.Colony]}");
            writer.WriteLine($"comets={view.FilterCheckmarks[UserOrbitSettings.OrbitBodyType.Comet]}");
            writer.WriteLine($"moons={view.FilterCheckmarks[UserOrbitSettings.OrbitBodyType.Moon]}");
            writer.WriteLine($"planets={view.FilterCheckmarks[UserOrbitSettings.OrbitBodyType.Planet]}");
            writer.WriteLine($"ships={view.FilterCheckmarks[UserOrbitSettings.OrbitBodyType.Ship]}");
            writer.WriteLine($"stars={view.FilterCheckmarks[UserOrbitSettings.OrbitBodyType.Star]}");
            writer.WriteLine($"unknown={view.FilterCheckmarks[UserOrbitSettings.OrbitBodyType.Unknown]}");
        }
    }

    private void CreateDefaultIni(string directory)
    {
        var filePath = Path.Combine(directory, DefaultFileName);

        using( var writer = new StreamWriter(filePath))
        {
            writer.WriteLine("[meta]");
            writer.WriteLine("name=Default");
            writer.WriteLine("[views]");
            writer.WriteLine("asteroids=True");
            writer.WriteLine("colonies=True");
            writer.WriteLine("comets=True");
            writer.WriteLine("moons=True");
            writer.WriteLine("planets=True");
            writer.WriteLine("ships=True");
            writer.WriteLine("stars=True");
            writer.WriteLine("unknown=True");
        }
    }

    internal override void Display()
    {
        if(!IsActive) return;

        if(_selectedEditorViewNames == null)
                throw new NullReferenceException();

        if(ImGui.Begin("System View Preferences", ref IsActive))
        {
            if(ImGui.Combo("###view-selector", ref _selectedEditorViewIndex, _selectedEditorViewNames, _selectedEditorViewNames.Length))
            {
                ImGui.EndCombo();
            }
            ImGui.Separator();

            foreach((var bodyType, var displayName) in FilterDisplayOptions)
            {
                bool isChecked = Views[_selectedEditorViewIndex].FilterCheckmarks[bodyType];
                if(ImGui.Checkbox(displayName, ref isChecked))
                {
                    Views[_selectedEditorViewIndex].FilterCheckmarks[bodyType] = isChecked;
                    SaveViewIni(Views[_selectedEditorViewIndex]);
                }
            }
            ImGui.End();
        }
    }

    internal void DisplayCombo(string key, Action<int> onItemSelected)
    {
        int viewIndex = GetViewIndex(key);
        if(ImGui.BeginCombo($"###{key}-view-selector", ViewNames[viewIndex], ImGuiComboFlags.PopupAlignLeft | ImGuiComboFlags.HeightSmall))
        {
            for(int i = 0; i < ViewNames.Length; i++)
            {
                if(ImGui.Selectable(ViewNames[i], i == viewIndex))
                {
                    SetViewIndex(key, i);
                    onItemSelected?.Invoke(i);
                }
            }

            ImGui.Separator();

            if(ImGui.Selectable("Edit..."))
            {
                SetActive(true);
            }
            ImGui.EndCombo();
        }
    }
}