using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Api;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Fleets;
using Pulsar4X.GeoSurveys;
using Pulsar4X.JumpPoints;
using Pulsar4X.Movement;
using Pulsar4X.Names;
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
                [typeof(CreateFleetCommand)] = TranslateCreateFleet,
                [typeof(DisbandFleetCommand)] = TranslateDisbandFleet,
                [typeof(ChangeFleetParentCommand)] = TranslateChangeFleetParent,
                [typeof(ReassignShipCommand)] = TranslateReassignShip,
                [typeof(SetFlagshipCommand)] = TranslateSetFlagship,
                [typeof(MoveToBodyCommand)] = TranslateMoveToBody,
                [typeof(Pulsar4X.Api.GeoSurveyCommand)] = TranslateGeoSurvey,
                [typeof(GravSurveyCommand)] = TranslateGravSurvey,
                [typeof(Pulsar4X.Api.JumpCommand)] = TranslateJump,
                [typeof(RefuelAtCommand)] = TranslateRefuelAt,
                [typeof(AssignScientistCommand)] = TranslateAssignScientist,
                [typeof(UnassignScientistCommand)] = TranslateUnassignScientist,
                [typeof(SetResearchFundingCommand)] = TranslateSetResearchFunding,
                [typeof(AddTechToQueueCommand)] = TranslateAddTechToQueue,
                [typeof(RemoveTechFromQueueCommand)] = TranslateRemoveTechFromQueue,
                [typeof(MoveTechInQueueCommand)] = TranslateMoveTechInQueue,
            };
        }

        /// <summary>
        /// Translates and dispatches a command that has already been authorized: <paramref name="faction"/>
        /// is the requesting faction entity and <paramref name="commanded"/> the resolved, owned target.
        /// </summary>
        public CommandResult Translate(Entity faction, Entity commanded, GameCommand command)
        {
            if (!_translators.TryGetValue(command.GetType(), out var translate))
                return CommandResult.Reject($"Unsupported command: {command.GetType().Name}");

            return translate(faction, commanded, command);
        }

        // ----- helpers -----

        private CommandResult Dispatch(EntityCommand order)
            => _game.OrderHandler.HandleOrder(order)
                ? CommandResult.Ok(Guid.NewGuid().ToString("N"))
                : CommandResult.Reject("Command rejected by engine validation.");

        private bool TryResolve(int entityId, out Entity entity)
            => _game.GlobalManager.TryGetGlobalEntityById(entityId, out entity);

        /// <summary>Finds the fleet in the faction's command tree whose direct children include
        /// <paramref name="ship"/>, or null when the ship sits at the faction root (or isn't in the tree).</summary>
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
        private CommandResult TranslateRename(Entity faction, Entity commanded, GameCommand command)
        {
            var rename = (Pulsar4X.Api.RenameCommand)command;
            bool accepted = Pulsar4X.Names.RenameCommand.CreateRenameCommand(_game, faction, commanded, rename.NewName);
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

        private CommandResult TranslateDisbandFleet(Entity faction, Entity commanded, GameCommand command)
            => Dispatch(FleetOrder.DisbandFleet(faction.Id, commanded));

        private CommandResult TranslateChangeFleetParent(Entity faction, Entity commanded, GameCommand command)
        {
            var change = (ChangeFleetParentCommand)command;
            if (!TryResolve(change.NewParentId, out var newParent))
                return CommandResult.Reject($"Entity {change.NewParentId} not found.");

            return Dispatch(FleetOrder.ChangeParent(faction.Id, commanded, newParent));
        }

        private CommandResult TranslateReassignShip(Entity faction, Entity commanded, GameCommand command)
        {
            var reassign = (ReassignShipCommand)command;
            if (!TryResolve(reassign.ToFleetId, out var toFleet))
                return CommandResult.Reject($"Entity {reassign.ToFleetId} not found.");
            if (!toFleet.HasDataBlob<FleetDB>())
                return CommandResult.Reject("Reassignment target is not a fleet.");

            // Detach the ship from wherever it currently sits: a fleet in the tree, or the faction root.
            var holder = FindHoldingFleet(faction, commanded) ?? faction;
            var unassign = FleetOrder.UnassignShip(faction.Id, holder, commanded);
            if (!_game.OrderHandler.HandleOrder(unassign))
                return CommandResult.Reject("Command rejected by engine validation.");

            return Dispatch(FleetOrder.AssignShip(faction.Id, toFleet, commanded));
        }

        private CommandResult TranslateSetFlagship(Entity faction, Entity commanded, GameCommand command)
        {
            var setFlagship = (SetFlagshipCommand)command;
            if (!TryResolve(setFlagship.ShipId, out var ship))
                return CommandResult.Reject($"Entity {setFlagship.ShipId} not found.");

            return Dispatch(FleetOrder.SetFlagShip(faction.Id, commanded, ship));
        }

        private CommandResult TranslateMoveToBody(Entity faction, Entity commanded, GameCommand command)
        {
            var move = (MoveToBodyCommand)command;
            if (!TryResolve(move.BodyId, out var body))
                return CommandResult.Reject($"Entity {move.BodyId} not found.");

            return Dispatch(MoveToSystemBodyOrder.CreateCommand(faction.Id, commanded, body));
        }

        private CommandResult TranslateGeoSurvey(Entity faction, Entity commanded, GameCommand command)
        {
            var survey = (Pulsar4X.Api.GeoSurveyCommand)command;
            if (!TryResolve(survey.BodyId, out var body))
                return CommandResult.Reject($"Entity {survey.BodyId} not found.");

            if (!_game.OrderHandler.HandleOrder(WarpFleetTowardsTargetOrder.CreateCommand(commanded, body)))
                return CommandResult.Reject("Command rejected by engine validation.");

            return Dispatch(GeoSurveyOrder.CreateCommand(faction.Id, commanded, body));
        }

        private CommandResult TranslateGravSurvey(Entity faction, Entity commanded, GameCommand command)
        {
            var survey = (GravSurveyCommand)command;
            if (!TryResolve(survey.LocationId, out var location))
                return CommandResult.Reject($"Entity {survey.LocationId} not found.");

            if (!_game.OrderHandler.HandleOrder(WarpFleetTowardsTargetOrder.CreateCommand(commanded, location)))
                return CommandResult.Reject("Command rejected by engine validation.");

            return Dispatch(JPSurveyOrder.CreateCommand(faction.Id, commanded, location));
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

            if (!_game.OrderHandler.HandleOrder(WarpFleetTowardsTargetOrder.CreateCommand(commanded, colony)))
                return CommandResult.Reject("Command rejected by engine validation.");

            return CargoTransferOrder.CreateRefuelFleetCommand(colony, commanded)
                ? CommandResult.Ok(Guid.NewGuid().ToString("N"))
                : CommandResult.Reject("No ship in the fleet could take on fuel.");
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
    }
}
