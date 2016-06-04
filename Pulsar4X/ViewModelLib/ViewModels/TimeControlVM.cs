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
            get { return _timeloop.Ticklength.Seconds; } 
            set { _timeloop.Ticklength = TimeSpan.FromSeconds(value); }
        }

        public float TickMultiplier
        {
            get { return _timeloop.TimeMultiplier; }
            set { _timeloop.TimeMultiplier = value; }
        }
        public int TickFreq
        {
            get { return _timeloop.TickFrequency.Seconds; }
            set { _timeloop.TickFrequency = TimeSpan.FromSeconds(value); }
        }

        public TimeControlVM(TimeLoop gameTimeLoop)
        {
            _timeloop = gameTimeLoop;
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
