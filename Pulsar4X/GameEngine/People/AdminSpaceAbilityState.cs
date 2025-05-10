using Pulsar4X.Components;
using Pulsar4X.People;

namespace GameEngine.People;

public class AdminSpaceAbilityState : ComponentAbilityState
{
    internal CommanderDB Commander { get;  set; }

    public bool TryGetCommander(out CommanderDB commander)
    {
        commander = Commander;
        if (Commander == null)
            return false;
        return true;
    }
    
    public AdminSpaceAbilityState(ComponentInstance componentInstance) : base(componentInstance)
    {
        
    }
}