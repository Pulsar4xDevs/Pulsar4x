using System;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client;

/// <summary>Debug portrait builder: inspect and pin layer and palette picks for a species pack.</summary>
public sealed class PortraitBuilderWindow : UniquePulsarGuiWindow<PortraitBuilderWindow>
{
    string _species = PortraitIdentity.DefaultSpecies;
    string _role = PortraitRoles.Civilian;
    int _seed = 1;
    readonly PortraitPickOverrides _overrides = new();

    PortraitBuilderWindow()
    {
    }

    internal static PortraitBuilderWindow GetInstance()
    {
        if (_uiState.TryGetUniqueWindow<PortraitBuilderWindow>(out var window))
            return window;
        return _uiState.AddUniqueWindow(new PortraitBuilderWindow());
    }

    internal override void Display()
    {
        if (!IsActive)
            return;

        ImGui.SetNextWindowSize(new Vector2(720, 740), ImGuiCond.FirstUseEver);
        using (var w = Window.Scope("Portrait Builder", ref IsActive, _flags))
        {
            if (!w.Visible)
                return;

            var catalog = PortraitRenderer.Catalog;
            if (catalog.Packs.Count == 0)
                ImGui.TextUnformatted("No species packs loaded. The catalog will fall back to the legacy human layout if portrait-layers.json is present.");

            var pack = catalog.ResolveSpecies(_species);
            _species = pack.Id;
            _role = PortraitPicker.NormalizeRole(_role, catalog.Roles);

            DrawSpeciesCombo(catalog, pack);
            ImGui.SameLine();
            DrawRoleCombo(catalog);

            ImGui.TextUnformatted("Seed");
            ImGui.SameLine();
            int seed = _seed;
            ImGui.SetNextItemWidth(140);
            if (ImGui.InputInt("##portrait-seed", ref seed))
                _seed = seed;
            ImGui.SameLine();
            if (ImGui.Button("Random"))
            {
                _seed = Random.Shared.Next(1, int.MaxValue);
                _overrides.Clear();
            }
            ImGui.SameLine();
            if (ImGui.Button("Reload packs"))
            {
                PortraitRenderer.Reload();
                catalog = PortraitRenderer.Catalog;
                pack = catalog.ResolveSpecies(_species);
                _species = pack.Id;
            }

            ImGui.Separator();
            ImGui.TextUnformatted("Layers");
            var seedPicks = PortraitPicker.Pick(catalog, _seed, _role, PortraitIdentity.DefaultFactionColour, _species);
            for (int i = 0; i < pack.Layers.Count; i++)
                DrawLayerRow(pack, pack.Layers[i], i < seedPicks.Count ? seedPicks[i] : null);

            ImGui.Separator();
            ImGui.TextUnformatted("Palettes");
            foreach (var palette in pack.Palettes)
                DrawPaletteRow(palette.Key, palette.Value);

            var picks = PortraitPicker.Pick(
                catalog, _seed, _role, PortraitIdentity.FactionColour(_uiState), _species, _overrides);

            ImGui.Separator();
            ImGui.TextUnformatted("Preview");
            var renderer = PortraitRenderer.For(_uiState);
            renderer.Draw(_seed, _role, PortraitIdentity.FactionColour(_uiState), 256f, _species, _overrides);
            ImGui.SameLine();
            renderer.Draw(_seed, _role, PortraitIdentity.FactionColour(_uiState), 128f, _species, _overrides);
            ImGui.SameLine();
            renderer.Draw(_seed, _role, PortraitIdentity.FactionColour(_uiState), 48f, _species, _overrides);

            ImGui.Separator();
            ImGui.TextUnformatted("Picked files");
            for (int i = 0; i < pack.Layers.Count && i < picks.Count; i++)
            {
                var layer = pack.Layers[i];
                var pick = picks[i];
                string file = string.IsNullOrEmpty(pick.File) ? "None" : pick.File;
                ImGui.TextUnformatted(
                    $"{layer.Id}: {file}  offset ({pick.Offset.X:0.##}, {pick.Offset.Y:0.##})");
            }
        }
    }

    void DrawSpeciesCombo(PortraitCatalog catalog, PortraitSpeciesPack current)
    {
        ImGui.TextUnformatted("Species");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);
        if (!ImGui.BeginCombo("##portrait-species", current.Name))
            return;
        foreach (var pack in catalog.Packs)
        {
            bool selected = string.Equals(pack.Id, _species, StringComparison.OrdinalIgnoreCase);
            if (ImGui.Selectable(pack.Name + "##" + pack.Id, selected) && !selected)
            {
                _species = pack.Id;
                _overrides.Clear();
            }
            if (selected)
                ImGui.SetItemDefaultFocus();
        }
        ImGui.EndCombo();
    }

    void DrawRoleCombo(PortraitCatalog catalog)
    {
        ImGui.TextUnformatted("Role");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);
        if (!ImGui.BeginCombo("##portrait-role", _role))
            return;
        foreach (var role in catalog.Roles)
        {
            bool selected = string.Equals(role, _role, StringComparison.OrdinalIgnoreCase);
            if (ImGui.Selectable(role, selected))
                _role = role;
            if (selected)
                ImGui.SetItemDefaultFocus();
        }
        ImGui.EndCombo();
    }

    void DrawLayerRow(PortraitSpeciesPack pack, PortraitLayerSpec layer, PortraitLayerPick? seedPick)
    {
        var files = PortraitPicker.FilesOf(pack, layer);
        int n = files.Count;
        bool pinned = _overrides.Layers.TryGetValue(layer.Id, out var pin);
        int k;
        bool isNone;
        if (pinned)
        {
            isNone = pin is null;
            k = isNone || n == 0 ? 0 : ((pin!.Value % n) + n) % n + 1;
        }
        else
        {
            isNone = string.IsNullOrEmpty(seedPick?.File);
            k = seedPick is { FileIndex: >= 0 } ? seedPick.FileIndex + 1 : 0;
        }

        ImGui.PushID("layer-" + layer.Id);
        if (ImGui.Button("<") && n > 0)
            StepLayer(layer.Id, n, -1, isNone ? n - 1 : k - 1);
        ImGui.SameLine();
        string label = isNone || k <= 0 ? $"{layer.Id}  None" : $"{layer.Id}  {k}/{n}";
        ImGui.TextUnformatted(label);
        ImGui.SameLine();
        if (ImGui.Button(">") && n > 0)
            StepLayer(layer.Id, n, 1, isNone ? 0 : k - 1);
        if (layer.EmptyChance > 0f)
        {
            ImGui.SameLine();
            bool none = isNone;
            if (ImGui.Checkbox("None", ref none) && none != isNone)
            {
                if (none)
                    _overrides.Layers[layer.Id] = null;
                else if (n > 0)
                    _overrides.Layers[layer.Id] = 0;
                else
                    _overrides.Layers.Remove(layer.Id);
            }
        }
        ImGui.PopID();
    }

    void StepLayer(string layerId, int n, int delta, int currentIndex)
    {
        if (n <= 0)
            return;
        int next = currentIndex + delta;
        if (next < 0) next = n - 1;
        if (next >= n) next = 0;
        _overrides.Layers[layerId] = next;
    }

    void DrawPaletteRow(string name, System.Collections.Generic.IReadOnlyList<Vector4> colours)
    {
        int n = colours.Count;
        if (n == 0)
            return;

        int index;
        if (_overrides.Palettes.TryGetValue(name, out var pinned))
            index = ((pinned % n) + n) % n;
        else
            index = SeedPaletteIndex(name, n);

        var colour = colours[index];
        ImGui.PushID("palette-" + name);
        if (ImGui.Button("<"))
            _overrides.Palettes[name] = (index - 1 + n) % n;
        ImGui.SameLine();
        ImGui.ColorButton("##swatch", colour, ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoDragDrop, new Vector2(18, 18));
        ImGui.SameLine();
        ImGui.TextUnformatted($"{name}  {index + 1}/{n}");
        ImGui.SameLine();
        if (ImGui.Button(">"))
            _overrides.Palettes[name] = (index + 1) % n;
        ImGui.PopID();
    }

    int SeedPaletteIndex(string name, int n)
    {
        // Reuse the same seeded pick as the preview so the k/N starts on the rolled colour.
        var catalog = PortraitRenderer.Catalog;
        var picks = PortraitPicker.Pick(catalog, _seed, _role, PortraitIdentity.DefaultFactionColour, _species);
        // The picker doesn't expose palette indices; recover by comparing the tint of a layer
        // that uses this palette. Fall back to 0 when none does.
        var pack = catalog.ResolveSpecies(_species);
        if (!pack.Palettes.TryGetValue(name, out var colours) || colours.Count == 0)
            return 0;
        for (int i = 0; i < pack.Layers.Count && i < picks.Count; i++)
        {
            if (!string.Equals(pack.Layers[i].Tint, name, StringComparison.OrdinalIgnoreCase))
                continue;
            var tint = picks[i].Tint;
            for (int c = 0; c < colours.Count; c++)
            {
                if (colours[c] == tint)
                    return c;
            }
        }
        return 0;
    }
}
