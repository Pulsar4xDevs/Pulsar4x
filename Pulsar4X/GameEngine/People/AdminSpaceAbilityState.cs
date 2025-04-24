using Pulsar4X.Components;
using Pulsar4X.People;

namespace GameEngine.People;

public class AdminSpaceAbilityState : ComponentAbilityState
{
    public CommanderDB Commander { get; internal set; }
}