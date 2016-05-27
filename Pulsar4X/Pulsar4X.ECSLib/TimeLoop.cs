using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Pulsar4X.ECSLib
{
    class TimeLoop
    {
        private Stopwatch _stopwatch = new Stopwatch();
        private Timer _timer = new Timer();

        //changes how often the tick happens
        public float TimeMultiplier
        {
            get {return _timeMultiplier;}
            set
            {
                _timeMultiplier = value;
                _timer.Interval = _tickInterval.Milliseconds * value;
            }
        } 
        private float _timeMultiplier = 1f;

        private TimeSpan _tickInterval = TimeSpan.FromSeconds(1);
        private TimeSpan _tickLenght = TimeSpan.FromSeconds(1);

        private bool _isProcessing = false;
        private bool _isOvertime = false;
        private Game _game;
        /// <summary>
        /// length of time it took to process the last DoProcess
        /// </summary>
        public TimeSpan LastProcessingTime { get; private set; } = TimeSpan.Zero;

        public TimeLoop(Game game)
        {
            _game = game;
            _timer.Interval = _tickInterval.Milliseconds;
            _timer.Enabled = true;
            _timer.Elapsed += Timer_Elapsed;
            
        }

        public void PauseTime()
        {
            _timer.Stop();
        }

        public void StartTime()
        {
            _timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!_isProcessing)
            {               
                DoProcessing(); //run DoProcessing if we're not already processing
            }
            else
            {
                _isOvertime = true; //if we're processing, then processing it taking longer than the sim speed
            }
        }

        void DoProcessing()
        {
            _isProcessing = true;
            _timer.Stop();
            _timer.Start(); //reset timer
            _stopwatch.Start(); //start the processor loop stopwatch
            _isOvertime = false;

            //do processors
            Parallel.ForEach<StarSystem>(_game.Systems.Values, item => SystemProcessing(item));
            //I think the above 'blocks' till all the tasks are done.

            LastProcessingTime = _stopwatch.Elapsed;
            _stopwatch.Reset();
            if (_isOvertime)
            {
                DoProcessing(); //if running overtime, DoProcessing wont be triggered by the event.
            }
            _isProcessing = false;
        }

        void SystemProcessing(object systemObj)
        {
            //check validity of commands etc.

            StarSystem system = systemObj as StarSystem;
            //the system may need to run several times for a wanted tickLength
            //the system itself needs to keep track of how much time it can process
            //should a system have a datetime? going to have to think about how to aproach this.
            //maybe somthing like this?
            TimeSpan systemElapsedTime = new TimeSpan();
            while (systemElapsedTime < _tickLenght)
            {
                
                //calculate max time the system can run/time to next interupt
                TimeSpan timeDelta = _tickLenght - systemElapsedTime; //math.min(tickLenght - systemElapsedTime, system.NextTickLen)
                //ShipMovementProcessor.Process(_game, system, timeDelta);
                //orbits 
                //jump ships out
                //econ & industry (as an interupt?)
                //sensors (as an interupt?)
                systemElapsedTime += timeDelta;

            }


        }


    }
}
