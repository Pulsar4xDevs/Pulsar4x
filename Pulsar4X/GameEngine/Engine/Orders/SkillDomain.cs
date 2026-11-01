namespace GameEngine.Engine.Orders;

/// <summary>
/// Commander skill tracks. Not GoalTypes — map via <see cref="DomainOf"/>.
/// Command, Nav and Survey came first. Construction is last so saved skill
/// dictionaries keep their meaning.
/// </summary>
public enum SkillDomain
{
    Command,
    Nav,
    Survey,
    Construction,
}

public static class SkillDomains
{
    /// <summary>
    /// Leaf <see cref="GoalType.MoveTo"/> is Nav; geo/grav survey is Survey;
    /// <see cref="GoalType.BootstrapColony"/> is Construction.
    /// Fleet rollup of any of those is Command. Unknown types: no domain (no XP).
    /// </summary>
    public static SkillDomain? DomainOf(GoalType type, bool isFleet)
    {
        if (isFleet)
        {
            return type is GoalType.MoveTo or GoalType.ServeyBodies or GoalType.ScanAnomalies
                or GoalType.FleetTrade or GoalType.FleetFreighter or GoalType.FleetMoveGoods or GoalType.FleetBootstrap
                ? SkillDomain.Command
                : null;
        }

        return type switch
        {
            GoalType.MoveTo => SkillDomain.Nav,
            GoalType.ServeyBodies or GoalType.ScanAnomalies => SkillDomain.Survey,
            GoalType.BootstrapColony => SkillDomain.Construction,
            _ => null,
        };
    }

    /// <summary>Player-facing line for the person card. One effect, not the grant rule.</summary>
    public static string Effect(SkillDomain domain) => domain switch
    {
        SkillDomain.Command => "Fleet orders pass down faster.",
        SkillDomain.Nav => "Warp drops lead the target.",
        SkillDomain.Survey => "Surveys finish faster.",
        SkillDomain.Construction => "Grows by bootstrapping colonies. No effect on play yet.",
        _ => "",
    };
}
