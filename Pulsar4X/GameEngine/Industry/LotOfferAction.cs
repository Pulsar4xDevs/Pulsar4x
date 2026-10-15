using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Newtonsoft.Json;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Logistics;
using Pulsar4X.Storage;

namespace Pulsar4X.Industry;

/// <summary>
/// Moves every carried mineral into one office and pays <see cref="CeresStart.LotPrice"/> once.
/// Does not write a market row. A line that cannot move puts the lot back and does not fail the mine.
/// </summary>
public class LotOfferAction : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InteractWithExternalEntity;

    public override bool IsBlocking => true;

    public override string Name => "Direct offer";

    public override string Details => _details;

    string _details = "";

    [JsonProperty]
    public int OfficeEntityId { get; set; }

    Entity? _entityCommanding;

    internal override Entity EntityCommanding => _entityCommanding!;

    public static LotOfferAction Create(Entity ship, int officeEntityId)
    {
        return new LotOfferAction
        {
            RequestingFactionGuid = ship.FactionOwnerID,
            EntityCommandingGuid = ship.Id,
            OfficeEntityId = officeEntityId,
            CreatedDate = ship.Manager.ManagerSubpulses.StarSysDateTime,
        };
    }

    internal override void Execute(DateTime atDateTime)
    {
        if (_isFinished)
            return;
        if (_entityCommanding == null)
        {
            Finish("Cannot trade");
            return;
        }

        var game = _entityCommanding.Manager.Game;
        if (!game.GlobalManager.TryGetGlobalEntityById(OfficeEntityId, out var office))
        {
            Finish("No office");
            return;
        }

        int shipFaction = _entityCommanding.FactionOwnerID;
        int officeFaction = office.FactionOwnerID;
        if (!FactionStanceRules.CanTrade(game, shipFaction, officeFaction))
        {
            Finish("Trade is not mutual.");
            return;
        }

        if (!MarketExchangeAction.InRange(_entityCommanding, office))
        {
            Finish("Out of range");
            return;
        }

        if (!_entityCommanding.TryGetDataBlob<CargoStorageDB>(out var shipStore)
            || !office.TryGetDataBlob<CargoStorageDB>(out var officeStore))
        {
            Finish("No store");
            return;
        }

        var carried = Carried(game, shipFaction, shipStore);
        if (carried.Count == 0)
        {
            Finish("Nothing to sell");
            return;
        }

        long units = 0;
        foreach (var line in carried)
            units += line.Units;

        if (shipFaction != officeFaction
            && MarketExchangeAction.Affordable(game, officeFaction, CeresStart.LotPrice) < units)
        {
            Finish("Cannot pay");
            return;
        }

        var moved = new List<Line>();
        foreach (var line in carried)
        {
            if (!TryMineral(game, officeFaction, line.ShipMineral.UniqueID, out var officeMineral))
            {
                Restore(shipStore, officeStore, moved);
                Finish("Could not move the lot");
                return;
            }

            long removed = shipStore.RemoveCargoByUnit(line.ShipMineral, line.Units);
            if (removed != line.Units)
            {
                if (removed > 0)
                    shipStore.AddCargoByUnit(line.ShipMineral, removed);
                Restore(shipStore, officeStore, moved);
                Finish("Could not move the lot");
                return;
            }

            long added = officeStore.AddCargoByUnit(officeMineral, removed);
            if (added != removed)
            {
                if (added > 0)
                    officeStore.RemoveCargoByUnit(officeMineral, added);
                shipStore.AddCargoByUnit(line.ShipMineral, removed);
                Restore(shipStore, officeStore, moved);
                Finish("Could not move the lot");
                return;
            }

            moved.Add(new Line(line.ShipMineral, officeMineral, added));
        }

        if (shipFaction != officeFaction
            && game.Factions.TryGetValue(shipFaction, out var shipFactionEntity)
            && game.Factions.TryGetValue(officeFaction, out var officeFactionEntity)
            && shipFactionEntity.TryGetDataBlob<FactionInfoDB>(out var shipInfo)
            && officeFactionEntity.TryGetDataBlob<FactionInfoDB>(out var officeInfo))
        {
            decimal amount = CeresStart.LotPrice * units;
            string forShip = $"minerals {units} {officeFaction}";
            string forOffice = $"minerals {units} {shipFaction}";
            officeInfo.Money.AddExpense(atDateTime, TransactionCategory.Trade, forOffice, amount);
            shipInfo.Money.AddIncome(atDateTime, TransactionCategory.Trade, forShip, amount);
        }

        CargoTransferProcessor.UpdateMassFuelAndDeltaV(_entityCommanding);
        CargoTransferProcessor.UpdateMassFuelAndDeltaV(office);
        Finish($"Sold {units} minerals");
    }

    internal override bool IsFinished() => _isFinished;

    internal override bool IsValidCommand(Game game)
    {
        return CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _, out _entityCommanding);
    }

    public override EntityAction Clone()
    {
        return new LotOfferAction
        {
            OfficeEntityId = OfficeEntityId,
            RequestingFactionGuid = RequestingFactionGuid,
            EntityCommandingGuid = EntityCommandingGuid,
            CreatedDate = CreatedDate,
            ActionOnDate = ActionOnDate,
            ParentGoalId = ParentGoalId,
            UseActionLanes = UseActionLanes,
        };
    }

    void Finish(string message)
    {
        _details = message;
        _isFinished = true;
    }

    static void Restore(CargoStorageDB shipStore, CargoStorageDB officeStore, List<Line> moved)
    {
        for (int i = moved.Count - 1; i >= 0; i--)
        {
            var line = moved[i];
            long removed = officeStore.RemoveCargoByUnit(line.OfficeMineral, line.Units);
            if (removed > 0)
                shipStore.AddCargoByUnit(line.ShipMineral, removed);
        }
    }

    static List<Line> Carried(Game game, int factionId, CargoStorageDB cargo)
    {
        var carried = new List<Line>();
        if (!game.Factions.TryGetValue(factionId, out var faction)
            || !faction.TryGetDataBlob<FactionInfoDB>(out var info))
            return carried;

        var library = info.Data.CargoGoods;
        foreach (var store in cargo.TypeStores.Values)
        {
            foreach (var (id, units) in store.CurrentStoreInUnits)
            {
                if (units <= 0 || !library.GetMinerals().TryGetValue(id, out var mineral) || mineral == null)
                    continue;
                carried.Add(new Line(mineral, mineral, units));
            }
        }

        return carried;
    }

    static bool TryMineral(Game game, int factionId, string uniqueId, out Mineral mineral)
    {
        mineral = null!;
        if (string.IsNullOrEmpty(uniqueId))
            return false;
        if (!game.Factions.TryGetValue(factionId, out var faction)
            || !faction.TryGetDataBlob<FactionInfoDB>(out var info))
            return false;
        if (!info.Data.CargoGoods.IsMineral(uniqueId))
            return false;
        mineral = info.Data.CargoGoods.GetMineral(uniqueId);
        return true;
    }

    readonly struct Line
    {
        public Line(Mineral shipMineral, Mineral officeMineral, long units)
        {
            ShipMineral = shipMineral;
            OfficeMineral = officeMineral;
            Units = units;
        }

        public Mineral ShipMineral { get; }
        public Mineral OfficeMineral { get; }
        public long Units { get; }
    }
}
