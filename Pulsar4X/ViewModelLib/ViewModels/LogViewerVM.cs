using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel
{
    public class LogViewerVM : ViewModelBase
    {
        private GameVM _gameVM;

        public RangeEnabledObservableCollection<Event> EventsDict { get; } = new RangeEnabledObservableCollection<Event>();

        public AuthenticationToken Auth { get { return _gameVM.CurrentAuthToken; } }
        public Game Game { get { return _gameVM.Game; } }
        
        public LogViewerVM(GameVM gameVM)
        {
            _gameVM = gameVM;
            if (Game != null && Auth != null)
                EventsDict.AddRange(Game.EventLog.GetAllEvents(Auth));
            _gameVM.Game.GameLoop.GameGlobalDateChangedEvent += GameLoop_GameGlobalDateChangedEvent;
        }

        private void GameLoop_GameGlobalDateChangedEvent(DateTime newDate)
        {
            Refresh();
        }

        public void Refresh()
        {
            EventsDict.AddRange(Game.EventLog.GetNewEvents(Auth));
        }

    }
}
