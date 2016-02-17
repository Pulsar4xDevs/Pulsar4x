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

        private readonly List<LogEvent> _events;

        private readonly Dictionary<Player, List<LogEvent>> _newEvents;

        private Player SpaceMaster => _game.SpaceMaster;

        internal EventLog()
        {
            _newEvents = new Dictionary<Player, List<LogEvent>>();
        }

        internal EventLog(Game game) : this()
        {
            _loadTime = game.CurrentDateTime;
            _game = game;
            _events = new List<LogEvent>();
            
            _newEvents.Add(SpaceMaster, new List<LogEvent>());
            foreach (Player player in _game.Players)
            {
                _newEvents.Add(player, new List<LogEvent>());
            }
        }

        public List<LogEvent> GetAllEvents(AuthenticationToken authToken)
        {
            Player player = _game.GetPlayerForToken(authToken);

            if (player == null)
            {
                return new List<LogEvent>();
            }

            var retVal = new List<LogEvent>();

            foreach (LogEvent logEvent in _events)
            {
                if (logEvent.ConcernedPlayers.Contains(player.ID))
                {
                    retVal.Add(logEvent);
                }
            }
            return retVal;
        }

        public List<LogEvent> GetNewEvents(AuthenticationToken authToken)
        {
            Player player = _game.GetPlayerForToken(authToken);

            if (player == null)
            {
                return new List<LogEvent>();
            }

            List<LogEvent> retVal = _newEvents[player];
            if (retVal.Count > 0)
            {
                _newEvents[player] = new List<LogEvent>();
            }
            return retVal;
        }

        internal void AddEvent(LogEvent logEvent)
        {
            logEvent.ConcernedPlayers.Add(_game.SpaceMaster.ID);
            _newEvents[SpaceMaster].Add(logEvent);

            foreach (Player player in _game.Players.Where(player => IsPlayerConcerned(logEvent, player)))
            {
                logEvent.ConcernedPlayers.Add(player.ID);
                _newEvents[player].Add(logEvent);
            }
        }

        private bool IsPlayerConcerned([NotNull] LogEvent logEvent, [NotNull] Player player)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (player == _game.SpaceMaster)
            {
                return true;
            }

            var ownedDB = logEvent.Entity?.GetDataBlob<OwnedDB>();
            if (ownedDB != null)
            {
                foreach (KeyValuePair<Entity, AccessRole> keyValuePair in player.AccessRoles)
                {
                    Entity arFaction = keyValuePair.Key;
                    AccessRole arRole = keyValuePair.Value;
                    if (ownedDB.Faction == arFaction)
                    {
                        if (logEvent.Entity.HasDataBlob<ShipInfoDB>() && (arRole & AccessRole.UnitVision) == AccessRole.UnitVision)
                        {
                            return true;
                        }
                        if (logEvent.Entity.HasDataBlob<ColonyInfoDB>() && (arRole & AccessRole.ColonyVision) == AccessRole.ColonyVision)
                        {
                            return true;
                        }
                        return false;
                    }
                }
                return false;
            }

            if (logEvent.SystemGuid != Guid.Empty)
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
                        if (knownSystem != logEvent.SystemGuid)
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

            if (logEvent.Faction != null)
            {
                foreach (KeyValuePair<Entity, AccessRole> keyValuePair in player.AccessRoles)
                {
                    Entity arFaction = keyValuePair.Key;
                    AccessRole arRole = keyValuePair.Value;

                    if (arFaction != logEvent.Faction)
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
            _events = (List<LogEvent>)info.GetValue("Events", typeof(List<LogEvent>));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            List<LogEvent> eventList = _events.Where(logEvent => logEvent.Time > _loadTime).ToList();
            info.AddValue("Events", eventList);
        }

        #endregion
    }
}
