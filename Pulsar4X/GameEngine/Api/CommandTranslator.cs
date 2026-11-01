using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using GameEngine.People;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Industry;
using Pulsar4X.Industry.Orders;
using Pulsar4X.JumpPoints;
using Pulsar4X.Logistics;
using Pulsar4X.Messaging;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;
using Pulsar4X.Technology;

namespace Pulsar4X.Engine.Api
{
    /// <summary>
    /// Translates authorized API <see cref="GameCommand"/> DTOs into engine orders and dispatches them.
    /// <see cref="EngineGameServer"/> handles auth (resolving the faction/commanded entity and checking
    /// ownership) and delegates the per-command mapping here, so the command surface grows in one
    /// isolated place. Adding a command is one DTO (in Pulsar4X.Api) + one entry in <see cref="_translators"/>
    /// and a <c>Translate*</c> method. Secondary targets (a move destination, a ship to assign, …) travel
    /// as DTO fields and are resolved — and visibility-checked where applicable — here.
    /// </summary>
    internal sealed class CommandTranslator
    {
        private readonly Game _game;
        private readonly Dictionary<Type, Func<Entity, Entity, GameCommand, CommandResult>> _translators;

        public CommandTranslator(Game game)
        {
            _game = game;
            _translators = new Dictionary<Type, Func<Entity, Entity, GameCommand, CommandResult>>
            {
                [typeof(Pulsar4X.Api.RenameCommand)] = TranslateRename,
                [typeof(SetFactionStanceCommand)] = TranslateSetFactionStance,
                [typeof(SetFactionGrantCommand)] = TranslateSetFactionGrant,
                [typeof(SetMarketPolicyCommand)] = TranslateSetMarketPolicy,
                [typeof(ClearMarketPolicyCommand)] = TranslateClearMarketPolicy,
                [typeof(RunMarketCommand)] = TranslateRunMarket,
                [typeof(SupplyLocalCommand)] = TranslateSupplyLocal,
                [typeof(TradeCommand)] = TranslateTrade,
                [typeof(FreighterCommand)] = TranslateFreighter,
                [typeof(FleetTradeCommand)] = TranslateFleetTrade,
                [typeof(FleetFreighterCommand)] = TranslateFleetFreighter,
                [typeof(FleetHaulContractCommand)] = TranslateFleetHaulContract,
                [typeof(MoveGoodsCommand)] = TranslateMoveGoods,
                [typeof(BootstrapColonyCommand)] = TranslateBootstrapColony,
                [typeof(FleetBootstrapCommand)] = TranslateFleetBootstrap,
                [typeof(QueueMoveGoodsCommand)] = TranslateQueueMoveGoods,
                [typeof(LoadGoodsCommand)] = TranslateLoadGoods,
                [typeof(UnloadGoodsCommand)] = TranslateUnloadGoods,
                [typeof(FleetMoveGoodsCommand)] = TranslateFleetMoveGoods,
                [typeof(SetMarketListingCommand)] = TranslateSetMarketListing,
                [typeof(ClearMarketListingCommand)] = TranslateClearMarketListing,
                [typeof(SetIntelListingCommand)] = TranslateSetIntelListing,
                [typeof(ClearIntelListingCommand)] = TranslateClearIntelListing,
                [typeof(BuyIntelCommand)] = TranslateBuyIntel,
                [typeof(CreateFleetCommand)] = TranslateCreateFleet,
                [typeof(CreateColonyCommand)] = TranslateCreateColony,
                [typeof(DisbandFleetCommand)] = TranslateDisbandFleet,
                [typeof(ChangeFleetParentCommand)] = TranslateChangeFleetParent,
                [typeof(ReassignShipCommand)] = TranslateReassignShip,
                [typeof(DetachShipCommand)] = TranslateDetachShip,
                [typeof(SetFlagshipCommand)] = TranslateSetFlagship,
                [typeof(AssignCaptainCommand)] = TranslateAssignCaptain,
                [typeof(AssignAdministratorCommand)] = TranslateAssignAdministrator,
                [typeof(TrainAcademyClassCommand)] = TranslateTrainAcademyClass,
                [typeof(AcademyCommandCourseCommand)] = TranslateAcademyCommandCourse,
                [typeof(CancelAcademyCourseCommand)] = TranslateCancelAcademyCourse,
                [typeof(HaulPairCommand)] = TranslateHaulPair,
                [typeof(TradePairCommand)] = TranslateTradePair,
                [typeof(SetStandingOrdersCommand)] = TranslateSetStandingOrders,
                [typeof(MoveToBodyCommand)] = TranslateMoveToBody,
                [typeof(Pulsar4X.Api.GeoSurveyCommand)] = TranslateGeoSurvey,
                [typeof(MineAsteroidsCommand)] = TranslateMineAsteroids,
                [typeof(GravSurveyCommand)] = TranslateGravSurvey,
                [typeof(Pulsar4X.Api.JumpCommand)] = TranslateJump,
                [typeof(RefuelAtCommand)] = TranslateRefuelAt,
                [typeof(AssignScientistCommand)] = TranslateAssignScientist,
                [typeof(UnassignScientistCommand)] = TranslateUnassignScientist,
                [typeof(SetResearchFundingCommand)] = TranslateSetResearchFunding,
                [typeof(AddTechToQueueCommand)] = TranslateAddTechToQueue,
                [typeof(RemoveTechFromQueueCommand)] = TranslateRemoveTechFromQueue,
                [typeof(MoveTechInQueueCommand)] = TranslateMoveTechInQueue,
                [typeof(CreateComponentDesignCommand)] = TranslateCreateComponentDesign,
                [typeof(SaveShipDesignCommand)] = TranslateSaveShipDesign,
                [typeof(DeleteShipDesignCommand)] = TranslateDeleteShipDesign,
                [typeof(SetShipDesignObsoleteCommand)] = TranslateSetShipDesignObsolete,
                [typeof(TransferCargoCommand)] = TranslateTransferCargo,
                [typeof(SetOrderPauseCommand)] = TranslateSetOrderPause,
                [typeof(Pulsar4X.Api.CancelOrderCommand)] = TranslateCancelOrder,
                [typeof(CancelGoalCommand)] = TranslateCancelGoal,
                [typeof(Pulsar4X.Api.NewtonThrustCommand)] = TranslateNewtonThrust,
                [typeof(Pulsar4X.Api.WarpMoveCommand)] = TranslateWarpMove,
                [typeof(GoToBodyCommand)] = TranslateGoToBody,
                [typeof(WarpToBodyCommand)] = TranslateWarpToBody,
                [typeof(CirculariseCommand)] = TranslateCircularise,
                [typeof(ChangeAltitudeCommand)] = TranslateChangeAltitude,
                [typeof(MatchOrbitCommand)] = TranslateMatchOrbit,
                [typeof(SetFireControlWeaponsCommand)] = TranslateSetFireControlWeapons,
                [typeof(SetFireControlTargetCommand)] = TranslateSetFireControlTarget,
                [typeof(AssignOrdnanceCommand)] = TranslateAssignOrdnance,
                [typeof(SetFireModeCommand)] = TranslateSetFireMode,
                [typeof(UninstallComponentCommand)] = TranslateUninstallComponent,
                [typeof(InstallComponentCommand)] = TranslateInstallComponent,
                [typeof(QueueIndustryJobCommand)] = TranslateQueueIndustryJob,
                [typeof(ChangeIndustryJobPriorityCommand)] = TranslateChangeIndustryJobPriority,
                [typeof(CancelIndustryJobCommand)] = TranslateCancelIndustryJob,
                [typeof(AddToConstructionQueueCommand)] = TranslateAddToConstructionQueue,
                [typeof(QueueShipRefitCommand)] = TranslateQueueShipRefit,
                [typeof(MoveConstructionJobCommand)] = TranslateMoveConstructionJob,
                [typeof(RemoveConstructionJobCommand)] = TranslateRemoveConstructionJob,
            };
        }

        /// <summary>
        /// Translates and dispatches a command that has already been authorized: <paramref name="faction"/>
        /// is the requesting faction entity and <paramref name="commanded"/> the resolved, owned target.
        /// </summary>
        public CommandResult Translate(Entity faction, Entity commanded, GameCommand command)
        {
            // A ship in a refit accepts no new orders. Move to Storage still uninstalls.
            if (command is not UninstallComponentCommand
                && commanded.HasDataBlob<ShipInfoDB>()
                && ShipRefitJob.TryFind(commanded, out _, out _))
                return CommandResult.Reject(ShipRefitJob.InRefit);

            if (!_translators.TryGetValue(command.GetType(), out var translate))
                return CommandResult.Reject($"Unsupported command: {command.GetType().Name}");

            return translate(faction, commanded, command);
        }

        // ----- helpers -----

        private CommandResult Dispatch(EntityAction order)
            => _game.OrderHandler.HandleOrder(order)
                ? CommandResult.Ok(Guid.NewGuid().ToString("N"))
                : CommandResult.Reject("Command rejected by engine validation.");

        // The cancel/pause/standing-order paths mutate the order or standing-order list directly
        // (they have no engine order of their own), so unlike HandleOrder they must signal the
        // change themselves — otherwise the fleet UI wouldn't refresh while paused.
        private static void PublishOrdersChanged(Entity holder)
            => MessagePublisher.Instance.Publish(Message.Create(
                MessageTypes.OrdersChanged,
                entityId: holder.Id,
                systemId: holder.Manager.ManagerID,
                factionId: holder.FactionOwnerID));

        // Captain changes are a direct write, same as standing orders: publish so the fleet tree
        // and the personnel roster refresh while the sim is paused.
        private static void PublishPersonnelChanged(Entity ship)
            => MessagePublisher.Instance.Publish(Message.Create(
                MessageTypes.PersonnelChanged,
                entityId: ship.Id,
                systemId: ship.Manager.ManagerID,
                factionId: ship.FactionOwnerID));

        private bool TryResolve(int entityId, out Entity entity)
            => _game.GlobalManager.TryGetGlobalEntityById(entityId, out entity);

        /// <summary>Finds the node in the faction's command tree whose direct children include
        /// <paramref name="ship"/>. That node is the faction itself when the ship sits on the
        /// faction root, and null when the ship is not in the tree.</summary>
        private static Entity? FindHoldingFleet(Entity fleet, Entity ship)
        {
            if (!fleet.TryGetDataBlob<FleetDB>(out var fleetDB)) return null;

            foreach (var child in fleetDB.GetChildren())
            {
                if (child == ship) return fleet;
                if (FindHoldingFleet(child, ship) is { } holder) return holder;
            }
            return null;
        }

        // ----- translators -----

        // Fully qualified engine order type: it shares its name with the API DTO.
        private CommandResult TranslateSetFactionStance(Entity faction, Entity commanded, GameCommand command)
        {
            var set = (SetFactionStanceCommand)command;
            if (commanded.Id != faction.Id)
                return CommandResult.Reject("Stance is set on the faction itself.");
            if (set.OtherFactionId == faction.Id)
                return CommandResult.Reject("A faction cannot set a stance toward itself.");
            if (set.OtherFactionId == Game.NeutralFactionId)
                return CommandResult.Reject("The neutral faction cannot be a stance target.");
            if (!_game.Factions.ContainsKey(set.OtherFactionId))
                return CommandResult.Reject($"Faction {set.OtherFactionId} not found.");
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var info))
                return CommandResult.Reject("Faction has no info.");

            info.Stances[set.OtherFactionId] = set.Stance;
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private CommandResult TranslateSetFactionGrant(Entity faction, Entity commanded, GameCommand command)
        {
            var set = (SetFactionGrantCommand)command;
            if (commanded.Id != faction.Id)
                return CommandResult.Reject("A grant is set on the faction itself.");
            if (set.OtherFactionId == faction.Id)
                return CommandResult.Reject("A faction cannot grant itself.");
            if (set.OtherFactionId == Game.NeutralFactionId)
                return CommandResult.Reject("The neutral faction cannot be a grant target.");
            if (!_game.Factions.ContainsKey(set.OtherFactionId))
                return CommandResult.Reject($"Faction {set.OtherFactionId} not found.");
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var info))
                return CommandResult.Reject("Faction has no info.");
            if (!FactionStanceRules.SetGrant(info, set.OtherFactionId, set.Grant, set.Enabled))
                return CommandResult.Reject("A grant command changes one flag.");

            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private static CommandResult? RejectWithoutLogisticsOffice(Entity commanded)
        {
            if (!commanded.HasDataBlob<LogiBaseDB>())
                return CommandResult.Reject("The colony has no logistics office.");
            return null;
        }

        private CommandResult TranslateSetMarketPolicy(Entity faction, Entity commanded, GameCommand command)
        {
            var set = (SetMarketPolicyCommand)command;
            if (RejectWithoutLogisticsOffice(commanded) is { } missing)
                return missing;
            if (string.IsNullOrEmpty(set.CargoId))
                return CommandResult.Reject("Cargo id is required.");
            if (set.Min < 0 || set.Max < 0 || set.Ask < 0 || set.Bid < 0)
                return CommandResult.Reject("Min, max, and prices cannot be negative.");
            if (set.Max < set.Min)
                return CommandResult.Reject("Max cannot be below min.");

            if (!commanded.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy))
            {
                policy = new ColonyMarketPolicyDB();
                commanded.SetDataBlob(policy);
            }

            policy.Rows[set.CargoId] = new MarketPolicyRow
            {
                CargoId = set.CargoId,
                Min = set.Min,
                Max = set.Max,
                AutoProduce = set.AutoProduce,
                Ask = set.Ask,
                Bid = set.Bid,
            };
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private CommandResult TranslateClearMarketPolicy(Entity faction, Entity commanded, GameCommand command)
        {
            var clear = (ClearMarketPolicyCommand)command;
            if (RejectWithoutLogisticsOffice(commanded) is { } missing)
                return missing;
            if (string.IsNullOrEmpty(clear.CargoId))
                return CommandResult.Reject("Cargo id is required.");

            if (commanded.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy))
                policy.Rows.Remove(clear.CargoId);
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private CommandResult TranslateRunMarket(Entity faction, Entity commanded, GameCommand command)
        {
            if (RejectWithoutLogisticsOffice(commanded) is { } missing)
                return missing;

            AgentProcessor.AssignGoal(commanded, new Goal(GoalType.RunMarket));
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private CommandResult TranslateSupplyLocal(Entity faction, Entity commanded, GameCommand command)
        {
            var supply = (SupplyLocalCommand)command;
            if (!commanded.HasDataBlob<ColonyInfoDB>())
                return CommandResult.Reject("The target is not a colony.");
            if (commanded.FactionOwnerID != faction.Id)
                return CommandResult.Reject("The colony is not yours.");
            if (RejectWithoutLogisticsOffice(commanded) is { } missing)
                return missing;

            var goal = new Goal(GoalType.SupplyLocal)
            {
                SupplyMode = supply.Mode,
                Name = supply.Mode switch
                {
                    SupplyMode.Balance => "Balance",
                    SupplyMode.Stockpile => "Stockpile",
                    _ => "Run markets",
                },
            };
            AgentProcessor.AssignGoal(commanded, goal);
            return CommandResult.Ok(goal.Id);
        }

        private CommandResult TranslateTrade(Entity faction, Entity commanded, GameCommand command)
        {
            if (RejectGoalWithoutCaptain(commanded) is { } rejected)
                return rejected;
            if (!commanded.HasDataBlob<CargoStorageDB>())
                return CommandResult.Reject("The ship has no cargo storage.");

            var goal = new Goal(GoalType.Trade);
            AgentProcessor.AssignGoal(commanded, goal);
            if (goal.Status == GoalStatus.Failed)
                return CommandResult.Reject(goal.Message);
            return CommandResult.Ok(goal.Id);
        }

        private CommandResult TranslateFreighter(Entity faction, Entity commanded, GameCommand command)
        {
            if (RejectGoalWithoutCaptain(commanded) is { } rejected)
                return rejected;
            if (!commanded.HasDataBlob<CargoStorageDB>())
                return CommandResult.Reject("The ship has no cargo storage.");

            var goal = new Goal(GoalType.Freighter);
            AgentProcessor.AssignGoal(commanded, goal);
            if (goal.Status == GoalStatus.Failed)
                return CommandResult.Reject(goal.Message);
            return CommandResult.Ok(goal.Id);
        }

        private CommandResult TranslateFleetTrade(Entity faction, Entity commanded, GameCommand command)
        {
            var trade = (FleetTradeCommand)command;
            if (RejectUnlessFleet(commanded) is { } notFleet)
                return notFleet;
            if (!InLogisticsReach(commanded, trade.BodyId))
                return CommandResult.Reject("The target is outside this fleet's reach.");

            var goal = new Goal(GoalType.FleetTrade) { TargetEntityID = trade.BodyId };
            AgentProcessor.AssignGoal(commanded, goal);
            if (goal.Status == GoalStatus.Failed)
                return CommandResult.Reject(goal.Message);
            return CommandResult.Ok(goal.Id);
        }

        private CommandResult TranslateFleetFreighter(Entity faction, Entity commanded, GameCommand command)
        {
            var haul = (FleetFreighterCommand)command;
            if (RejectUnlessFleet(commanded) is { } notFleet)
                return notFleet;
            if (!InLogisticsReach(commanded, haul.BodyId))
                return CommandResult.Reject("The target is outside this fleet's reach.");

            var goal = new Goal(GoalType.FleetFreighter) { TargetEntityID = haul.BodyId };
            AgentProcessor.AssignGoal(commanded, goal);
            if (goal.Status == GoalStatus.Failed)
                return CommandResult.Reject(goal.Message);
            return CommandResult.Ok(goal.Id);
        }

        private CommandResult TranslateFleetHaulContract(Entity faction, Entity commanded, GameCommand command)
        {
            var haul = (FleetHaulContractCommand)command;
            if (RejectUnlessFleet(commanded) is { } notFleet)
                return notFleet;
            if (string.IsNullOrEmpty(haul.CargoId))
                return CommandResult.Reject("Cargo id is required.");
            if (!TryResolve(haul.SourceId, out var source) || !TryResolve(haul.DestId, out var dest)
                || !LogisticsSpan.ContractReaches(commanded, source, dest))
                return CommandResult.Reject("The target is outside this fleet's reach.");
            if (source.Id == dest.Id
                || !source.HasDataBlob<ColonyInfoDB>()
                || !dest.HasDataBlob<ColonyInfoDB>())
                return CommandResult.Reject("The contract needs two colonies.");
            if (source.FactionOwnerID != commanded.FactionOwnerID || dest.FactionOwnerID != commanded.FactionOwnerID)
                return CommandResult.Reject("The colony is not yours.");

            int anchor = dest.Id;
            if (dest.TryGetDataBlob<ColonyInfoDB>(out var colony)
                && colony.PlanetEntity != null
                && colony.PlanetEntity.IsValid)
                anchor = colony.PlanetEntity.Id;

            var goal = new Goal(GoalType.FleetFreighter)
            {
                CargoId = haul.CargoId,
                SourceEntityId = source.Id,
                DestEntityId = dest.Id,
                TargetEntityID = anchor,
            };
            AgentProcessor.AssignGoal(commanded, goal);
            if (goal.Status == GoalStatus.Failed)
                return CommandResult.Reject(goal.Message);
            return CommandResult.Ok(goal.Id);
        }

        private CommandResult TranslateMoveGoods(Entity faction, Entity commanded, GameCommand command)
        {
            if (RejectGoalWithoutCaptain(commanded) is { } rejected)
                return rejected;
            var cmd = (MoveGoodsCommand)command;
            if (RejectOwnHaul(faction, commanded, cmd.SourceId, cmd.DestId, cmd.Items, out var lines) is { } bad)
                return bad;
            if (!PairReaches(commanded, cmd.SourceId, cmd.DestId))
                return CommandResult.Reject("The target is outside this ship's reach.");
            var goal = new Goal(GoalType.MoveGoods)
            {
                Name = "Move goods",
                SourceEntityId = cmd.SourceId,
                DestEntityId = cmd.DestId,
                MoveGoodsLines = lines,
            };
            AgentProcessor.AssignGoal(commanded, goal);
            if (goal.Status == GoalStatus.Failed)
                return CommandResult.Reject(goal.Message);
            return CommandResult.Ok(goal.Id);
        }

        private CommandResult TranslateBootstrapColony(Entity faction, Entity commanded, GameCommand command)
        {
            if (RejectGoalWithoutCaptain(commanded) is { } rejected)
                return rejected;
            var cmd = (BootstrapColonyCommand)command;
            if (commanded.HasDataBlob<FleetDB>() || !commanded.HasDataBlob<ShipInfoDB>())
                return CommandResult.Reject("Bootstrap colony is one ship.");
            if (!commanded.TryGetDataBlob<LocalConstructionDB>(out var bay) || bay.PointsPerDay <= 0)
                return CommandResult.Reject("The ship has no Construction Bay.");
            if (!commanded.HasDataBlob<CargoStorageDB>())
                return CommandResult.Reject("The ship has no cargo storage.");
            if (!TryResolve(cmd.SourceId, out var source) || !TryResolve(cmd.DestId, out var dest))
                return CommandResult.Reject("the colony is gone.");
            if (RejectWarehouse(faction, source) is { } sourceBad)
                return sourceBad;
            if (RejectWarehouse(faction, dest) is { } destBad)
                return destBad;
            if (source.Id == dest.Id)
                return CommandResult.Reject("Pick two different colonies.");
            if (cmd.Crates == null || cmd.Crates.Count == 0)
                return CommandResult.Reject("Nothing to build.");

            if (RejectCrates(faction, cmd.Crates, out var crates) is { } crateBad)
                return crateBad;

            if (!PairReaches(commanded, cmd.SourceId, cmd.DestId))
                return CommandResult.Reject("The target is outside this ship's reach.");
            var goal = new Goal(GoalType.BootstrapColony)
            {
                Name = "Bootstrap colony",
                SourceEntityId = cmd.SourceId,
                DestEntityId = cmd.DestId,
                TargetEntityID = cmd.DestId,
                BootstrapCrates = crates,
            };
            AgentProcessor.AssignGoal(commanded, goal);
            if (goal.Status == GoalStatus.Failed)
                return CommandResult.Reject(goal.Message);
            return CommandResult.Ok(goal.Id);
        }

        /// <summary>Every requested design is a crate-able facility of this faction, with a positive count.</summary>
        static CommandResult? RejectCrates(Entity faction, IReadOnlyList<BootstrapCrateRequest> requests, out List<BootstrapCrateLine> crates)
        {
            crates = new List<BootstrapCrateLine>();
            var info = faction.GetDataBlob<FactionInfoDB>();
            foreach (var request in requests)
            {
                if (request.Count <= 0)
                    return CommandResult.Reject("Crate counts must be positive.");
                if (!info.ComponentDesigns.TryGetValue(request.DesignId, out var design))
                    return CommandResult.Reject($"Unknown design {request.DesignId}.");
                if (!LocalConstructionJob.IsCrate(design))
                    return CommandResult.Reject($"{design.Name} is not a crate-able facility.");
                crates.Add(new BootstrapCrateLine { DesignId = request.DesignId, Count = request.Count });
            }
            return null;
        }

        private CommandResult TranslateFleetBootstrap(Entity faction, Entity commanded, GameCommand command)
        {
            if (RejectUnlessFleet(commanded) is { } notFleet)
                return notFleet;
            var cmd = (FleetBootstrapCommand)command;
            if (!TryResolve(cmd.SourceId, out var source) || !TryResolve(cmd.DestId, out var dest))
                return CommandResult.Reject("the colony is gone.");
            if (RejectWarehouse(faction, source) is { } sourceBad)
                return sourceBad;
            if (RejectWarehouse(faction, dest) is { } destBad)
                return destBad;
            if (source.Id == dest.Id)
                return CommandResult.Reject("Pick two different colonies.");
            if (cmd.Crates == null || cmd.Crates.Count == 0)
                return CommandResult.Reject("Nothing to build.");
            if (RejectCrates(faction, cmd.Crates, out var listed) is { } crateBad)
                return crateBad;

            // The list order is the priority. A design named twice keeps its first place and the counts add up.
            var crates = new List<BootstrapCrateLine>();
            foreach (var line in listed)
            {
                var same = crates.Find(c => c.DesignId == line.DesignId);
                if (same != null)
                    same.Count += line.Count;
                else
                    crates.Add(line);
            }

            if (!PairReaches(commanded, cmd.SourceId, cmd.DestId))
                return CommandResult.Reject("The target is outside this fleet's reach.");
            var goal = new Goal(GoalType.FleetBootstrap)
            {
                Name = "Bootstrap",
                SourceEntityId = cmd.SourceId,
                DestEntityId = cmd.DestId,
                TargetEntityID = cmd.DestId,
                BootstrapCrates = crates,
            };
            AgentProcessor.AssignGoal(commanded, goal);
            if (goal.Status == GoalStatus.Failed)
                return CommandResult.Reject(goal.Message);
            return CommandResult.Ok(goal.Id);
        }

        private CommandResult TranslateQueueMoveGoods(Entity faction, Entity commanded, GameCommand command)
        {
            if (RejectPlotOnCaptainOrFleet(commanded) is { } rejected)
                return rejected;
            var cmd = (QueueMoveGoodsCommand)command;
            if (RejectOwnHaul(faction, commanded, cmd.SourceId, cmd.DestId, cmd.Items, out var lines) is { } bad)
                return bad;

            if (!TryResolve(cmd.SourceId, out var source))
                return CommandResult.Reject("the colony is gone.");

            var actions = new List<EntityAction>();
            if (!MarketExchangeAction.InRange(commanded, source))
            {
                if (!MoveGoodsPlan.TryMoveIntoRange(commanded, source, out var moves, out var reason))
                    return CommandResult.Reject(string.IsNullOrEmpty(reason) ? "out of range" : reason);
                actions.AddRange(moves);
            }
            actions.Add(LoadGoodsAction.Create(commanded, cmd.SourceId, cmd.DestId, lines, plotDestination: true));
            return DispatchActions(actions, "Nothing to move.");
        }

        private CommandResult TranslateLoadGoods(Entity faction, Entity commanded, GameCommand command)
        {
            if (RejectPlotOnCaptainOrFleet(commanded) is { } rejected)
                return rejected;
            var cmd = (LoadGoodsCommand)command;
            if (!commanded.HasDataBlob<CargoStorageDB>())
                return CommandResult.Reject("The ship has no cargo storage.");
            if (!TryResolve(cmd.ColonyId, out var colony))
                return CommandResult.Reject("the colony is gone.");
            if (RejectWarehouse(faction, colony) is { } bad)
                return bad;
            if (!MarketExchangeAction.InRange(commanded, colony))
                return CommandResult.Reject("out of range");
            if (RejectLines(colony, cmd.Items, aboard: false, out var lines) is { } linesBad)
                return linesBad;
            return Dispatch(LoadGoodsAction.Create(commanded, colony.Id, 0, lines, plotDestination: false));
        }

        private CommandResult TranslateUnloadGoods(Entity faction, Entity commanded, GameCommand command)
        {
            if (RejectPlotOnCaptainOrFleet(commanded) is { } rejected)
                return rejected;
            var cmd = (UnloadGoodsCommand)command;
            if (!commanded.HasDataBlob<CargoStorageDB>())
                return CommandResult.Reject("The ship has no cargo storage.");
            if (!TryResolve(cmd.ColonyId, out var colony))
                return CommandResult.Reject("the colony is gone.");
            if (RejectWarehouse(faction, colony) is { } bad)
                return bad;
            if (!MarketExchangeAction.InRange(commanded, colony))
                return CommandResult.Reject("out of range");
            if (RejectLines(commanded, cmd.Items, aboard: true, out var lines) is { } linesBad)
                return linesBad;
            return Dispatch(UnloadGoodsAction.Create(commanded, colony.Id, lines));
        }

        private CommandResult TranslateFleetMoveGoods(Entity faction, Entity commanded, GameCommand command)
        {
            if (RejectUnlessFleet(commanded) is { } notFleet)
                return notFleet;
            var cmd = (FleetMoveGoodsCommand)command;
            if (RejectOwnHaul(faction, commanded, cmd.SourceId, cmd.DestId, cmd.Items, out var lines) is { } bad)
                return bad;
            if (!PairReaches(commanded, cmd.SourceId, cmd.DestId))
                return CommandResult.Reject("The target is outside this fleet's reach.");
            var goal = new Goal(GoalType.FleetMoveGoods)
            {
                Name = "Move goods",
                SourceEntityId = cmd.SourceId,
                DestEntityId = cmd.DestId,
                MoveGoodsLines = lines,
            };
            AgentProcessor.AssignGoal(commanded, goal);
            if (goal.Status == GoalStatus.Failed)
                return CommandResult.Reject(goal.Message);
            return CommandResult.Ok(goal.Id);
        }

        /// <summary>
        /// Both ends are owned colonies with a warehouse. Lines are read from the source warehouse.
        /// A fleet has no hold of its own, so the cargo-storage check is the ship's.
        /// </summary>
        private CommandResult? RejectOwnHaul(
            Entity faction, Entity commanded, int sourceId, int destId,
            IReadOnlyList<CargoTransferItem> items, out List<MoveGoodsLine> lines)
        {
            lines = new List<MoveGoodsLine>();
            if (!commanded.HasDataBlob<FleetDB>() && !commanded.HasDataBlob<CargoStorageDB>())
                return CommandResult.Reject("The ship has no cargo storage.");
            if (!TryResolve(sourceId, out var source) || !TryResolve(destId, out var dest))
                return CommandResult.Reject("the colony is gone.");
            if (RejectWarehouse(faction, source) is { } sourceBad)
                return sourceBad;
            if (RejectWarehouse(faction, dest) is { } destBad)
                return destBad;
            if (source.Id == dest.Id)
                return CommandResult.Reject("Pick two different colonies.");
            if (RejectLines(source, items, aboard: false, out lines) is { } linesBad)
                return linesBad;
            return null;
        }

        static CommandResult? RejectWarehouse(Entity faction, Entity colony)
        {
            if (!colony.HasDataBlob<ColonyInfoDB>() || !colony.HasDataBlob<CargoStorageDB>())
                return CommandResult.Reject("The colony has no warehouse.");
            if (colony.FactionOwnerID != faction.Id)
                return CommandResult.Reject("The colony is not yours.");
            return null;
        }

        static CommandResult? RejectLines(
            Entity storeEntity, IReadOnlyList<CargoTransferItem> items, bool aboard, out List<MoveGoodsLine> lines)
        {
            lines = new List<MoveGoodsLine>();
            if (items == null || items.Count == 0)
                return CommandResult.Reject("Nothing to move.");
            if (!storeEntity.HasDataBlob<CargoStorageDB>())
                return CommandResult.Reject(aboard ? "The ship has no cargo storage." : "The colony has no warehouse.");
            foreach (var item in items)
            {
                if (item.Units <= 0)
                    return CommandResult.Reject("Transfer amounts must be positive.");
                if (!MoveGoodsPlan.TryCargo(storeEntity, item.CargoItemId, out _))
                {
                    return CommandResult.Reject(aboard
                        ? $"Cargo item {item.CargoItemId} is not aboard."
                        : $"Cargo item {item.CargoItemId} is not in the source's storage.");
                }
                lines.Add(new MoveGoodsLine { ItemId = item.CargoItemId, Requested = item.Units });
            }
            return null;
        }

        private static CommandResult RejectUnlessFleet(Entity commanded)
            => commanded.HasDataBlob<FleetDB>()
                ? null
                : CommandResult.Reject("The commanded entity is not a fleet.");

        private bool InLogisticsReach(Entity fleet, int entityId)
        {
            if (!TryResolve(entityId, out var entity) || entity.Manager == null)
                return false;
            var span = LogisticsSpan.Of(fleet);
            if (span == CommandSpanKind.Neighbor)
                return LogisticsSpan.ReachesManager(fleet, entity.Manager);
            if (entity.Manager != fleet.Manager)
                return false;
            if (span == CommandSpanKind.System)
                return true;
            return entity.TryGetDataBlob<PositionDB>(out var pos) && pos.Parent != null;
        }

        private bool PairReaches(Entity commanded, int sourceId, int destId)
            => TryResolve(sourceId, out var source)
                && TryResolve(destId, out var dest)
                && LogisticsSpan.ContractReaches(commanded, source, dest);

        private CommandResult TranslateSetMarketListing(Entity faction, Entity commanded, GameCommand command)
        {
            var set = (SetMarketListingCommand)command;
            if (RejectWithoutLogisticsOffice(commanded) is { } missing)
                return missing;
            if (string.IsNullOrEmpty(set.CargoId))
                return CommandResult.Reject("Cargo id is required.");
            if (set.SellQuantity < 0 || set.BuyQuantity < 0 || set.Reserve < 0 || set.Ask < 0 || set.Bid < 0)
                return CommandResult.Reject("Prices and quantities cannot be negative.");
            if (!OwnerCargo(commanded, set.CargoId))
                return CommandResult.Reject("Unknown cargo id.");

            var posted = MarketBook.SetListing(commanded, new MarketListing
            {
                CargoId = set.CargoId,
                SellQuantity = set.SellQuantity,
                Ask = set.Ask,
                BuyQuantity = set.BuyQuantity,
                Bid = set.Bid,
                Reserve = set.Reserve,
            });
            if (!posted)
                return CommandResult.Reject("The logistics office is at capacity.");

            UpsertListingPolicy(commanded, set.CargoId, set.Reserve, set.Ask, set.Bid);
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private CommandResult TranslateClearMarketListing(Entity faction, Entity commanded, GameCommand command)
        {
            var clear = (ClearMarketListingCommand)command;
            if (RejectWithoutLogisticsOffice(commanded) is { } missing)
                return missing;
            if (string.IsNullOrEmpty(clear.CargoId))
                return CommandResult.Reject("Cargo id is required.");

            MarketBook.RemoveListing(commanded, clear.CargoId);
            if (commanded.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy))
                policy.Rows.Remove(clear.CargoId);
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private CommandResult TranslateSetIntelListing(Entity faction, Entity commanded, GameCommand command)
        {
            var set = (SetIntelListingCommand)command;
            if (RejectWithoutLogisticsOffice(commanded) is { } missing)
                return missing;
            if (!IntelBook.TrySet(commanded, set.Kind, set.Subject, set.Ask, set.ForSale, out var reason))
                return CommandResult.Reject(reason);
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private CommandResult TranslateClearIntelListing(Entity faction, Entity commanded, GameCommand command)
        {
            var clear = (ClearIntelListingCommand)command;
            if (RejectWithoutLogisticsOffice(commanded) is { } missing)
                return missing;
            if (string.IsNullOrEmpty(clear.Subject))
                return CommandResult.Reject("Subject is required.");
            IntelBook.Remove(commanded, clear.Kind, clear.Subject);
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private CommandResult TranslateBuyIntel(Entity faction, Entity commanded, GameCommand command)
        {
            var buy = (BuyIntelCommand)command;
            if (RejectWithoutLogisticsOffice(commanded) is { } missing)
                return missing;
            if (!IntelBook.TryBuy(commanded, faction, buy.Kind, buy.Subject, out var reason))
                return CommandResult.Reject(reason);
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private static bool OwnerCargo(Entity colony, string cargoId)
        {
            var game = colony.Manager?.Game;
            if (game == null || !game.Factions.TryGetValue(colony.FactionOwnerID, out var owner))
                return false;
            if (!owner.TryGetDataBlob<FactionInfoDB>(out var info))
                return false;
            return info.Data.CargoGoods.Contains(cargoId);
        }

        private static void UpsertListingPolicy(Entity colony, string cargoId, long reserve, decimal ask, decimal bid)
        {
            if (!colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy))
            {
                policy = new ColonyMarketPolicyDB();
                colony.SetDataBlob(policy);
            }

            if (policy.Rows.TryGetValue(cargoId, out var row))
            {
                row.CargoId = cargoId;
                row.Min = reserve;
                row.Ask = ask;
                row.Bid = bid;
                if (row.Max < reserve)
                    row.Max = reserve;
                return;
            }

            policy.Rows[cargoId] = new MarketPolicyRow
            {
                CargoId = cargoId,
                Min = reserve,
                Max = reserve,
                AutoProduce = false,
                Ask = ask,
                Bid = bid,
            };
        }

        private CommandResult TranslateRename(Entity faction, Entity commanded, GameCommand command)
        {
            var rename = (Pulsar4X.Api.RenameCommand)command;
            bool accepted = Pulsar4X.Names.RenameAction.CreateRenameCommand(_game, faction, commanded, rename.NewName);
            return accepted
                ? CommandResult.Ok(Guid.NewGuid().ToString("N"))
                : CommandResult.Reject("Command rejected by engine validation.");
        }

        private CommandResult TranslateCreateFleet(Entity faction, Entity commanded, GameCommand command)
        {
            var create = (CreateFleetCommand)command;
            if (commanded != faction)
                return CommandResult.Reject("A fleet can only be created by commanding the faction itself.");

            var system = _game.Systems.FirstOrDefault(s => s.ID == create.SystemId);
            if (system == null)
                return CommandResult.Reject($"System {create.SystemId} not found.");

            // The fleet's name is generated server-side; the client renames it afterwards if desired.
            return Dispatch(FleetOrder.CreateFleetOrder(NameFactory.GetFleetName(_game), faction, system));
        }

        private CommandResult TranslateCreateColony(Entity faction, Entity commanded, GameCommand command)
        {
            var create = (CreateColonyCommand)command;
            if (commanded != faction)
                return CommandResult.Reject("A colony can only be created by commanding the faction itself.");

            if (!TryResolve(create.BodyId, out var body))
                return CommandResult.Reject($"Entity {create.BodyId} not found.");

            if (!faction.TryGetDataBlob<FactionInfoDB>(out var factionInfo) || factionInfo.Species.Count == 0)
                return CommandResult.Reject("The faction has no species to settle the colony with.");

            return Dispatch(Pulsar4X.Colonies.CreateColonyOrder.CreateCommand(faction, factionInfo.Species[0], body));
        }

        private CommandResult TranslateDisbandFleet(Entity faction, Entity commanded, GameCommand command)
            => Dispatch(FleetOrder.DisbandFleet(faction.Id, commanded));

        private CommandResult TranslateChangeFleetParent(Entity faction, Entity commanded, GameCommand command)
        {
            var change = (ChangeFleetParentCommand)command;
            if (!TryResolve(change.NewParentId, out var newParent))
                return CommandResult.Reject($"Entity {change.NewParentId} not found.");
            // The engine only validates the source fleet's owner, so check the new parent here.
            if (newParent.FactionOwnerID != faction.Id || !newParent.HasDataBlob<FleetDB>())
                return CommandResult.Reject("The new parent is not one of your fleets.");

            if (FleetOrder.WouldCycle(commanded, newParent))
                return CommandResult.Reject("A fleet cannot be nested under itself or one of its own sub-fleets.");

            return Dispatch(FleetOrder.ChangeParent(faction.Id, commanded, newParent));
        }

        private CommandResult TranslateReassignShip(Entity faction, Entity commanded, GameCommand command)
        {
            var reassign = (ReassignShipCommand)command;
            if (!TryResolve(reassign.ToFleetId, out var toFleet))
                return CommandResult.Reject($"Entity {reassign.ToFleetId} not found.");
            if (!toFleet.HasDataBlob<FleetDB>())
                return CommandResult.Reject("Reassignment target is not a fleet.");
            if (toFleet.FactionOwnerID != faction.Id)
                return CommandResult.Reject("Reassignment target is not one of your fleets.");
            if (!faction.TryGetDataBlob<FleetDB>(out var root))
                return CommandResult.Reject("Faction has no fleet root.");

            // A ship in a fleet is removed by UnassignShip. That order rejects the faction
            // entity as the commander, so a ship sitting on the faction root is lifted off
            // directly. A ship that is in neither place is only added.
            var holder = FindHoldingFleet(faction, commanded);
            if (holder != null && holder.Id != faction.Id)
            {
                var unassign = FleetOrder.UnassignShip(faction.Id, holder, commanded);
                if (!_game.OrderHandler.HandleOrder(unassign))
                    return CommandResult.Reject("Command rejected by engine validation.");
            }
            else if (root.Children.Contains(commanded))
                root.RemoveChild(commanded);

            return Dispatch(FleetOrder.AssignShip(faction.Id, toFleet, commanded));
        }

        private CommandResult TranslateDetachShip(Entity faction, Entity commanded, GameCommand command)
        {
            if (!commanded.HasDataBlob<ShipInfoDB>())
                return CommandResult.Reject("Detach target is not a ship.");
            if (!faction.TryGetDataBlob<FleetDB>(out var root))
                return CommandResult.Reject("Faction has no fleet root.");

            var holder = FindHoldingFleet(faction, commanded);
            if (holder != null && holder.Id != faction.Id)
            {
                // Unassign removes the ship from that fleet. It does not hang the ship on the
                // faction root, which is what the unassigned list is.
                var unassign = FleetOrder.UnassignShip(faction.Id, holder, commanded);
                if (!_game.OrderHandler.HandleOrder(unassign))
                    return CommandResult.Reject("Command rejected by engine validation.");
            }

            if (!root.Children.Contains(commanded))
                root.AddChild(commanded);

            MessagePublisher.Instance.Publish(Message.Create(
                MessageTypes.FleetReorganized, factionId: faction.Id));
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private CommandResult TranslateSetFlagship(Entity faction, Entity commanded, GameCommand command)
        {
            var setFlagship = (SetFlagshipCommand)command;
            if (!TryResolve(setFlagship.ShipId, out var ship))
                return CommandResult.Reject($"Entity {setFlagship.ShipId} not found.");
            // The engine only validates the fleet's owner, so check the flagship ship here.
            if (ship.FactionOwnerID != faction.Id)
                return CommandResult.Reject("The flagship must be one of your own ships.");

            return Dispatch(FleetOrder.SetFlagShip(faction.Id, commanded, ship));
        }

        // The standing-orders editor runs client-side; the whole list replaces in one validated
        // write. Engine conditions/actions are rebuilt from their StandingOrderTypes ids — there is
        // no engine order for this (the pre-port UI mutated FleetDB.StandingOrders by reference).
        private CommandResult TranslateSetStandingOrders(Entity faction, Entity commanded, GameCommand command)
        {
            var set = (SetStandingOrdersCommand)command;
            if (!commanded.TryGetDataBlob<FleetDB>(out var fleetDB))
                return CommandResult.Reject("The commanded entity is not a fleet.");

            var rebuilt = new List<ConditionalOrder>();
            foreach (var order in set.Orders ?? Array.Empty<Pulsar4X.Api.StandingOrder>())
            {
                var compound = new CompoundCondition();
                var conditions = order.Conditions ?? Array.Empty<StandingOrderCondition>();
                for (int i = 0; i < conditions.Count; i++)
                {
                    var condition = conditions[i];
                    Interfaces.ICondition? engineCondition = condition.ConditionType switch
                    {
                        StandingOrderTypes.FuelCondition => new FuelCondition(
                            Math.Clamp(condition.Threshold, 0, 100), ToComparisonType(condition.Comparison)),
                        _ => null,
                    };
                    if (engineCondition == null)
                        return CommandResult.Reject($"Unknown standing-order condition: {condition.ConditionType}");

                    // Operators link a condition to the next one; the last carries none.
                    DataStructures.LogicalOperation? logic = i < conditions.Count - 1
                        ? condition.Logic == StandingOrderLogic.Or
                            ? DataStructures.LogicalOperation.Or
                            : DataStructures.LogicalOperation.And
                        : null;
                    compound.ConditionItems.Add(new ConditionItem(engineCondition, logic));
                }

                var actions = new DataStructures.SafeList<EntityAction>();
                foreach (var actionType in order.Actions ?? Array.Empty<string>())
                {
                    EntityAction? action = actionType switch
                    {
                        StandingOrderTypes.MoveToNearestColony => MoveToNearestColonyAction.CreateCommand(faction.Id, commanded),
                        StandingOrderTypes.MoveToNearestGeoSurvey => MoveToNearestGeoSurveyAction.CreateCommand(faction.Id, commanded),
                        StandingOrderTypes.MoveToNearestAnomaly => MoveToNearestAnomalyAction.CreateCommand(faction.Id, commanded),
                        StandingOrderTypes.Refuel => new RefuelAction(),
                        StandingOrderTypes.Resupply => new ResupplyAction(),
                        _ => null,
                    };
                    if (action == null)
                        return CommandResult.Reject($"Unknown standing-order action: {actionType}");
                    actions.Add(action);
                }

                rebuilt.Add(new ConditionalOrder(compound, actions) { Name = order.Name ?? "" });
            }

            // Replace wholesale only after everything validated. Safe to swap live: the standing-
            // orders processor clones actions out of this list rather than executing them in place.
            fleetDB.StandingOrders.Clear();
            foreach (var order in rebuilt)
                fleetDB.StandingOrders.Add(order);

            PublishOrdersChanged(commanded);
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private static DataStructures.ComparisonType ToComparisonType(StandingOrderComparison comparison)
            => comparison switch
            {
                StandingOrderComparison.LessThan => DataStructures.ComparisonType.LessThan,
                StandingOrderComparison.LessThanOrEqual => DataStructures.ComparisonType.LessThanOrEqual,
                StandingOrderComparison.EqualTo => DataStructures.ComparisonType.EqualTo,
                StandingOrderComparison.GreaterThan => DataStructures.ComparisonType.GreaterThan,
                _ => DataStructures.ComparisonType.GreaterThanOrEqual,
            };

        /// <summary>
        /// A ship with no captain has no one to turn a goal into a plot. The player queues actions.
        /// A fleet is still a valid goal target: the flagship's commander owns that order.
        /// </summary>
        private static CommandResult? RejectGoalWithoutCaptain(Entity commanded)
        {
            if (commanded.HasDataBlob<FleetDB>())
                return null;
            if (!commanded.TryGetDataBlob<ShipInfoDB>(out var info) || info.CommanderID >= 0)
                return null;
            return CommandResult.Reject("This ship has no captain. Plot an action instead.");
        }

        private CommandResult TranslateMoveToBody(Entity faction, Entity commanded, GameCommand command)
        {
            if (RejectGoalWithoutCaptain(commanded) is { } rejected)
                return rejected;
            var cmd = (MoveToBodyCommand)command;
            var goal = new Goal(GoalType.MoveTo)
            {
                TargetEntityID = cmd.BodyId,
            };
            AgentProcessor.AssignGoal(commanded, goal);
            if(goal.Status == GoalStatus.Failed)
                return CommandResult.Reject(goal.Message);
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private CommandResult TranslateGeoSurvey(Entity faction, Entity commanded, GameCommand command)
        {
            var cmd = (GeoSurveyCommand)command;
            if (!commanded.HasDataBlob<FleetDB>() && IsEmptyChairShip(commanded))
            {
                if (!TryResolve(cmd.BodyId, out var body))
                    return CommandResult.Reject($"Entity {cmd.BodyId} not found.");
                return PlotGeoSurvey(commanded, body);
            }

            if (RejectGoalWithoutCaptain(commanded) is { } rejected)
                return rejected;
            var goal = new Goal(GoalType.ServeyBodies)
            {
                TargetEntityID = cmd.BodyId,
            };
            AgentProcessor.AssignGoal(commanded, goal);
            if(goal.Status == GoalStatus.Failed)
                return CommandResult.Reject(goal.Message);
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private CommandResult TranslateMineAsteroids(Entity faction, Entity commanded, GameCommand command)
        {
            if (RejectGoalWithoutCaptain(commanded) is { } rejected)
                return rejected;
            if (!commanded.HasOrChildHasAbility<AsteroidMineAbilityDB>())
                return CommandResult.Reject("This ship has no asteroid miner.");

            var goal = new Goal(GoalType.MineAsteroids);
            AgentProcessor.AssignGoal(commanded, goal);
            if (goal.Status == GoalStatus.Failed)
                return CommandResult.Reject(goal.Message);
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private CommandResult TranslateGravSurvey(Entity faction, Entity commanded, GameCommand command)
        {
            var cmd = (GravSurveyCommand)command;
            if (!commanded.HasDataBlob<FleetDB>() && IsEmptyChairShip(commanded))
            {
                if (!TryResolve(cmd.LocationId, out var site))
                    return CommandResult.Reject($"Entity {cmd.LocationId} not found.");
                return PlotGravSurvey(commanded, site);
            }

            if (RejectGoalWithoutCaptain(commanded) is { } rejected)
                return rejected;
            var goal = new Goal(GoalType.ScanAnomalies)
            {
                TargetEntityID = cmd.LocationId,
            };
            AgentProcessor.AssignGoal(commanded, goal);
            if(goal.Status == GoalStatus.Failed)
                return CommandResult.Reject(goal.Message);
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private CommandResult TranslateJump(Entity faction, Entity commanded, GameCommand command)
        {
            var jump = (Pulsar4X.Api.JumpCommand)command;
            if (!TryResolve(jump.JumpPointId, out var jumpPoint)
                || !jumpPoint.TryGetDataBlob<JumpPointDB>(out var jumpPointDB))
                return CommandResult.Reject($"Jump point {jump.JumpPointId} not found.");

            // Visibility enforced at the boundary: a faction may only use jump points it has discovered.
            if (!jumpPointDB.IsDiscovered.Contains(faction.Id))
                return CommandResult.Reject("Jump point has not been discovered by the faction.");

            if (!commanded.HasDataBlob<FleetDB>() && IsEmptyChairShip(commanded))
                return PlotShipJump(commanded, jumpPoint, jumpPointDB);

            if (RejectGoalWithoutCaptain(commanded) is { } rejected)
                return rejected;
            return JumpOrder.CreateAndExecute(_game, faction, commanded, jumpPointDB)
                ? CommandResult.Ok(Guid.NewGuid().ToString("N"))
                : CommandResult.Reject("Command rejected by engine validation.");
        }

        private CommandResult TranslateRefuelAt(Entity faction, Entity commanded, GameCommand command)
        {
            var refuel = (RefuelAtCommand)command;
            if (!TryResolve(refuel.ColonyId, out var colony))
                return CommandResult.Reject($"Entity {refuel.ColonyId} not found.");
            if (!colony.HasDataBlob<CargoStorageDB>())
                return CommandResult.Reject("Refuel target has no cargo storage.");

            // A captained ship gets the goal. The captain plots the flight and the fill.
            // A fleet still warps and fills its ships directly.
            if (!commanded.HasDataBlob<FleetDB>())
            {
                if (RejectGoalWithoutCaptain(commanded) is { } rejected)
                    return rejected;
                if (!commanded.HasDataBlob<ShipInfoDB>())
                    return CommandResult.Reject("Refuel needs a ship or a fleet.");

                var goal = new Goal(GoalType.RefuelAt)
                {
                    Name = "Refuel",
                    TargetEntityID = colony.Id,
                };
                AgentProcessor.AssignGoal(commanded, goal);
                if (goal.Status == GoalStatus.Failed)
                    return CommandResult.Reject(string.IsNullOrEmpty(goal.Message) ? "Refuel failed." : goal.Message);
                return CommandResult.Ok(goal.Id);
            }

            if (!_game.OrderHandler.HandleOrder(WarpFleetTowardsTargetOrder.CreateCommand(commanded, colony)))
                return CommandResult.Reject("Command rejected by engine validation.");

            return CargoTransferOrder.CreateRefuelFleetCommand(colony, commanded)
                ? CommandResult.Ok(Guid.NewGuid().ToString("N"))
                : CommandResult.Reject("No ship in the fleet could take on fuel.");
        }

        // ----- cargo transfer (commanded entity: the source) -----

        private CommandResult TranslateTransferCargo(Entity faction, Entity commanded, GameCommand command)
        {
            var transfer = (TransferCargoCommand)command;
            if (!TryResolve(transfer.PartnerEntityId, out var partner))
                return CommandResult.Reject($"Entity {transfer.PartnerEntityId} not found.");
            if (partner.FactionOwnerID != faction.Id)
                return CommandResult.Reject("The transfer partner does not belong to the faction.");
            if (!commanded.TryGetDataBlob<CargoStorageDB>(out var storage))
                return CommandResult.Reject("The commanded entity has no cargo storage.");
            if (!partner.HasDataBlob<CargoStorageDB>())
                return CommandResult.Reject("The transfer partner has no cargo storage.");
            if (transfer.Items is not { Count: > 0 })
                return CommandResult.Reject("Nothing to transfer.");

            // Items travel as cargo-item ids; resolve each against the source's stores (this also
            // covers entity-specific cargoables like component instances, which aren't in the
            // faction's goods library).
            var items = new List<(ICargoable, long)>(transfer.Items.Count);
            foreach (var (cargoItemId, units) in transfer.Items)
            {
                if (units <= 0)
                    return CommandResult.Reject("Transfer amounts must be positive.");

                ICargoable? cargoable = null;
                foreach (var store in storage.TypeStores.Values)
                {
                    if (store.GetCargoables().TryGetValue(cargoItemId, out var found))
                    {
                        cargoable = found;
                        break;
                    }
                }
                if (cargoable == null)
                    return CommandResult.Reject($"Cargo item {cargoItemId} is not in the source's storage.");

                // Negative = out of the primary (commanded) entity, per the engine's convention.
                items.Add((cargoable, -units));
            }

            return CargoTransferOrder.CreateCommands(faction.Id, commanded, partner, items)
                ? CommandResult.Ok(Guid.NewGuid().ToString("N"))
                : CommandResult.Reject("Command rejected by engine validation.");
        }

        // ----- order queue (commanded entity: the holder) -----

        // PauseOnAction is a plain flag on the queued order with no engine order of its own (the
        // pre-port UI flipped it by reference), so the translator sets it directly.
        private CommandResult TranslateSetOrderPause(Entity faction, Entity commanded, GameCommand command)
        {
            var pause = (SetOrderPauseCommand)command;
            if (!commanded.TryGetDataBlob<ActionQueueDB>(out var orderable))
                return CommandResult.Reject("The entity has no order queue.");

            var order = orderable.ActionList.FirstOrDefault(o => o.CmdID == pause.OrderId);
            if (order == null)
                return CommandResult.Reject($"Order {pause.OrderId} is not in the queue.");

            order.PauseOnAction = pause.Pause;
            PublishOrdersChanged(commanded);
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        // Like SetOrderPause: removing a queued order has no engine order of its own (the pre-port
        // maneuver UI removed it from the queue by reference), so the translator does it directly.
        private CommandResult TranslateCancelOrder(Entity faction, Entity commanded, GameCommand command)
        {
            var cancel = (Pulsar4X.Api.CancelOrderCommand)command;
            if (!commanded.TryGetDataBlob<ActionQueueDB>(out var orderable))
                return CommandResult.Reject("The entity has no order queue.");

            var order = orderable.ActionList.FirstOrDefault(o => o.CmdID == cancel.OrderId);
            if (order == null)
                return CommandResult.Reject($"Order {cancel.OrderId} is not in the queue.");

            AgentProcessor.CancelAction(commanded, order, commanded.StarSysDateTime);
            PublishOrdersChanged(commanded);
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private CommandResult TranslateCancelGoal(Entity faction, Entity commanded, GameCommand command)
        {
            var cancel = (CancelGoalCommand)command;
            if (commanded.HasDataBlob<ColonyInfoDB>())
                return CommandResult.Reject("Cancel a ship or a fleet order.");
            if (!AgentProcessor.CancelGoal(commanded, cancel.Timing, commanded.StarSysDateTime))
                return CommandResult.Reject("Nothing to cancel.");

            PublishOrdersChanged(commanded);
            if (commanded.TryGetDataBlob<FleetDB>(out var fleet)
                && commanded.TryGetDataBlob<GoalsDB>(out var goals)
                && goals.GivenGoal != null)
            {
                foreach (var child in fleet.Children)
                {
                    if (child.TryGetDataBlob<GoalsDB>(out var childGoals)
                        && childGoals.ActiveGoal != null
                        && childGoals.ActiveGoal.ParentGoalId == goals.GivenGoal.Id
                        && childGoals.ActiveGoal.PlayerCancelled)
                        PublishOrdersChanged(child);
                }
            }
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        // ----- ship movement (commanded entity: the ship) -----

        /// <summary>Empty chair: the player plots. A seated captain can still be handed a plot.</summary>
        static bool IsEmptyChairShip(Entity commanded)
        {
            if (commanded.HasDataBlob<FleetDB>())
                return false;
            if (!commanded.TryGetDataBlob<ShipInfoDB>(out var info))
                return true;
            return info.CommanderID < 0;
        }

        /// <summary>
        /// A fleet has no hull to plot. A ship queues the action on its own list, captain or not.
        /// The agent only replans actions tagged with its goal, so a hand-queued plot stays.
        /// </summary>
        static CommandResult? RejectPlotOnCaptainOrFleet(Entity commanded)
        {
            if (commanded.HasDataBlob<FleetDB>())
                return CommandResult.Reject("Plot actions on a ship.");
            if (!commanded.HasDataBlob<ActionQueueDB>())
                return CommandResult.Reject("The entity has no order queue.");
            return null;
        }

        CommandResult? RejectUnseen(Entity faction, Entity destination)
        {
            if (destination.Manager == null
                || !destination.Manager.IsEntityVisibleToFaction(destination, faction.Id))
                return CommandResult.Reject($"Entity {destination.Id} not found.");
            return null;
        }

        CommandResult DispatchActions(List<EntityAction> actions, string emptyReason)
        {
            if (actions.Count == 0)
                return CommandResult.Reject(string.IsNullOrEmpty(emptyReason) ? "nothing to queue" : emptyReason);
            foreach (var action in actions)
            {
                if (!_game.OrderHandler.HandleOrder(action))
                    return CommandResult.Reject("Command rejected by engine validation.");
            }
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private CommandResult TranslateGoToBody(Entity faction, Entity commanded, GameCommand command)
        {
            if (RejectPlotOnCaptainOrFleet(commanded) is { } rejected)
                return rejected;
            var cmd = (GoToBodyCommand)command;
            if (!TryResolve(cmd.BodyId, out var body))
                return CommandResult.Reject($"Entity {cmd.BodyId} not found.");
            if (RejectUnseen(faction, body) is { } unseen)
                return unseen;
            if (!MovePlanner.TryBuildMoveActions(commanded, body, out var actions, out var reason, commanded.StarSysDateTime))
                return CommandResult.Reject(reason);
            return DispatchActions(actions, reason);
        }

        private CommandResult TranslateWarpToBody(Entity faction, Entity commanded, GameCommand command)
        {
            if (RejectPlotOnCaptainOrFleet(commanded) is { } rejected)
                return rejected;
            if (!commanded.HasDataBlob<WarpAbilityDB>())
                return CommandResult.Reject("The entity has no warp drive.");
            var cmd = (WarpToBodyCommand)command;
            if (!TryResolve(cmd.BodyId, out var body))
                return CommandResult.Reject($"Entity {cmd.BodyId} not found.");
            if (RejectUnseen(faction, body) is { } unseen)
                return unseen;
            try
            {
                var actions = MovePlanner.BuildWarpAndCircularise(commanded, body, commanded.StarSysDateTime);
                return DispatchActions(actions, "warp could not be plotted");
            }
            catch (Exception e)
            {
                return CommandResult.Reject($"Warp could not be plotted: {e.Message}");
            }
        }

        private CommandResult TranslateCircularise(Entity faction, Entity commanded, GameCommand command)
        {
            if (RejectPlotOnCaptainOrFleet(commanded) is { } rejected)
                return rejected;
            if (!CirculariseAction.TryCompute(commanded, commanded.StarSysDateTime, out _, out _, out _))
                return CommandResult.Reject("Cannot circularise here.");
            return Dispatch(CirculariseAction.CreateCommand(commanded));
        }

        private CommandResult TranslateChangeAltitude(Entity faction, Entity commanded, GameCommand command)
        {
            if (RejectPlotOnCaptainOrFleet(commanded) is { } rejected)
                return rejected;
            var cmd = (ChangeAltitudeCommand)command;
            if (!(cmd.RadiusMetres > 0) || !double.IsFinite(cmd.RadiusMetres))
                return CommandResult.Reject("Orbit radius must be a positive finite number.");
            if (!ChangeOrbitalAltitudeAction.TryPlanBurns(commanded, cmd.RadiusMetres, commanded.StarSysDateTime, out var burns, out var reason))
                return CommandResult.Reject(reason);
            if (burns.Count == 0)
                return CommandResult.Reject("Already at altitude");
            return Dispatch(ChangeOrbitalAltitudeAction.CreateCommand(commanded, cmd.RadiusMetres));
        }

        private CommandResult TranslateMatchOrbit(Entity faction, Entity commanded, GameCommand command)
        {
            if (RejectPlotOnCaptainOrFleet(commanded) is { } rejected)
                return rejected;
            var cmd = (MatchOrbitCommand)command;
            if (!TryResolve(cmd.BodyId, out var body))
                return CommandResult.Reject($"Entity {cmd.BodyId} not found.");
            if (RejectUnseen(faction, body) is { } unseen)
                return unseen;
            if (!MatchOrbitAction.TryPlanBurns(commanded, body, commanded.StarSysDateTime, out _, out var reason))
                return CommandResult.Reject(reason);
            return Dispatch(MatchOrbitAction.CreateCommand(commanded, body));
        }

        CommandResult PlotGeoSurvey(Entity ship, Entity body)
        {
            if (RejectPlotOnCaptainOrFleet(ship) is { } rejected)
                return rejected;
            if (!ship.HasDataBlob<GeoSurveyAbilityDB>())
                return CommandResult.Reject("The ship has no geological survey gear.");
            if (!body.TryGetDataBlob<GeoSurveyableDB>(out var surveyable)
                || surveyable.IsSurveyComplete(ship.FactionOwnerID))
                return CommandResult.Reject("Nothing to geo-survey here.");
            if (!IsAtGeoSite(ship, body))
                return CommandResult.Reject("The ship is not at this body.");
            return Dispatch(GeoSurveyOrder.CreateCommand(ship.FactionOwnerID, ship, body));
        }

        CommandResult PlotGravSurvey(Entity ship, Entity site)
        {
            if (RejectPlotOnCaptainOrFleet(ship) is { } rejected)
                return rejected;
            if (!ship.HasDataBlob<JPSurveyAbilityDB>())
                return CommandResult.Reject("The ship has no gravitational survey gear.");
            if (!site.TryGetDataBlob<JPSurveyableDB>(out var surveyable)
                || surveyable.IsSurveyComplete(ship.FactionOwnerID))
                return CommandResult.Reject("Nothing to grav-survey here.");
            if (!IsAtGravSite(ship, site))
                return CommandResult.Reject("The ship is not at this site.");
            return Dispatch(JPSurveyOrder.CreateCommand(ship.FactionOwnerID, ship, site));
        }

        static bool IsAtGeoSite(Entity ship, Entity body)
        {
            if (ship.TryGetDataBlob<PositionDB>(out var pos) && pos.Parent == body)
                return true;
            return ship.GetSOIParentEntity() == body;
        }

        static bool IsAtGravSite(Entity ship, Entity site)
        {
            if (ship.TryGetDataBlob<PositionDB>(out var pos) && pos.Parent == site)
                return true;
            return MoveMath.GetDistanceBetween(ship, site) < 100_000;
        }

        CommandResult PlotShipJump(Entity ship, Entity jumpPoint, JumpPointDB jumpPointDB)
        {
            if (RejectPlotOnCaptainOrFleet(ship) is { } rejected)
                return rejected;
            var actions = new List<EntityAction>();
            var gate = jumpPointDB.OwningEntity ?? jumpPoint;
            bool atGate = ship.TryGetDataBlob<PositionDB>(out var pos) && pos.Parent == gate;
            if (!atGate)
            {
                if (!ship.HasDataBlob<WarpAbilityDB>())
                    return CommandResult.Reject("The entity has no warp drive.");
                try
                {
                    actions.Add(WarpMoveAction.CreateCommandEZ(ship, gate, ship.StarSysDateTime));
                }
                catch (Exception e)
                {
                    return CommandResult.Reject($"Warp could not be plotted: {e.Message}");
                }
            }
            actions.Add(ShipJumpAction.Create(ship, jumpPointDB));
            return DispatchActions(actions, "jump could not be plotted");
        }

        private CommandResult TranslateNewtonThrust(Entity faction, Entity commanded, GameCommand command)
        {
            var thrust = (Pulsar4X.Api.NewtonThrustCommand)command;
            if (!commanded.TryGetDataBlob<Pulsar4X.Movement.NewtonThrustAbilityDB>(out var thrustAbility))
                return CommandResult.Reject("The entity has no newtonian thrust ability.");
            if (!commanded.TryGetDataBlob<MassVolumeDB>(out var massVolume))
                return CommandResult.Reject("The entity has no mass data.");

            var deltaV = new Pulsar4X.Orbital.Vector3(thrust.DeltaVMps.X, thrust.DeltaVMps.Y, thrust.DeltaVMps.Z);
            if (deltaV.Length() <= 0)
                return CommandResult.Reject("The burn has no ΔV.");
            if (deltaV.Length() > thrustAbility.DeltaV)
                return CommandResult.Reject("The burn exceeds the ship's available ΔV.");

            double fuelBurned = Pulsar4X.Orbital.OrbitalMath.TsiolkovskyFuelUse(
                massVolume.MassTotal, thrustAbility.ExhaustVelocity, deltaV.Length());
            double burnSeconds = thrustAbility.FuelBurnRate > 0 ? fuelBurned / thrustAbility.FuelBurnRate : 0;

            return Dispatch(Pulsar4X.Movement.NewtonThrustAction.CreateCommand(
                faction.Id, commanded, thrust.NodeTime, deltaV, burnSeconds));
        }

        private CommandResult TranslateWarpMove(Entity faction, Entity commanded, GameCommand command)
        {
            var warp = (Pulsar4X.Api.WarpMoveCommand)command;
            if (!TryResolve(warp.DestinationId, out var destination))
                return CommandResult.Reject($"Entity {warp.DestinationId} not found.");

            // Visibility enforced at the boundary: a faction can only warp to what it can see.
            if (destination.Manager == null
                || !destination.Manager.IsEntityVisibleToFaction(destination, faction.Id))
                return CommandResult.Reject($"Entity {warp.DestinationId} not found.");

            DateTime now = commanded.StarSysDateTime;
            try
            {
                if (warp.InsertionPointRelative is { } insertion)
                    return Pulsar4X.Movement.WarpMoveAction.CreateCommand(commanded, destination, now,
                            new Pulsar4X.Orbital.Vector3(insertion.X, insertion.Y, insertion.Z))
                        ? CommandResult.Ok(Guid.NewGuid().ToString("N"))
                        : CommandResult.Reject("Command rejected by engine validation.");

                return Dispatch(Pulsar4X.Movement.WarpMoveAction.CreateCommandEZ(commanded, destination, now));
            }
            catch (Exception e)
            {
                // The intercept/insertion math throws on movement states it can't predict
                // (e.g. an entity with no velocity); reject rather than crash the server.
                return CommandResult.Reject($"Warp could not be plotted: {e.Message}");
            }
        }

        // ----- fire control (commanded entity: the ship) -----

        private CommandResult TranslateSetFireControlWeapons(Entity faction, Entity commanded, GameCommand command)
        {
            var assign = (SetFireControlWeaponsCommand)command;
            bool accepted = Pulsar4X.Weapons.SetWeaponsFireControlOrder.CreateCommand(
                _game, commanded.StarSysDateTime, faction.Id, commanded.Id,
                assign.FireControlId, assign.WeaponIds.ToList());
            return accepted
                ? CommandResult.Ok(Guid.NewGuid().ToString("N"))
                : CommandResult.Reject("Command rejected by engine validation.");
        }

        private CommandResult TranslateSetFireControlTarget(Entity faction, Entity commanded, GameCommand command)
        {
            var target = (SetFireControlTargetCommand)command;
            if (!TryResolve(target.TargetId, out var targetEntity))
                return CommandResult.Reject($"Entity {target.TargetId} not found.");
            // A faction can only target what it can see (no locking onto undetected entities).
            if (targetEntity.Manager == null
                || !targetEntity.Manager.IsEntityVisibleToFaction(targetEntity, faction.Id))
                return CommandResult.Reject($"Entity {target.TargetId} not found.");

            bool accepted = Pulsar4X.Weapons.SetTargetFireControlOrder.CreateCommand(
                _game, commanded.StarSysDateTime, faction.Id, commanded.Id,
                target.FireControlId, target.TargetId);
            return accepted
                ? CommandResult.Ok(Guid.NewGuid().ToString("N"))
                : CommandResult.Reject("Command rejected by engine validation.");
        }

        private CommandResult TranslateAssignOrdnance(Entity faction, Entity commanded, GameCommand command)
        {
            var assign = (AssignOrdnanceCommand)command;
            bool accepted = Pulsar4X.Weapons.SetOrdinanceToWpnOrder.CreateCommand(
                _game, commanded.StarSysDateTime, faction.Id, commanded.Id,
                assign.WeaponId, assign.OrdnanceDesignId);
            return accepted
                ? CommandResult.Ok(Guid.NewGuid().ToString("N"))
                : CommandResult.Reject("Command rejected by engine validation.");
        }

        private CommandResult TranslateSetFireMode(Entity faction, Entity commanded, GameCommand command)
        {
            var mode = (SetFireModeCommand)command;
            bool accepted = Pulsar4X.Weapons.SetOpenFireControlOrder.CreateCmd(
                _game, faction.Id, commanded.Id, mode.FireControlId,
                mode.OpenFire
                    ? Pulsar4X.Weapons.SetOpenFireControlOrder.FireModes.OpenFire
                    : Pulsar4X.Weapons.SetOpenFireControlOrder.FireModes.CeaseFire);
            return accepted
                ? CommandResult.Ok(Guid.NewGuid().ToString("N"))
                : CommandResult.Reject("Command rejected by engine validation.");
        }

        // ----- captain (commanded entity: the ship). The fleet commander is the flagship's captain. -----

        private CommandResult TranslateAssignCaptain(Entity faction, Entity commanded, GameCommand command)
        {
            var assign = (AssignCaptainCommand)command;
            if (!commanded.TryGetDataBlob<ShipInfoDB>(out var shipInfo))
                return CommandResult.Reject("A captain sits a ship.");

            if (assign.CommanderId < 0)
            {
                if (shipInfo.CommanderID >= 0 && TryResolve(shipInfo.CommanderID, out var sitting))
                    ShipLogBook.NoteLeftPost(commanded, sitting, commanded.StarSysDateTime, "command");
                ClearChair(commanded, shipInfo);
                PublishPersonnelChanged(commanded);
                return CommandResult.Ok(Guid.NewGuid().ToString("N"));
            }

            if (!TryResolve(assign.CommanderId, out var officer)
                || officer.FactionOwnerID != faction.Id
                || !officer.TryGetDataBlob<CommanderDB>(out var commanderDB))
                return CommandResult.Reject($"Officer {assign.CommanderId} not found.");

            if (commanderDB.Type != DataStructures.CommanderTypes.Navy)
                return CommandResult.Reject("Only a navy officer can captain a ship.");

            if (NavalAcademyCourse.IsEnrolled(_game, officer.Id))
                return CommandResult.Reject("This officer is already on a course.");

            // A lab or admin post is not a ship. Leave that seat alone.
            if (commanderDB.AssignedTo >= 0 && commanderDB.AssignedTo != commanded.Id)
            {
                if (!TryResolve(commanderDB.AssignedTo, out var post) || !post.HasDataBlob<ShipInfoDB>())
                    return CommandResult.Reject("This officer already holds a post.");
            }

            var vacated = new List<Entity>();
            if (commanderDB.AssignedTo >= 0 && commanderDB.AssignedTo != commanded.Id
                && TryResolve(commanderDB.AssignedTo, out var previousShip))
            {
                ClearOfficer(previousShip, officer.Id, commanded.Id, vacated);
            }
            ClearOfficerFromFleet(faction, officer.Id, commanded.Id, vacated);

            int leaving = shipInfo.CommanderID;
            bool alreadySeated = leaving == officer.Id;
            Entity bumped = null;
            if (!alreadySeated && leaving >= 0 && TryResolve(leaving, out var leavingOfficer))
            {
                bumped = leavingOfficer;
                if (leavingOfficer.TryGetDataBlob<CommanderDB>(out var leavingDB)
                    && leavingDB.AssignedTo == commanded.Id)
                    leavingDB.AssignedTo = -1;
            }

            shipInfo.CommanderID = officer.Id;
            commanderDB.AssignedTo = commanded.Id;

            if (bumped != null)
                ShipLogBook.NoteLeftPost(commanded, bumped, commanded.StarSysDateTime, "command");
            if (!alreadySeated)
                ShipLogBook.NoteTookPost(commanded, officer, commanded.StarSysDateTime, "command");

            PublishPersonnelChanged(commanded);
            foreach (var ship in vacated)
                PublishPersonnelChanged(ship);

            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        // ----- administrator (commanded entity: the colony). One chair, the first post at colony level or wider. -----

        private CommandResult TranslateAssignAdministrator(Entity faction, Entity commanded, GameCommand command)
        {
            var assign = (AssignAdministratorCommand)command;
            if (!commanded.HasDataBlob<ColonyInfoDB>())
                return CommandResult.Reject("An administrator sits a colony.");

            var chair = ColonyChair(commanded);
            if (chair == null)
                return CommandResult.Reject("This colony has no administrative post.");

            if (assign.CommanderId < 0)
            {
                if (chair.CommanderID >= 0 && TryResolve(chair.CommanderID, out var sitting))
                {
                    ShipLogBook.NoteLeftPost(commanded, sitting, commanded.StarSysDateTime, "the post");
                    if (sitting.TryGetDataBlob<CommanderDB>(out var sittingDB)
                        && sittingDB.AssignedTo == commanded.Id)
                        sittingDB.AssignedTo = -1;
                }
                chair.CommanderID = -1;
                chair.Commander = null;
                PublishPersonnelChanged(commanded);
                return CommandResult.Ok(Guid.NewGuid().ToString("N"));
            }

            if (!TryResolve(assign.CommanderId, out var person)
                || person.FactionOwnerID != faction.Id
                || !person.TryGetDataBlob<CommanderDB>(out var commanderDB))
                return CommandResult.Reject($"Civilian {assign.CommanderId} not found.");

            if (commanderDB.Type != DataStructures.CommanderTypes.Civilian)
                return CommandResult.Reject("Only a civilian can administer a colony.");

            if (chair.CommanderID == person.Id)
                return CommandResult.Ok(Guid.NewGuid().ToString("N"));

            var vacated = new List<Entity>();
            if (commanderDB.AssignedTo >= 0
                && commanderDB.AssignedTo != commanded.Id
                && TryResolve(commanderDB.AssignedTo, out var previous))
            {
                ClearPostedCivilian(previous, person, vacated);
            }

            int leaving = chair.CommanderID;
            if (leaving >= 0 && leaving != person.Id && TryResolve(leaving, out var bumped))
            {
                ShipLogBook.NoteLeftPost(commanded, bumped, commanded.StarSysDateTime, "the post");
                if (bumped.TryGetDataBlob<CommanderDB>(out var bumpedDB)
                    && bumpedDB.AssignedTo == commanded.Id)
                    bumpedDB.AssignedTo = -1;
            }

            chair.CommanderID = person.Id;
            chair.Commander = commanderDB;
            commanderDB.AssignedTo = commanded.Id;
            ShipLogBook.NoteTookPost(commanded, person, commanded.StarSysDateTime, "the post");

            PublishPersonnelChanged(commanded);
            foreach (var entity in vacated)
                PublishPersonnelChanged(entity);

            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        // A ship bridge is not a colony chair. Extra posts are not listed.
        private static AdminSpaceAbilityState? ColonyChair(Entity colony)
        {
            if (!colony.TryGetDataBlob<AdminSpaceDB>(out var admin))
                return null;
            foreach (var seat in admin.CommanderSeats)
            {
                if (seat.SeatType >= AdminLevel.Colony)
                    return seat;
            }
            return null;
        }

        // Leaves every admin seat on the previous entity that this civilian holds.
        // The previous post is found by the person, not by the component name.
        private static void ClearPostedCivilian(Entity previous, Entity person, List<Entity> vacated)
        {
            if (!person.TryGetDataBlob<CommanderDB>(out var commanderDB))
                return;

            bool left = false;
            if (previous.TryGetDataBlob<AdminSpaceDB>(out var admin))
            {
                foreach (var seat in admin.CommanderSeats)
                {
                    if (seat.CommanderID != person.Id)
                        continue;
                    seat.CommanderID = -1;
                    seat.Commander = null;
                    left = true;
                }
            }

            if (commanderDB.AssignedTo == previous.Id)
                commanderDB.AssignedTo = -1;
            if (!left)
                return;

            ShipLogBook.NoteLeftPost(previous, person, previous.StarSysDateTime, "the post");
            if (!vacated.Contains(previous))
                vacated.Add(previous);
        }

        private CommandResult TranslateTrainAcademyClass(Entity faction, Entity commanded, GameCommand command)
        {
            var train = (TrainAcademyClassCommand)command;
            var reason = NavalAcademyCourse.TryTrain(commanded, train.ComponentId, train.Count);
            return reason == null
                ? CommandResult.Ok(Guid.NewGuid().ToString("N"))
                : CommandResult.Reject(reason);
        }

        private CommandResult TranslateAcademyCommandCourse(Entity faction, Entity commanded, GameCommand command)
        {
            var course = (AcademyCommandCourseCommand)command;
            if (!TryResolve(course.CommanderId, out var officer) || officer.FactionOwnerID != faction.Id)
                return CommandResult.Reject($"Officer {course.CommanderId} not found.");
            var reason = NavalAcademyCourse.TryCommand(commanded, course.ComponentId, officer);
            return reason == null
                ? CommandResult.Ok(Guid.NewGuid().ToString("N"))
                : CommandResult.Reject(reason);
        }

        private CommandResult TranslateCancelAcademyCourse(Entity faction, Entity commanded, GameCommand command)
        {
            var cancel = (CancelAcademyCourseCommand)command;
            var reason = NavalAcademyCourse.TryCancel(commanded, cancel.ComponentId, cancel.CohortId);
            return reason == null
                ? CommandResult.Ok(Guid.NewGuid().ToString("N"))
                : CommandResult.Reject(reason);
        }

        private CommandResult TranslateHaulPair(Entity faction, Entity commanded, GameCommand command)
        {
            var haul = (HaulPairCommand)command;
            if (commanded.HasDataBlob<FleetDB>())
                return CommandResult.Reject("Haul is one ship.");
            if (RejectGoalWithoutCaptain(commanded) is { } rejected)
                return rejected;
            if (!commanded.HasDataBlob<CargoStorageDB>())
                return CommandResult.Reject("The ship has no cargo storage.");
            if (string.IsNullOrEmpty(haul.CargoId))
                return CommandResult.Reject("Cargo id is required.");
            if (!TryResolve(haul.SourceId, out var source) || !TryResolve(haul.DestId, out var dest)
                || !LogisticsSpan.ContractReaches(commanded, source, dest))
                return CommandResult.Reject("The target is outside this ship's reach.");
            if (source.Id == dest.Id
                || !source.HasDataBlob<ColonyInfoDB>()
                || !dest.HasDataBlob<ColonyInfoDB>())
                return CommandResult.Reject("The contract needs two colonies.");
            if (source.FactionOwnerID != commanded.FactionOwnerID || dest.FactionOwnerID != commanded.FactionOwnerID)
                return CommandResult.Reject("The colony is not yours.");
            if (!source.HasDataBlob<CargoStorageDB>() || !dest.HasDataBlob<CargoStorageDB>())
                return CommandResult.Reject("The colony has no warehouse.");

            var goal = new Goal(GoalType.Freighter)
            {
                CargoId = haul.CargoId,
                SourceEntityId = source.Id,
                DestEntityId = dest.Id,
                TargetEntityID = ColonyAnchor(dest),
            };
            AgentProcessor.AssignGoal(commanded, goal);
            if (goal.Status == GoalStatus.Failed)
                return CommandResult.Reject(goal.Message);
            return CommandResult.Ok(goal.Id);
        }

        private CommandResult TranslateTradePair(Entity faction, Entity commanded, GameCommand command)
        {
            var trade = (TradePairCommand)command;
            if (commanded.HasDataBlob<FleetDB>())
                return CommandResult.Reject("Trade is one ship.");
            if (RejectGoalWithoutCaptain(commanded) is { } rejected)
                return rejected;
            if (!commanded.HasDataBlob<CargoStorageDB>())
                return CommandResult.Reject("The ship has no cargo storage.");
            if (string.IsNullOrEmpty(trade.CargoId))
                return CommandResult.Reject("Cargo id is required.");
            if (!TryResolve(trade.SourceId, out var source) || !TryResolve(trade.DestId, out var dest)
                || !LogisticsSpan.ContractReaches(commanded, source, dest))
                return CommandResult.Reject("The target is outside this ship's reach.");
            if (source.Id == dest.Id
                || !source.HasDataBlob<ColonyInfoDB>()
                || !dest.HasDataBlob<ColonyInfoDB>())
                return CommandResult.Reject("The contract needs two colonies.");

            var goal = new Goal(GoalType.Trade)
            {
                CargoId = trade.CargoId,
                SourceEntityId = source.Id,
                DestEntityId = dest.Id,
                TargetEntityID = ColonyAnchor(dest),
            };
            AgentProcessor.AssignGoal(commanded, goal);
            if (goal.Status == GoalStatus.Failed)
                return CommandResult.Reject(goal.Message);
            return CommandResult.Ok(goal.Id);
        }

        static int ColonyAnchor(Entity colony)
        {
            if (colony.TryGetDataBlob<ColonyInfoDB>(out var info)
                && info.PlanetEntity != null
                && info.PlanetEntity.IsValid)
                return info.PlanetEntity.Id;
            return colony.Id;
        }

        private void ClearChair(Entity ship, ShipInfoDB shipInfo)
        {
            int leaving = shipInfo.CommanderID;
            shipInfo.CommanderID = -1;
            if (leaving >= 0
                && TryResolve(leaving, out var officer)
                && officer.TryGetDataBlob<CommanderDB>(out var commanderDB)
                && commanderDB.AssignedTo == ship.Id)
            {
                commanderDB.AssignedTo = -1;
            }
        }

        // Factory captains record the person only on the ship. Walk the fleet tree so moving
        // an officer clears the chair they actually sit, even when AssignedTo was never set.
        private static void ClearOfficerFromFleet(Entity faction, int commanderId, int exceptShipId, List<Entity> vacated)
        {
            if (!faction.TryGetDataBlob<FleetDB>(out var factionFleet) || factionFleet.RootDB == null)
                return;
            foreach (var child in factionFleet.RootDB.GetChildren())
                ClearOfficer(child, commanderId, exceptShipId, vacated);
        }

        private static void ClearOfficer(Entity node, int commanderId, int exceptShipId, List<Entity> vacated)
        {
            if (node.Id != exceptShipId
                && node.TryGetDataBlob<ShipInfoDB>(out var info)
                && info.CommanderID == commanderId)
            {
                if (node.Manager != null && node.Manager.TryGetGlobalEntityById(commanderId, out var officer))
                    ShipLogBook.NoteLeftPost(node, officer, node.StarSysDateTime, "command");
                info.CommanderID = -1;
                if (!vacated.Contains(node))
                    vacated.Add(node);
            }

            if (node.TryGetDataBlob<FleetDB>(out var fleetDB))
            {
                foreach (var child in fleetDB.GetChildren())
                    ClearOfficer(child, commanderId, exceptShipId, vacated);
            }
        }

        // ----- research (commanded entity: the lab) -----

        private CommandResult TranslateAssignScientist(Entity faction, Entity commanded, GameCommand command)
        {
            var assign = (AssignScientistCommand)command;
            if (!TryResolve(assign.ScientistId, out var scientist)
                || scientist.FactionOwnerID != faction.Id)
                return CommandResult.Reject($"Scientist {assign.ScientistId} not found.");

            return Dispatch(AssignScientistOrder.Create(commanded, assign.ScientistId));
        }

        private CommandResult TranslateUnassignScientist(Entity faction, Entity commanded, GameCommand command)
        {
            var unassign = (UnassignScientistCommand)command;
            return Dispatch(UnassignScientistOrder.Create(commanded, unassign.ScientistId));
        }

        private CommandResult TranslateSetResearchFunding(Entity faction, Entity commanded, GameCommand command)
        {
            var funding = (SetResearchFundingCommand)command;
            if (funding.FundingLevel is < 0 or > 5)
                return CommandResult.Reject("Funding level must be between 0 and 5.");

            return Dispatch(FundingChangedOrder.Create(commanded, (byte)funding.FundingLevel));
        }

        private CommandResult TranslateAddTechToQueue(Entity faction, Entity commanded, GameCommand command)
        {
            var add = (AddTechToQueueCommand)command;
            return Dispatch(AddTechToQueueOrder.Create(commanded, add.TechId));
        }

        private CommandResult TranslateRemoveTechFromQueue(Entity faction, Entity commanded, GameCommand command)
        {
            var remove = (RemoveTechFromQueueCommand)command;
            return Dispatch(RemoveTechFromQueueOrder.Create(commanded, remove.TechId));
        }

        private CommandResult TranslateMoveTechInQueue(Entity faction, Entity commanded, GameCommand command)
        {
            var move = (MoveTechInQueueCommand)command;
            return move.MoveUp
                ? Dispatch(MoveUpInQueueOrder.Create(commanded, move.TechId))
                : Dispatch(MoveDownInQueueOrder.Create(commanded, move.TechId));
        }

        // ----- component design (commanded entity: the faction itself) -----

        private CommandResult TranslateCreateComponentDesign(Entity faction, Entity commanded, GameCommand command)
        {
            var create = (CreateComponentDesignCommand)command;
            if (commanded != faction)
                return CommandResult.Reject("A component design can only be created by commanding the faction itself.");

            if (string.IsNullOrWhiteSpace(create.Name))
                return CommandResult.Reject("The design needs a name.");

            if (!faction.TryGetDataBlob<FactionInfoDB>(out var factionInfo)
                || !factionInfo.Data.ComponentTemplates.TryGetValue(create.TemplateId, out var template))
                return CommandResult.Reject($"Component template {create.TemplateId} not found.");

            if (!faction.TryGetDataBlob<FactionTechDB>(out var factionTech))
                return CommandResult.Reject("The faction has no tech state.");

            try
            {
                // Like the research instant orders, design creation is a direct faction-data write
                // (no engine order exists): replay the inputs onto a fresh designer and finalise it,
                // which also registers the design's research project.
                var designer = DesignerInputs.Build(factionInfo.Data, factionTech, template,
                    create.Inputs ?? Array.Empty<DesignerInput>());
                designer.Name = create.Name;
                designer.CreateDesign(faction);
            }
            catch (Exception e)
            {
                // Formula/attribute evaluation runs over moddable data; reject rather than crash.
                return CommandResult.Reject($"Design creation failed: {e.Message}");
            }

            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        // ----- ship design (commanded entity: the faction itself) -----

        private CommandResult TranslateSaveShipDesign(Entity faction, Entity commanded, GameCommand command)
        {
            var save = (SaveShipDesignCommand)command;
            if (commanded != faction)
                return CommandResult.Reject("A ship design can only be saved by commanding the faction itself.");

            if (string.IsNullOrWhiteSpace(save.Name))
                return CommandResult.Reject("The design needs a name.");

            if (!faction.TryGetDataBlob<FactionInfoDB>(out var factionInfo))
                return CommandResult.Reject("The faction has no data store.");

            if (!factionInfo.Data.Armor.TryGetValue(save.ArmorId, out var armor))
                return CommandResult.Reject($"Armor {save.ArmorId} not found.");

            if (save.ArmorThickness < 0)
                return CommandResult.Reject("Armor thickness cannot be negative.");

            // Resolve the component stacks against the faction's own designs — referencing another
            // faction's (or an unknown) component rejects here at the boundary.
            var components = new List<(ComponentDesign design, int count)>(save.Components?.Count ?? 0);
            foreach (var entry in save.Components ?? Array.Empty<ShipComponentCount>())
            {
                if (!factionInfo.ComponentDesigns.TryGetValue(entry.ComponentDesignId, out var componentDesign))
                    return CommandResult.Reject($"Component design {entry.ComponentDesignId} not found.");
                if (entry.Count < 0)
                    return CommandResult.Reject("Component counts cannot be negative.");
                components.Add((componentDesign, entry.Count));
            }

            try
            {
                Pulsar4X.Ships.ShipDesign design;
                if (!string.IsNullOrEmpty(save.DesignId))
                {
                    if (!factionInfo.ShipDesigns.TryGetValue(save.DesignId, out design!))
                        return CommandResult.Reject($"Ship design {save.DesignId} not found.");

                    design.Name = save.Name;
                    design.Components = components;
                    design.Armor = (armor, save.ArmorThickness);
                }
                else
                {
                    design = new Pulsar4X.Ships.ShipDesign(factionInfo, save.Name, components, (armor, save.ArmorThickness));
                }

                design.IsObsolete = save.IsObsolete;
                design.Tanker = save.Tanker;
                // Recalculates the derived values and (re-)registers the design on the faction.
                design.Initialise(factionInfo);
                design.IsValid = !design.IsObsolete && IsShipDesignValid(design);
            }
            catch (Exception e)
            {
                // The damage-profile/armor math runs over moddable data; reject rather than crash.
                return CommandResult.Reject($"Ship design save failed: {e.Message}");
            }

            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        // Mirrors the designer UI's validity rule: a producible ship needs mass, newtonian thrust,
        // and energy generation + storage.
        private static bool IsShipDesignValid(Pulsar4X.Ships.ShipDesign design)
        {
            bool hasThrust = false, hasEnergyGen = false, hasEnergyStore = false;
            foreach (var (componentDesign, count) in design.Components)
            {
                if (count <= 0) continue;
                hasThrust |= componentDesign.HasAttribute<NewtonionThrustAtb>();
                hasEnergyGen |= componentDesign.HasAttribute<Pulsar4X.Energy.EnergyGenerationAtb>();
                hasEnergyStore |= componentDesign.HasAttribute<Pulsar4X.Energy.EnergyStoreAtb>();
            }

            return design.MassPerUnit > 0 && hasThrust && hasEnergyGen && hasEnergyStore;
        }

        private CommandResult TranslateDeleteShipDesign(Entity faction, Entity commanded, GameCommand command)
        {
            var delete = (DeleteShipDesignCommand)command;
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var factionInfo)
                || !factionInfo.ShipDesigns.Remove(delete.DesignId))
                return CommandResult.Reject($"Ship design {delete.DesignId} not found.");

            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        private CommandResult TranslateSetShipDesignObsolete(Entity faction, Entity commanded, GameCommand command)
        {
            var obsolete = (SetShipDesignObsoleteCommand)command;
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var factionInfo)
                || !factionInfo.ShipDesigns.TryGetValue(obsolete.DesignId, out var design))
                return CommandResult.Reject($"Ship design {obsolete.DesignId} not found.");

            design.IsObsolete = true;
            return CommandResult.Ok(Guid.NewGuid().ToString("N"));
        }

        // ----- installations/components (commanded entity: the colony or ship holding them) -----

        private CommandResult TranslateUninstallComponent(Entity faction, Entity commanded, GameCommand command)
        {
            var uninstall = (UninstallComponentCommand)command;
            if (!commanded.TryGetDataBlob<ComponentInstancesDB>(out var instancesDB)
                || !instancesDB.ComponentsByDesign.TryGetValue(uninstall.DesignId, out var instances)
                || instances.Count == 0)
                return CommandResult.Reject($"No installed component of design {uninstall.DesignId}.");

            var instance = instances[0];
            if (!commanded.TryGetDataBlob<Pulsar4X.Storage.CargoStorageDB>(out var storage)
                || !storage.TypeStores.ContainsKey(instance.CargoTypeID))
                return CommandResult.Reject("The entity has no cargo storage that can hold the component.");

            if (!_game.OrderHandler.HandleOrder(UninstallComponentInstanceOrder.Create(commanded, instance)))
                return CommandResult.Reject("Command rejected by engine validation.");

            return Dispatch(AddComponentToStorageOrder.Create(commanded, instance));
        }

        private CommandResult TranslateInstallComponent(Entity faction, Entity commanded, GameCommand command)
        {
            var install = (InstallComponentCommand)command;
            bool colony = commanded.HasDataBlob<ColonyInfoDB>();
            if (!commanded.TryGetDataBlob<LocalConstructionDB>(out var construction) || construction.PointsPerDay <= 0)
                return CommandResult.Reject(colony
                    ? "This colony has no installation points."
                    : "This ship has no installation points.");

            if (!TryFindCargoComponent(commanded, install.ComponentId, out var instance))
                return CommandResult.Reject(colony
                    ? "That component is not in the warehouse."
                    : "That component is not in the hold.");

            if (!LocalConstructionJob.IsCrate(instance.Design))
                return CommandResult.Reject("That component is not a colony installation crate.");

            if (AlreadyQueued(construction, instance))
                return CommandResult.Reject("That component is already in the construction queue.");

            if (colony)
                return Dispatch(AddToConstructionQueueOrder.CreateMount(commanded, instance));

            if (install.ColonyId is not int colonyId)
                return CommandResult.Reject("No colony in transfer range.");
            if (!TryResolveConstructionColony(commanded, colonyId, out var target, out var reason))
                return CommandResult.Reject(reason);

            return Dispatch(AddToConstructionQueueOrder.CreateMount(commanded, instance, target));
        }

        // ----- industry / local construction (commanded entity: the colony) -----

        // IndustryTools index ProductionLines unguarded and IndustryOrder2 runs synchronously, so a
        // bad/stale line id would throw out of SubmitCommand rather than reject. Validate it here.
        private CommandResult? ValidateIndustryLine(Entity commanded, string productionLineId)
        {
            if (!commanded.TryGetDataBlob<IndustryAbilityDB>(out var industryDB))
                return CommandResult.Reject("The commanded entity has no industry.");
            if (!industryDB.ProductionLines.ContainsKey(productionLineId))
                return CommandResult.Reject($"Production line {productionLineId} not found.");
            return null;
        }

        private CommandResult TranslateQueueIndustryJob(Entity faction, Entity commanded, GameCommand command)
        {
            var queue = (QueueIndustryJobCommand)command;
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var factionInfo)
                || !factionInfo.IndustryDesigns.TryGetValue(queue.DesignId, out var design))
                return CommandResult.Reject($"Design {queue.DesignId} not found.");

            if (queue.Quantity is < 1 or > ushort.MaxValue)
                return CommandResult.Reject("Quantity must be between 1 and 65535.");

            if (ValidateIndustryLine(commanded, queue.ProductionLineId) is { } lineError)
                return lineError;

            if (design is ShipDesign ship
                && commanded.TryGetDataBlob<IndustryAbilityDB>(out var industry)
                && industry.ProductionLines.TryGetValue(queue.ProductionLineId, out var line)
                && ship.MassPerUnit > line.MaxVolume)
                return CommandResult.Reject("That design is over this slip.");

            var job = new IndustryJob(factionInfo, queue.DesignId);

            // A checked box queues a mount on a colony that has installation points. It does not install now.
            if (queue.AutoInstall
                && design is Pulsar4X.Components.ComponentDesign componentDesign
                && componentDesign.ComponentMountType.HasFlag(DataStructures.ComponentMountType.PlanetInstallation)
                && commanded.TryGetDataBlob<LocalConstructionDB>(out var construction)
                && construction.PointsPerDay > 0)
            {
                job.InstallOn = commanded;
            }

            job.InitialiseJob((ushort)queue.Quantity, queue.Repeat);
            return Dispatch(IndustryOrder2.CreateNewJobOrder(faction.Id, commanded, queue.ProductionLineId, job));
        }

        private CommandResult TranslateChangeIndustryJobPriority(Entity faction, Entity commanded, GameCommand command)
        {
            var move = (ChangeIndustryJobPriorityCommand)command;
            if (ValidateIndustryLine(commanded, move.ProductionLineId) is { } lineError)
                return lineError;
            return Dispatch(IndustryOrder2.CreateChangePriorityOrder(
                faction.Id, commanded, move.ProductionLineId, move.JobId, (short)move.Delta));
        }

        private CommandResult TranslateCancelIndustryJob(Entity faction, Entity commanded, GameCommand command)
        {
            var cancel = (CancelIndustryJobCommand)command;
            if (ValidateIndustryLine(commanded, cancel.ProductionLineId) is { } lineError)
                return lineError;
            return Dispatch(IndustryOrder2.CreateCancelJobOrder(
                faction.Id, commanded, cancel.ProductionLineId, cancel.JobId));
        }

        private CommandResult TranslateQueueShipRefit(Entity faction, Entity commanded, GameCommand command)
        {
            var refit = (QueueShipRefitCommand)command;
            if (!TryResolveShip(commanded, refit.ShipEntityId, out var ship))
                return CommandResult.Reject(ShipRefitJob.OutOfRange);

            var remove = new List<ComponentInstance>();
            foreach (var componentId in refit.RemoveInstanceIds ?? Array.Empty<int>())
            {
                if (!TryFindMounted(ship, componentId, out var instance))
                    return CommandResult.Reject(ShipRefitJob.NotOnShip);
                remove.Add(instance);
            }

            var mount = new List<ComponentInstance>();
            foreach (var componentId in refit.MountInstanceIds ?? Array.Empty<int>())
            {
                if (!TryFindCargoComponent(commanded, componentId, out var instance))
                    return CommandResult.Reject(ShipRefitJob.NotInWarehouse);
                mount.Add(instance);
            }

            var reason = ShipRefitJob.Rejection(commanded, refit.ProductionLineId, ship, remove, mount);
            if (reason != null)
                return CommandResult.Reject(reason);
            string? targetDesignId = string.IsNullOrEmpty(refit.TargetDesignId) ? null : refit.TargetDesignId;
            if (targetDesignId != null
                && (!faction.TryGetDataBlob<FactionInfoDB>(out var info)
                    || !info.ShipDesigns.ContainsKey(targetDesignId)))
                return CommandResult.Reject(ShipRefitJob.UnknownDesign);
            return Dispatch(QueueShipRefitOrder.Create(
                commanded, refit.ProductionLineId, ship, remove, mount, targetDesignId));
        }

        static bool TryResolveShip(Entity yard, int shipId, out Entity ship)
        {
            if (yard.Manager != null && yard.Manager.TryGetEntityById(shipId, out ship!))
                return true;
            ship = null!;
            return false;
        }

        static bool TryFindMounted(Entity ship, int componentId, out ComponentInstance instance)
        {
            instance = null!;
            if (!ship.TryGetDataBlob<ComponentInstancesDB>(out var components))
                return false;
            foreach (var part in components.AllComponents.Values)
            {
                if (part.ID != componentId)
                    continue;
                instance = part;
                return true;
            }
            return false;
        }

        private CommandResult TranslateAddToConstructionQueue(Entity faction, Entity commanded, GameCommand command)
        {
            var add = (AddToConstructionQueueCommand)command;
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var factionInfo)
                || !factionInfo.ComponentDesigns.TryGetValue(add.DesignId, out var design))
                return CommandResult.Reject($"Design {add.DesignId} not found.");

            bool colony = commanded.HasDataBlob<ColonyInfoDB>();
            if (!commanded.TryGetDataBlob<LocalConstructionDB>(out var construction) || construction.PointsPerDay <= 0)
                return CommandResult.Reject(colony
                    ? "This colony has no installation points."
                    : "This ship has no installation points.");

            if (add.ComponentId is int componentId)
            {
                if (!TryFindCargoComponent(commanded, componentId, out var instance))
                    return CommandResult.Reject(colony
                        ? "That component is not in the warehouse."
                        : "That component is not in the hold.");
                if (instance.Design.UniqueID != design.UniqueID)
                    return CommandResult.Reject("That crate is a different design.");
                if (!LocalConstructionJob.IsCrate(instance.Design))
                    return CommandResult.Reject("That component is not a colony installation crate.");
                if (AlreadyQueued(construction, instance))
                    return CommandResult.Reject("That component is already in the construction queue.");

                if (colony)
                    return Dispatch(AddToConstructionQueueOrder.CreateMount(commanded, instance));
                if (add.ColonyId is not int mountColonyId)
                    return CommandResult.Reject("No colony in transfer range.");
                if (!TryResolveConstructionColony(commanded, mountColonyId, out var mountTarget, out var mountReason))
                    return CommandResult.Reject(mountReason);
                return Dispatch(AddToConstructionQueueOrder.CreateMount(commanded, instance, mountTarget));
            }

            if (!LocalConstructionJob.IsSiteOnly(design))
                return CommandResult.Reject(colony
                    ? "That design is carried as cargo. Queue a crate from the warehouse."
                    : "That design is carried as cargo. Queue a crate from the hold.");

            if (colony)
                return Dispatch(AddToConstructionQueueOrder.CreateAssemble(commanded, design));
            if (add.ColonyId is not int assembleColonyId)
                return CommandResult.Reject("No colony in transfer range.");
            if (!TryResolveConstructionColony(commanded, assembleColonyId, out var assembleTarget, out var assembleReason))
                return CommandResult.Reject(assembleReason);
            return Dispatch(AddToConstructionQueueOrder.CreateAssemble(commanded, design, assembleTarget));
        }

        static bool TryResolveConstructionColony(Entity ship, int colonyId, out Entity colony, out string reason)
        {
            colony = null!;
            if (ship.Manager == null || !ship.Manager.TryGetEntityById(colonyId, out colony!))
            {
                reason = "That colony was not found.";
                return false;
            }
            if (!colony.HasDataBlob<ColonyInfoDB>()
                || colony.FactionOwnerID != ship.FactionOwnerID
                || !LocalConstructionProcessor.InTransferRange(ship, colony))
            {
                reason = "That colony is out of transfer range.";
                return false;
            }
            reason = "";
            return true;
        }

        static bool AlreadyQueued(LocalConstructionDB construction, ComponentInstance instance)
        {
            foreach (var job in construction.BuildQueue)
            {
                if (job.Instance != null && job.Instance.ID == instance.ID)
                    return true;
            }
            return false;
        }

        static bool TryFindCargoComponent(Entity entity, int componentId, out ComponentInstance instance)
        {
            instance = null!;
            if (!entity.TryGetDataBlob<CargoStorageDB>(out var storage))
                return false;

            foreach (var pair in storage.TypeStores)
            {
                if (pair.Value.GetCargoables().TryGetValue(componentId, out var cargoable)
                    && cargoable is ComponentInstance found)
                {
                    instance = found;
                    return true;
                }
            }
            return false;
        }

        private CommandResult TranslateMoveConstructionJob(Entity faction, Entity commanded, GameCommand command)
        {
            var move = (MoveConstructionJobCommand)command;
            if (!TryGetConstructionJob(commanded, move.QueueIndex, out var job))
                return CommandResult.Reject($"No construction job at queue position {move.QueueIndex}.");

            return move.MoveUp
                ? Dispatch(MoveUpInConstructionQueueOrder.Create(commanded, job))
                : Dispatch(MoveDownInConstructionQueueOrder.Create(commanded, job));
        }

        private CommandResult TranslateRemoveConstructionJob(Entity faction, Entity commanded, GameCommand command)
        {
            var remove = (RemoveConstructionJobCommand)command;
            if (!TryGetConstructionJob(commanded, remove.QueueIndex, out var job))
                return CommandResult.Reject($"No construction job at queue position {remove.QueueIndex}.");

            return Dispatch(RemoveFromConstructionQueueOrder.Create(commanded, job));
        }

        // Local-construction jobs carry no id, so commands address them by queue position.
        private static bool TryGetConstructionJob(Entity entity, int queueIndex, out LocalConstructionJob job)
        {
            job = null!;
            if (!entity.TryGetDataBlob<LocalConstructionDB>(out var construction)) return false;
            if (queueIndex < 0 || queueIndex >= construction.BuildQueue.Count) return false;

            job = construction.BuildQueue.ElementAt(queueIndex);
            return true;
        }
    }
}
