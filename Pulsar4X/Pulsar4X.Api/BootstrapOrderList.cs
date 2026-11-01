namespace Pulsar4X.Api;

/// <summary>One line of the fleet Bootstrap crate list.</summary>
public sealed class BootstrapOrderEntry
{
    public string DesignId { get; }
    public string Name { get; }
    public int Count { get; set; }

    public BootstrapOrderEntry(string designId, string name, int count)
    {
        DesignId = designId;
        Name = name;
        Count = count;
    }
}

/// <summary>
/// The editable, prioritized crate list behind the fleet Bootstrap order. Top of the list ships first.
/// It reads snapshots only, so the client draws it with up/down buttons and tests run it without a window.
/// </summary>
public sealed class BootstrapOrderList
{
    public const int MaxCount = 999;

    readonly List<BootstrapOrderEntry> _entries = new();

    public IReadOnlyList<BootstrapOrderEntry> Entries => _entries;

    /// <summary>The mod's default list, in its order. Designs the faction cannot crate are already dropped.</summary>
    public static BootstrapOrderList FromDefaults(ComponentDesignsSnapshot designs)
    {
        var list = new BootstrapOrderList();
        foreach (var line in designs.BootstrapDefaults)
        {
            var design = designs.Designs.FirstOrDefault(d => d.Id == line.DesignId);
            if (design != null && design.IsCrate)
                list.Add(design.Id, design.Name, line.Count);
        }
        return list;
    }

    /// <summary>
    /// Adds a design at the bottom. A design already on the list gains the count instead, so a design
    /// never appears twice. Returns false for a count below one.
    /// </summary>
    public bool Add(string designId, string name, int count = 1)
    {
        if (count < 1)
            return false;
        var same = _entries.FirstOrDefault(e => e.DesignId == designId);
        if (same != null)
            same.Count = Clamp(same.Count + count);
        else
            _entries.Add(new BootstrapOrderEntry(designId, name, Clamp(count)));
        return true;
    }

    /// <summary>Crate designs the faction has that are not on the list yet, by name.</summary>
    public IReadOnlyList<ComponentDesignSummary> Addable(ComponentDesignsSnapshot designs)
        => designs.Designs
            .Where(d => d.IsCrate && _entries.All(e => e.DesignId != d.Id))
            .OrderBy(d => d.Name, StringComparer.Ordinal)
            .ThenBy(d => d.Id, StringComparer.Ordinal)
            .ToList();

    /// <summary>Moves a line one place toward the top, which raises its priority.</summary>
    public bool MoveUp(int index) => Swap(index, index - 1);

    /// <summary>Moves a line one place toward the bottom.</summary>
    public bool MoveDown(int index) => Swap(index, index + 1);

    public bool Remove(int index)
    {
        if (index < 0 || index >= _entries.Count)
            return false;
        _entries.RemoveAt(index);
        return true;
    }

    /// <summary>Sets a line's quantity, held between one and <see cref="MaxCount"/>. Remove the line to drop it.</summary>
    public void SetCount(int index, int count)
    {
        if (index >= 0 && index < _entries.Count)
            _entries[index].Count = Clamp(count);
    }

    /// <summary>The lines to send, in priority order.</summary>
    public List<BootstrapCrateRequest> Requests()
        => _entries.Select(e => new BootstrapCrateRequest(e.DesignId, e.Count)).ToList();

    /// <summary>
    /// Why the fleet order cannot be sent yet, or null. <paramref name="inReach"/> is the client's span test
    /// of the pair. <paramref name="capableShips"/> counts the fleet's captained ships with a Construction Bay.
    /// </summary>
    public static string? Validate(int capableShips, int sourceId, int destId, IReadOnlyList<BootstrapCrateRequest> lines, bool inReach)
    {
        if (capableShips == 0)
            return "No captained ship in this fleet has a Construction Bay.";
        if (sourceId < 0)
            return "Pick a source colony.";
        if (destId < 0)
            return "Pick a destination colony.";
        if (sourceId == destId)
            return "Pick two different colonies.";
        if (lines.Count == 0)
            return "Nothing to build.";
        foreach (var line in lines)
        {
            if (line.Count <= 0)
                return "Crate counts must be positive.";
        }
        if (!inReach)
            return "The target is outside this fleet's reach.";
        return null;
    }

    bool Swap(int a, int b)
    {
        if (a < 0 || b < 0 || a >= _entries.Count || b >= _entries.Count)
            return false;
        (_entries[a], _entries[b]) = (_entries[b], _entries[a]);
        return true;
    }

    static int Clamp(int count) => Math.Min(Math.Max(count, 1), MaxCount);
}

/// <summary>
/// The site-only facilities the player wants built at the destination, beside the crate list. The player picks
/// facilities and quantities only. The resources the fleet hauls for them are derived here for display
/// (cost x quantity), and the server derives the same sums from the design costs when it plans the hauls.
/// </summary>
public sealed class BootstrapSiteBuildList
{
    readonly List<BootstrapOrderEntry> _entries = new();

    public IReadOnlyList<BootstrapOrderEntry> Entries => _entries;

    /// <summary>Adds a facility, or raises the count of one already listed. False for a count below one.</summary>
    public bool Add(string designId, string name, int count = 1)
    {
        if (count < 1)
            return false;
        var same = _entries.FirstOrDefault(e => e.DesignId == designId);
        if (same != null)
            same.Count = Clamp(same.Count + count);
        else
            _entries.Add(new BootstrapOrderEntry(designId, name, Clamp(count)));
        return true;
    }

    /// <summary>Site-only designs the faction has that are not listed yet, by name.</summary>
    public IReadOnlyList<ComponentDesignSummary> Addable(ComponentDesignsSnapshot designs)
        => designs.Designs
            .Where(d => d.IsSiteOnly && _entries.All(e => e.DesignId != d.Id))
            .OrderBy(d => d.Name, StringComparer.Ordinal)
            .ThenBy(d => d.Id, StringComparer.Ordinal)
            .ToList();

    public bool Remove(int index)
    {
        if (index < 0 || index >= _entries.Count)
            return false;
        _entries.RemoveAt(index);
        return true;
    }

    public void SetCount(int index, int count)
    {
        if (index >= 0 && index < _entries.Count)
            _entries[index].Count = Clamp(count);
    }

    public void Clear() => _entries.Clear();

    public List<BootstrapCrateRequest> Requests()
        => _entries.Select(e => new BootstrapCrateRequest(e.DesignId, e.Count)).ToList();

    /// <summary>What the fleet will haul: every listed facility's resource cost times its count, summed per resource.</summary>
    public List<ResourceCostLine> DerivedResources(ComponentDesignsSnapshot designs)
    {
        var totals = new List<ResourceCostLine>();
        foreach (var entry in _entries)
        {
            var design = designs.Designs.FirstOrDefault(d => d.Id == entry.DesignId);
            if (design == null)
                continue;
            foreach (var cost in design.ResourceCosts)
            {
                long amount = cost.Amount * entry.Count;
                int at = totals.FindIndex(t => t.CargoId == cost.CargoId);
                if (at >= 0)
                    totals[at] = totals[at] with { Amount = totals[at].Amount + amount };
                else
                    totals.Add(new ResourceCostLine(cost.CargoId, cost.Name, amount));
            }
        }
        return totals;
    }

    static int Clamp(int count) => Math.Min(Math.Max(count, 1), BootstrapOrderList.MaxCount);
}
