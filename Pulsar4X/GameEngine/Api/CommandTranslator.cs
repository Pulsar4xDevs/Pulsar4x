using System;
using System.Collections.Generic;
using Pulsar4X.Api;
using Pulsar4X.Engine;

namespace Pulsar4X.Engine.Api
{
    /// <summary>
    /// Translates authorized API <see cref="GameCommand"/> DTOs into engine orders and dispatches them.
    /// <see cref="EngineGameServer"/> handles auth (resolving the faction/commanded entity and checking
    /// ownership) and delegates the per-command mapping here, so the command surface grows in one
    /// isolated place. Adding a command is one DTO (in Pulsar4X.Api) + one entry in <see cref="_translators"/>
    /// and a <c>Translate*</c> method.
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

        // Fully qualified engine order type: it shares its name with the API DTO.
        private CommandResult TranslateRename(Entity faction, Entity commanded, GameCommand command)
        {
            var rename = (Pulsar4X.Api.RenameCommand)command;
            bool accepted = Pulsar4X.Names.RenameCommand.CreateRenameCommand(_game, faction, commanded, rename.NewName);
            return accepted
                ? CommandResult.Ok(Guid.NewGuid().ToString("N"))
                : CommandResult.Reject("Command rejected by engine validation.");
        }
    }
}
