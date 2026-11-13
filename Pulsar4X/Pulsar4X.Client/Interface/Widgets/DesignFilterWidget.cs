using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client.Interface.Widgets;

/// <summary>
/// A compact Category / Type / search filter for design pickers (component templates, industry jobs).
/// State is explicit so each list (templates, refinery, factory, shipyard) can keep its own instance.
/// Filter() has no ImGui dependency.
/// </summary>
public sealed class DesignFilterWidget
{
    public const string All = "All";

    public string SelectedCategory { get; set; } = All;
    public string SelectedType { get; set; } = All;
    public string SearchText { get; set; } = "";

    public void Reset()
    {
        SelectedCategory = All;
        SelectedType = All;
        SearchText = "";
    }

    public bool IsAllCategory => IsAll(SelectedCategory);
    public bool IsAllType => IsAll(SelectedType);

    /// <summary>
    /// Categories implied by a template or design's mount flags. A design can belong to more than
    /// one category (e.g. Ship + Colony when it can mount as both a ship component and a planetary
    /// installation).
    /// </summary>
    public static IReadOnlyList<string> MountCategories(ComponentMountFlags flags)
    {
        var categories = new List<string>(4);
        if ((flags & (ComponentMountFlags.ShipComponent | ComponentMountFlags.Fighter)) != 0)
            categories.Add("Ship");
        if ((flags & ComponentMountFlags.PlanetInstallation) != 0)
            categories.Add("Colony");
        if ((flags & ComponentMountFlags.Missile) != 0)
            categories.Add("Missile");
        if (categories.Count == 0 && (flags & ComponentMountFlags.ShipCargo) != 0)
            categories.Add("Cargo");
        return categories;
    }

    /// <summary>Categories for an industry-line constructible (ship design, material, or component).</summary>
    public static IReadOnlyList<string> ConstructibleCategories(ConstructibleItemView item)
    {
        if (item.IsShipDesign)
            return new[] { "Ship Design" };
        if (item.IsMaterial)
            return new[] { "Material" };
        return MountCategories(item.MountFlags);
    }

    public static DesignFilterEntry DescribeTemplate(ComponentTemplateSummary template)
        => new(template.Name, MountCategories(template.MountFlags), template.ComponentType, template.Description);

    public static DesignFilterEntry DescribeConstructible(ConstructibleItemView item)
        => new(item.Name, ConstructibleCategories(item), item.ComponentType,
            string.IsNullOrEmpty(item.CrewWarning) ? null : item.CrewWarning);

    /// <summary>
    /// If the current items no longer contain the selected category or type, reset that
    /// selection to All. Returns whether anything changed.
    /// </summary>
    public bool Sanitize<T>(IReadOnlyList<T> items, Func<T, DesignFilterEntry> describe)
    {
        bool changed = false;
        var (categories, types) = DistinctValues(items, describe);
        if (!IsAll(SelectedCategory) && !ContainsIgnoreCase(categories, SelectedCategory))
        {
            SelectedCategory = All;
            changed = true;
        }
        if (!IsAll(SelectedType) && !ContainsIgnoreCase(types, SelectedType))
        {
            SelectedType = All;
            changed = true;
        }
        return changed;
    }

    /// <summary>
    /// Applies the current category, type, and search text. Sanitizes stale selections first
    /// so a vanished category or type does not hide every item.
    /// </summary>
    public List<T> Filter<T>(IReadOnlyList<T> items, Func<T, DesignFilterEntry> describe)
    {
        Sanitize(items, describe);
        if (items == null || items.Count == 0)
            return new List<T>();

        var result = new List<T>(items.Count);
        string search = SearchText ?? "";
        bool hasSearch = search.Length > 0;
        foreach (var item in items)
        {
            var entry = describe(item);
            if (!IsAll(SelectedCategory) && !entry.HasCategory(SelectedCategory))
                continue;
            if (!IsAll(SelectedType) && !EqualsIgnoreCase(entry.Type, SelectedType))
                continue;
            if (hasSearch && !entry.MatchesSearch(search))
                continue;
            result.Add(item);
        }
        return result;
    }

    /// <summary>
    /// Draws the compact Category combo (only when more than one category exists), Type combo,
    /// and search box. Returns true when the filters changed this frame.
    /// </summary>
    public bool Draw<T>(string id, IReadOnlyList<T> items, Func<T, DesignFilterEntry> describe, out List<T> filtered)
    {
        bool changed = Sanitize(items, describe);
        var (categories, types) = DistinctValues(items, describe);

        ImGui.PushID(id);
        try
        {
            if (categories.Count > 1)
            {
                string preview = "Category: " + (IsAll(SelectedCategory) ? All : SelectedCategory);
                ImGui.SetNextItemWidth(-1);
                if (ImGui.BeginCombo("##category", preview))
                {
                    if (ImGui.Selectable(All, IsAll(SelectedCategory)))
                    {
                        if (!IsAll(SelectedCategory))
                        {
                            SelectedCategory = All;
                            changed = true;
                        }
                    }
                    foreach (var category in categories)
                    {
                        if (ImGui.Selectable(category, EqualsIgnoreCase(SelectedCategory, category)))
                        {
                            if (!EqualsIgnoreCase(SelectedCategory, category))
                            {
                                SelectedCategory = category;
                                changed = true;
                            }
                        }
                    }
                    ImGui.EndCombo();
                }
            }

            string typePreview = "Type: " + (IsAll(SelectedType) ? All : SelectedType);
            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo("##type", typePreview))
            {
                if (ImGui.Selectable(All, IsAll(SelectedType)))
                {
                    if (!IsAll(SelectedType))
                    {
                        SelectedType = All;
                        changed = true;
                    }
                }
                foreach (var type in types)
                {
                    if (ImGui.Selectable(type, EqualsIgnoreCase(SelectedType, type)))
                    {
                        if (!EqualsIgnoreCase(SelectedType, type))
                        {
                            SelectedType = type;
                            changed = true;
                        }
                    }
                }
                ImGui.EndCombo();
            }

            string search = SearchText ?? "";
            float clearWidth = string.IsNullOrEmpty(search) ? 0f : ImGui.GetFrameHeight();
            float gap = clearWidth > 0 ? ImGui.GetStyle().ItemInnerSpacing.X : 0f;
            ImGui.SetNextItemWidth(Math.Max(16f, ImGui.GetContentRegionAvail().X - clearWidth - gap));
            if (ImGui.InputTextWithHint("##search", "Search...", ref search, 128))
            {
                SearchText = search;
                changed = true;
            }
            if (!string.IsNullOrEmpty(SearchText))
            {
                ImGui.SameLine(0, gap);
                if (ImGui.SmallButton("x##clear"))
                {
                    SearchText = "";
                    changed = true;
                }
            }
        }
        finally
        {
            ImGui.PopID();
        }

        filtered = Filter(items, describe);
        return changed;
    }

    static (List<string> categories, List<string> types) DistinctValues<T>(IReadOnlyList<T>? items, Func<T, DesignFilterEntry> describe)
    {
        var categories = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var types = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        if (items != null)
        {
            foreach (var item in items)
            {
                var entry = describe(item);
                foreach (var category in entry.Categories)
                {
                    if (!string.IsNullOrWhiteSpace(category))
                        categories.Add(category);
                }
                if (!string.IsNullOrWhiteSpace(entry.Type))
                    types.Add(entry.Type);
            }
        }
        return (categories.ToList(), types.ToList());
    }

    static bool IsAll(string? value)
        => string.IsNullOrEmpty(value) || string.Equals(value, All, StringComparison.OrdinalIgnoreCase);

    static bool EqualsIgnoreCase(string? a, string? b)
        => string.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase);

    static bool ContainsIgnoreCase(IEnumerable<string> values, string? wanted)
        => values.Any(v => EqualsIgnoreCase(v, wanted));
}

/// <summary>One list item's searchable fields for <see cref="DesignFilterWidget"/>.</summary>
public readonly struct DesignFilterEntry
{
    public string Name { get; }
    public IReadOnlyList<string> Categories { get; }
    public string Type { get; }
    public string? Extra { get; }

    /// <summary>First category, or empty when the item has none. Kept for callers of the single-category constructor.</summary>
    public string Category => Categories.Count == 0 ? "" : Categories[0];

    /// <summary>Single-category constructor; a null or whitespace category becomes an empty list.</summary>
    public DesignFilterEntry(string Name, string Category, string Type, string? Extra = null)
        : this(Name, string.IsNullOrWhiteSpace(Category) ? Array.Empty<string>() : new[] { Category }, Type, Extra)
    {
    }

    public DesignFilterEntry(string Name, IReadOnlyList<string>? Categories, string Type, string? Extra = null)
    {
        this.Name = Name ?? "";
        this.Type = Type ?? "";
        this.Extra = Extra;
        this.Categories = SanitizeCategories(Categories);
    }

    public bool HasCategory(string? selected)
    {
        if (string.IsNullOrEmpty(selected))
            return false;
        foreach (var category in Categories)
        {
            if (string.Equals(category, selected, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public bool MatchesSearch(string search)
    {
        if (string.IsNullOrEmpty(search))
            return true;
        if (Contains(Name, search) || Contains(Type, search) || Contains(Extra, search))
            return true;
        foreach (var category in Categories)
        {
            if (Contains(category, search))
                return true;
        }
        return false;
    }

    static IReadOnlyList<string> SanitizeCategories(IReadOnlyList<string>? categories)
    {
        if (categories == null || categories.Count == 0)
            return Array.Empty<string>();

        var cleaned = new List<string>(categories.Count);
        foreach (var category in categories)
        {
            if (string.IsNullOrWhiteSpace(category))
                continue;
            bool exists = false;
            foreach (var already in cleaned)
            {
                if (string.Equals(already, category, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }
            if (!exists)
                cleaned.Add(category);
        }
        return cleaned.Count == 0 ? Array.Empty<string>() : cleaned;
    }

    static bool Contains(string? haystack, string needle)
        => !string.IsNullOrEmpty(haystack)
           && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
