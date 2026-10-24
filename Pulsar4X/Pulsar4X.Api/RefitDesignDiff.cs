namespace Pulsar4X.Api;

/// <summary>
/// Compares a ship to a target design by component design id and count.
/// Armor is not part of the comparison.
/// </summary>
public static class RefitDesignDiff
{
    public static RefitDesignDiffResult Compute(
        RefitTargetView target,
        IReadOnlyList<RefitPartView> onShip,
        IReadOnlyList<RefitPartView> cargo)
    {
        var wanted = new Dictionary<string, int>();
        var names = new Dictionary<string, string>();
        foreach (var part in target.Parts)
        {
            if (part.Count <= 0 || string.IsNullOrEmpty(part.DesignId))
                continue;
            wanted.TryGetValue(part.DesignId, out var count);
            wanted[part.DesignId] = count + part.Count;
            names[part.DesignId] = part.Name;
        }

        var aboard = Group(onShip);
        var stored = Group(cargo);
        var order = new List<string>();
        foreach (var part in target.Parts)
        {
            if (!string.IsNullOrEmpty(part.DesignId) && !order.Contains(part.DesignId))
                order.Add(part.DesignId);
        }
        foreach (var part in onShip)
        {
            if (!string.IsNullOrEmpty(part.DesignId) && !order.Contains(part.DesignId))
                order.Add(part.DesignId);
        }

        var remove = new List<int>();
        var mount = new List<int>();
        var missing = new List<RefitMissingPartView>();
        foreach (var designId in order)
        {
            wanted.TryGetValue(designId, out var need);
            if (!aboard.TryGetValue(designId, out var onBoard))
                onBoard = new List<RefitPartView>();
            if (onBoard.Count > need)
            {
                for (int i = need; i < onBoard.Count; i++)
                    remove.Add(onBoard[i].ComponentId);
            }

            int shortBy = need - onBoard.Count;
            if (shortBy <= 0)
                continue;
            if (!stored.TryGetValue(designId, out var inCargo))
                inCargo = new List<RefitPartView>();
            int take = Math.Min(shortBy, inCargo.Count);
            for (int i = 0; i < take; i++)
                mount.Add(inCargo[i].ComponentId);
            int still = shortBy - take;
            if (still > 0)
            {
                names.TryGetValue(designId, out var name);
                missing.Add(new RefitMissingPartView(
                    designId, string.IsNullOrEmpty(name) ? designId : name, still));
            }
        }

        return new RefitDesignDiffResult(remove, mount, missing);
    }

    static Dictionary<string, List<RefitPartView>> Group(IReadOnlyList<RefitPartView> parts)
    {
        var grouped = new Dictionary<string, List<RefitPartView>>();
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part.DesignId))
                continue;
            if (!grouped.TryGetValue(part.DesignId, out var list))
            {
                list = new List<RefitPartView>();
                grouped[part.DesignId] = list;
            }
            list.Add(part);
        }
        return grouped;
    }
}
