namespace GameEngine.Engine.Orders;

/// <summary>
/// Commander skill tracks. Not GoalTypes — map via <see cref="DomainOf"/>.
/// First pass: Command, Nav, Survey only.
/// </summary>
public enum SkillDomain
{
    Command,
    Nav,
    Survey,
}

public static class SkillDomains
{
    /// <summary>
    /// Leaf <see cref="GoalType.MoveTo"/> is Nav; geo/grav survey is Survey.
    /// Fleet rollup of any of those is Command. Unknown types: no domain (no XP).
    /// </summary>
    public static SkillDomain? DomainOf(GoalType type, bool isFleet)
    {
        if (isFleet)
        {
            return type is GoalType.MoveTo or GoalType.ServeyBodies or GoalType.ScanAnomalies
                or GoalType.FleetTrade or GoalType.FleetFreighter or GoalType.FleetMoveGoods
                ? SkillDomain.Command
                : null;
        }

        return type switch
        {
            GoalType.MoveTo => SkillDomain.Nav,
            GoalType.ServeyBodies or GoalType.ScanAnomalies => SkillDomain.Survey,
            _ => null,
        };
    }

    /// <summary>Player-facing line for the person card. One effect, not the grant rule.</summary>
    public static string Effect(SkillDomain domain) => domain switch
    {
        SkillDomain.Command => "Fleet orders pass down faster.",
        SkillDomain.Nav => "Warp drops lead the target.",
        SkillDomain.Survey => "Surveys finish faster.",
        _ => "",
    };
}
