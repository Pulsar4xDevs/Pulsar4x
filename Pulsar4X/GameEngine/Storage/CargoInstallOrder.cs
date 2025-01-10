using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Names;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Ships;


namespace Pulsar4X.Storage;

public class CargoInstallOrder : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement | ActionLaneTypes.InteractWithExternalEntity;

    public override bool IsBlocking => true;

    public override string Name { get; } = "Facility Installation";

    public override string Details
    {
        get
        {
            var facName = facilityComponent.Name;
            var installOn = InstallOnEntity.GetOwnersName();
            return "Install " + facName + " on " + installOn;
        }
    }

    Entity _entityCommanding;

    internal override Entity EntityCommanding { get { return _entityCommanding; } }
    internal Entity InstallOnEntity { get; set; }

    internal ComponentInstance facilityComponent { get; private set; }

    [JsonIgnore]
    Entity factionEntity;
    

    private CargoInstallOrder()
    {

    }
    
    public static void CreateCommand(int faction, Entity cargoFrom, Entity installOn, ComponentInstance facility )
    {
        var cmd1 = new CargoInstallOrder()
        {
            RequestingFactionGuid = faction,
            EntityCommandingGuid = cargoFrom.Id,
            CreatedDate = cargoFrom.Manager.ManagerSubpulses.StarSysDateTime,
            InstallOnEntity = installOn,
            facilityComponent = facility
        };
        cargoFrom.Manager.Game.OrderHandler.HandleOrder(cmd1);
    }
    

    
    /// <summary>
    /// Validates and actions the command.
    /// may eventualy need to return a responce instead of void.
    /// This creates a CargoTranferDB from the command, which does all the work.
    /// the command is to create and enqueue a CargoTransferDB.
    /// </summary>
    internal override void Execute(DateTime atDateTime)
    {
        if (!IsRunning)
        {
            InstallOnEntity.AddComponent(facilityComponent);
            ReCalcProcessor.ReCalcAbilities(InstallOnEntity);
            CargoMath.RemoveCargoByUnit(InstallOnEntity.GetDataBlob<CargoStorageDB>(), facilityComponent, 1);
            IsRunning = true;
            _isFinished = true;
        }
    }

    internal override bool IsValidCommand(Game game)
    {
        if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out factionEntity, out _entityCommanding))
        {
            return true;
        }
        return false;
    }

    internal override bool IsFinished()
    {
        return _isFinished;
    }
    
    
    public override EntityCommand Clone()
    {
        throw new NotImplementedException();
    }
    
}