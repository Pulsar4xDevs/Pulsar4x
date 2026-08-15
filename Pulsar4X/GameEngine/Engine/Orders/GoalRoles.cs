namespace GameEngine.Engine.Orders;

/// <summary>
/// Classifies a <see cref="GoalType"/> for the weighting / planner pipeline.
/// One enum of kinds; role is metadata — not a second GoalType list.
/// </summary>
public enum GoalRole
{
    /// <summary>Standing concern. Feeds context weights; rarely a concrete GivenGoal.</summary>
    Drive,

    /// <summary>Has (or should have) an <see cref="IGoalPlanner"/>; can be assigned and produce actions.</summary>
    Task,

    /// <summary>Soft bias on other goals (help, stealth) more than a mission in itself.</summary>
    Stance,
}

public static class GoalRoles
{
    public static GoalRole RoleOf(GoalType type) => type switch
    {
        GoalType.StayAlive or GoalType.DontRunOutOfFuel => GoalRole.Drive,

        GoalType.HelpOwn or GoalType.HelpAllied or GoalType.HelpFrendly
            or GoalType.HelpNeutral or GoalType.Stealth => GoalRole.Stance,

        _ => GoalRole.Task,
    };

    public static bool IsDrive(GoalType type) => RoleOf(type) == GoalRole.Drive;
    public static bool IsTask(GoalType type) => RoleOf(type) == GoalRole.Task;
}
