using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Pulsar4X.ECSLib
{
    public class EventLog : ISerializable
    {
        private readonly Game _game;
        private readonly DateTime _loadTime;
        private readonly List<Event> _events;
        private readonly Dictionary<Player, List<Event>> _newEvents;

        private Player SpaceMaster => _game.SpaceMaster;

        internal EventLog()
        {
            _newEvents = new Dictionary<Player, List<Event>>();
        }

        internal EventLog(Game game) : this()
        {
            _loadTime = game.CurrentDateTime;
            _game = game;
            _events = new List<Event>();
            
            _newEvents.Add(SpaceMaster, new List<Event>());
            foreach (Player player in _game.Players)
            {
                _newEvents.Add(player, new List<Event>());
            }
        }

        public List<Event> GetAllEvents(AuthenticationToken authToken)
        {
            Player player = _game.GetPlayerForToken(authToken);

            if (player == null)
            {
                return new List<Event>();
            }

            var retVal = new List<Event>();

            foreach (Event logEvent in _events)
            {
                if (logEvent.ConcernedPlayers.Contains(player.ID))
                {
                    retVal.Add(logEvent);
                }
            }
            return retVal;
        }

        public List<Event> GetNewEvents(AuthenticationToken authToken)
        {
            Player player = _game.GetPlayerForToken(authToken);

            if (player == null)
            {
                return new List<Event>();
            }

            List<Event> retVal = _newEvents[player];
            if (retVal.Count > 0)
            {
                _newEvents[player] = new List<Event>();
            }
            return retVal;
        }

        internal void AddEvent(Event @event)
        {
            @event.ConcernedPlayers.Add(_game.SpaceMaster.ID);
            _newEvents[SpaceMaster].Add(@event);

            foreach (Player player in _game.Players.Where(player => IsPlayerConcerned(@event, player)))
            {
                @event.ConcernedPlayers.Add(player.ID);
                _newEvents[player].Add(@event);
            }
        }

        private bool IsPlayerConcerned([NotNull] Event @event, [NotNull] Player player)
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (player == _game.SpaceMaster)
            {
                return true;
            }

            var ownedDB = @event.Entity?.GetDataBlob<OwnedDB>();
            if (ownedDB != null)
            {
                foreach (KeyValuePair<Entity, AccessRole> keyValuePair in player.AccessRoles)
                {
                    Entity arFaction = keyValuePair.Key;
                    AccessRole arRole = keyValuePair.Value;
                    if (ownedDB.Faction == arFaction)
                    {
                        if (@event.Entity.HasDataBlob<ShipInfoDB>() && (arRole & AccessRole.UnitVision) == AccessRole.UnitVision)
                        {
                            return true;
                        }
                        if (@event.Entity.HasDataBlob<ColonyInfoDB>() && (arRole & AccessRole.ColonyVision) == AccessRole.ColonyVision)
                        {
                            return true;
                        }
                        return false;
                    }
                }
                return false;
            }

            if (@event.SystemGuid != Guid.Empty)
            {
                foreach (KeyValuePair<Entity, AccessRole> keyValuePair in player.AccessRoles)
                {
                    Entity arFaction = keyValuePair.Key;
                    AccessRole arRole = keyValuePair.Value;
                    var factionInfo = arFaction.GetDataBlob<FactionInfoDB>();

                    if ((arRole & AccessRole.SystemKnowledge) == 0)
                    {
                        continue;
                    }

                    foreach (Guid knownSystem in factionInfo.KnownSystems)
                    {
                        if (knownSystem != @event.SystemGuid)
                        {
                            continue;
                        }

                        List<Entity> ownedEntities = _game.StarSystems[knownSystem].SystemManager.GetAllEntitiesWithDataBlob<OwnedDB>();
                        if (ownedEntities.Any(ownedEntity => ownedEntity.GetDataBlob<OwnedDB>().Faction == arFaction))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            if (@event.Faction != null)
            {
                foreach (KeyValuePair<Entity, AccessRole> keyValuePair in player.AccessRoles)
                {
                    Entity arFaction = keyValuePair.Key;
                    AccessRole arRole = keyValuePair.Value;

                    if (arFaction != @event.Faction)
                    {
                        continue;
                    }

                    return (arRole & AccessRole.FactionEvents) != 0;
                }
            }
            return false;
        }

        #region ISerializable interface

        public EventLog(SerializationInfo info, StreamingContext context) : this((Game)context.Context)
        {
            _events = (List<Event>)info.GetValue("Events", typeof(List<Event>));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            List<Event> eventList = _events.Where(logEvent => logEvent.Time > _loadTime).ToList();
            info.AddValue("Events", eventList);
        }

        #endregion
    }
}
