using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ECSLib
{
    public class LogViewerVM : ViewModelBase
    {
        private GameVM _gameVM;

        public RangeEnabledObservableCollection<EventVM> EventsDict { get; } = new RangeEnabledObservableCollection<EventVM>();

        public RangeEnabledObservableCollection<EventTypeBoolPair> EventTypes { get; } = new RangeEnabledObservableCollection<EventTypeBoolPair>();
        public AuthenticationToken Auth { get { return _gameVM.CurrentAuthToken; } }
        public Game Game { get { return _gameVM.Game; } }
        
        public LogViewerVM(GameVM gameVM)
        {
            _gameVM = gameVM;
            if (Game != null && Auth != null)
                foreach (var item in StaticRefLib.EventLog.GetAllEvents(Auth))
                {
                    EventsDict.Add(new EventVM(item, _gameVM.CurrentFaction, _gameVM));
                }
                

            foreach (var kvp in gameVM.CurrentPlayer.HaltsOnEvent)
            {
                EventTypes.Add(new EventTypeBoolPair(gameVM.CurrentPlayer, kvp.Key));
            }
            

            _gameVM.Game.GamePulse.GameGlobalDateChangedEvent += GameLoop_GameGlobalDateChangedEvent;
        }

        private void GameLoop_GameGlobalDateChangedEvent(DateTime newDate)
        {
            Refresh();
        }

        public void Refresh()
        {
            foreach (var item in StaticRefLib.EventLog.GetNewEvents(Auth))
            {
                EventsDict.Add(new EventVM(item, _gameVM.CurrentFaction, _gameVM));
            }
        }

        public class EventTypeBoolPair : ViewModelBase
        {
            private Player _player;
            public EventType EventType { get; private set; }

            public bool? IsHalting
            {
                get { return _player.HaltsOnEvent[EventType]; }
                set {
                    if (value == null)
                        _player.HaltsOnEvent[EventType] = false;
                    else
                        _player.HaltsOnEvent[EventType] = (bool)value;
                    OnPropertyChanged();
                }
            }

            public EventTypeBoolPair(Player player, EventType eventType)
            {
                _player = player;
                EventType = eventType;
            }

        }
    }

    //requred due to how events are currently stored ingame. ie they don't have a name string, and the name string will be different depending on 
    //which faction is looking at it. ditto for system name. 
    public class EventVM
    {
        private Event _evnt { get;  set; } 
        private Entity _faction { get; set; }
        private GameVM _gameVM;

        public DateTime Time { get { return  _evnt.Time; } }

        public string Message { get { return _evnt.Message; } }

        public string Faction { get { return _evnt.Faction?.GetDataBlob<NameDB>()?.GetName(_faction) ?? "";  } }

        public string SystemName { get
            {
                if (_evnt.SystemGuid != Guid.Empty)
                    return _gameVM.Game.GetSystem(_gameVM.CurrentAuthToken, _evnt.SystemGuid).NameDB.GetName(_faction);
                else
                    return "";
            }}

        public string EntityName { get { return _evnt.Entity?.GetDataBlob<NameDB>()?.GetName(_faction) ?? ""; } }

        public string EventTypeSsring { get { return Enum.GetName(typeof(EventType), _evnt.EventType); } }

        public EventVM(Event evnt, Entity Faction, GameVM gameVM)
        {
            _evnt = evnt;
            _faction = Faction;
            _gameVM = gameVM;
        }
    }
}
