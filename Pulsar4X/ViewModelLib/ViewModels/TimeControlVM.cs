using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using System.Windows.Input;

namespace Pulsar4X.ViewModel
{
    public class TimeControlVM : ViewModelBase
    {

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
        public int TickFreq
        {
            get { return _timeloop?.TickFrequency.Seconds ?? 5; }
            set { _timeloop.TickFrequency = TimeSpan.FromSeconds(value); OnPropertyChanged(); }
        }

        public TimeControlVM()
        {         
        }

        public void Initialise(TimeLoop gameTimeLoop)
        {
            _timeloop = gameTimeLoop;
            OnPropertyChanged(nameof(TickLength));
            OnPropertyChanged(nameof(TickMultiplier));
            OnPropertyChanged(nameof(TickFreq));
        }

        public ICommand PausePlayCMD { get { return new RelayCommand<object>(obj => PausePlay()); } }

        private void PausePlay()
        {
            if (IsPaused)
                _timeloop.StartTime();
            else 
                _timeloop.PauseTime();
        }

    }
}
