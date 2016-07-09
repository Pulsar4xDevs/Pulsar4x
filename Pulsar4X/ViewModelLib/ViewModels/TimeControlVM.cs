using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using System.Windows.Input;
using System.Threading;
using System.ComponentModel;

namespace Pulsar4X.ViewModel
{
    public class TimeControlVM : ViewModelBase
    {
        
        private GameVM _gameVM;
        private TimeLoop _timeloop;
        public bool IsPaused { get; set; } = true;

        public int TickLength
        {
            get { return _timeloop?.Ticklength.Seconds ?? 1; }
            set { _timeloop.Ticklength = TimeSpan.FromSeconds(value); OnPropertyChanged(); }
        }

        public float TickMultiplier
        {
            get { return _timeloop?.TimeMultiplier ?? 1; }
            set { _timeloop.TimeMultiplier = value; OnPropertyChanged(); }
        }
        public double TickFreq
        {
            get { return _timeloop?.TickFrequency.TotalMilliseconds ?? 5; }
            set { _timeloop.TickFrequency = TimeSpan.FromMilliseconds(value); OnPropertyChanged(); }
        }

        private SystemSubPulses SystemSubPulse { get { return _gameVM?.StarSystemViewModel?.StarSystems.SelectedKey.SystemSubpulses; } }
        public string CurrentGameDate { get { return _timeloop?.GameGlobalDateTime.ToString(); } }

        public string LastTickLen { get { return _timeloop?.LastProcessingTime.TotalMilliseconds.ToString(); } }

        public TimeControlVM()
        {

            
        }

        public void Initialise(GameVM gameVM)
        {
            _gameVM = gameVM;
            _timeloop = gameVM.Game.GameLoop;

            OnPropertyChanged(nameof(TickLength));
            OnPropertyChanged(nameof(TickMultiplier));
            OnPropertyChanged(nameof(TickFreq));
            OnPropertyChanged(nameof(CurrentGameDate));

            _timeloop.GameGlobalDateChangedEvent += OnTimeDateChange;

        }


        public ICommand PausePlayCMD { get { return new RelayCommand<object>(obj => PausePlay()); } }

        private void PausePlay()
        {
            if (IsPaused)
            {
                _timeloop.StartTime();
                IsPaused = false;
            }
            else
            {
                _timeloop.PauseTime();
                IsPaused = true;
            }
                
        }

        private void OnTimeDateChange(DateTime newDate)
        {                        
            OnPropertyChanged(nameof(CurrentGameDate));
            OnPropertyChanged(nameof(LastTickLen));
        }
    }
}
