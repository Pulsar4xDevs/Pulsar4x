using Pulsar4X.People;

namespace GameEngine.People;

public class AdminSpaceAbilityState// : ComponentAbilityState
{
    public string ComponentName  { get; internal set; }

    public int CommanderID { get; internal set; } = -1;
    internal CommanderDB Commander { get;  set; }
    public AdminLevel SeatType { get; internal set; }
    public bool TryGetCommander(out CommanderDB commander)
    {
        commander = Commander;
        if (Commander == null)
            return false;
        return true;
    }


    public AdminSpaceAbilityState(AdminLevel type, string componentName)
    {
        SeatType = type;
        ComponentName = componentName;
    }
}