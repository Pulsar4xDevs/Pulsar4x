using System;

using Pulsar4X.ECSLib;
using System.Windows.Input;

namespace Pulsar4X.ECSLib
{
    public class TimeControlVM : ViewModelBase
    {
        
        private GameVM _gameVM;
        private TimeLoop _timeloop;
        public bool IsPaused { get; set; } = true;
        public int TickLength
        {
            get { return (int)(_timeloop?.Ticklength.TotalSeconds ?? 3600); }
            set { _timeloop.Ticklength = TimeSpan.FromSeconds(value); OnPropertyChanged(); }
        }
        public float TickMultiplier
        {
            get { return _timeloop?.TimeMultiplier ?? 1; }
            set { _timeloop.TimeMultiplier = value; OnPropertyChanged(); }
        }
        public double TickFreq
        {
            get { return _timeloop?.TickFrequency.TotalMilliseconds ?? 250; }
            set { _timeloop.TickFrequency = TimeSpan.FromMilliseconds(value); OnPropertyChanged(); }
        }
        private ManagerSubPulse SystemSubPulse { get { return _gameVM?.StarSystemSelectionViewModel?.StarSystems.SelectedKey.SystemManager.ManagerSubpulses; } }
        public string CurrentGameDate { get { return _timeloop?.GameGlobalDateTime.ToString(); } }
        public string LastTickLen { get { return _timeloop?.LastProcessingTime.TotalMilliseconds.ToString(); } }
        private TimeSpan _lastFrameLen;
        public TimeSpan LastFrameLen
        {
            get { return _lastFrameLen; }
            set { _lastFrameLen = value; OnPropertyChanged(); }
        }

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
            if (_timeloop == null)
                return;
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
            OnPropertyChanged(nameof(IsPaused));

        }

        public ICommand TimeStepCMD { get { return new RelayCommand<object>(obj => TimeStep()); } }

        private void TimeStep()
        {
            if (_timeloop == null)
                return;
            _timeloop.TimeStep();
        }

        private void OnTimeDateChange(DateTime newDate)
        {                        
            OnPropertyChanged(nameof(CurrentGameDate));
            OnPropertyChanged(nameof(LastTickLen));
        }
    }
}
