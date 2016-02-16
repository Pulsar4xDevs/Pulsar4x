using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class EventLog
    {
        private Game _game;
        private DateTime _loadTime;
        private List<LogEvent> _events;

        private Dictionary<Player, List<LogEvent>> _newEvents;

        [JsonConstructor]
        private EventLog()
        {
            // JsonConstructor
            _newEvents = new Dictionary<Player, List<LogEvent>>();
        }

        internal EventLog(Game game) : this()
        {
            _loadTime = game.CurrentDateTime;
            _game = game;
            _events = new List<LogEvent>();


        }

        public List<LogEvent> GetEvents(AuthenticationToken authToken, Entity faction)
        {
            Player player = _game.GetPlayerForToken(authToken);

            if (player?.AccessRoles == null)
            {
                return new List<LogEvent>();
            }

            return new List<LogEvent>();
        }

    }
}
