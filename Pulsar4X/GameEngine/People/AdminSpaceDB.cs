using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;

namespace GameEngine.People;

public class AdminSpaceDB : BaseDataBlob
{
    /// <summary>
    ///  
    /// </summary>
    public List<AdminSpaceAbilityState> CommanderSeats { get; internal set; } =  new List<AdminSpaceAbilityState>();
    
    public AdminSpaceDB() { }
}