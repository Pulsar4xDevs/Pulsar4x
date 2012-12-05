using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Pulsar4X.Helpers
{
    /// <summary> Timer, Keeps time. </summary>
    class Timer
    {
        Dictionary<string, Stopwatch> m_dicTimers;
        Dictionary<string, double> m_dicPrevTimes;

        public Timer()
        {
            m_dicTimers = new Dictionary<string, Stopwatch>();
            m_dicPrevTimes = new Dictionary<string, double>();
        }

        /// <summary>   Creates a new timer with the specified name. </summary>
        /// <param name="a_szName"> Name of the Timer. </param>
        public void NewTimer(string a_szName)
        {
            m_dicTimers[a_szName] = new Stopwatch();
            m_dicPrevTimes[a_szName] = 0.0;
        }

        /// <summary>   Deletes the specified timer. </summary>
        /// <param name="a_szName"> Name of the Timer. </param>
        public void DeleteTimer(string a_szName)
        {
            m_dicTimers.Remove(a_szName);
            m_dicPrevTimes.Remove(a_szName);
        }

        /// <summary> Starts the Specified Timer </summary>
        /// <param name="a_szName"> Name of the Timer. </param>
        public void Start(string a_szName)
        {
            Stopwatch oTimer;
            if (m_dicTimers.TryGetValue(a_szName, out oTimer))
            {
                oTimer.Start();
            }
        }

        /// <summary> Restarts the Specified Timer, Clearing all data in the process. </summary>
        /// <param name="a_szName"> Name of the Timer. </param>
        public void Restart(string a_szName)
        {
            Stopwatch oTimer;
            if (m_dicTimers.TryGetValue(a_szName, out oTimer))
            {
                oTimer.Restart();
                m_dicPrevTimes[a_szName] = 0.0; // reset prev time to make sure getDelta returns corectly.
            }
        }

        /// <summary> Stops the Specified Timer </summary>
        /// <param name="a_szName"> Name of the Timer. </param>
        public void Stop(string a_szName)
        {
            Stopwatch oTimer;
            if (m_dicTimers.TryGetValue(a_szName, out oTimer))
            {
                oTimer.Stop();
            }
        }

        /// <summary> Stops the Specified Timer, Clearing all data in the process. </summary>
        /// <param name="a_szName"> Name of the Timer. </param>
        public void StopAndClear(string a_szName)
        {
            Stopwatch oTimer;
            if (m_dicTimers.TryGetValue(a_szName, out oTimer))
            {
                oTimer.Stop();
                oTimer.Reset();
                m_dicPrevTimes[a_szName] = 0.0; // reset prev time to make sure getDelta returns corectly.
            }
        }

        /// <summary>   Works out the Delta timeof the specified timer. i.e. how long it has been since the last call to GetDelta. </summary>
        /// <param name="a_szName"> Name of the Timer. </param>
        /// <returns>   The delta time, in secods. </returns>
        public double GetDelta(string a_szName)
        {
            Stopwatch oTimer;
            if (m_dicTimers.TryGetValue(a_szName, out oTimer))
            {
                double dCurrTime = oTimer.ElapsedMilliseconds;
                double dPrevTime = 0.0;
                if (m_dicPrevTimes.TryGetValue(a_szName, out dPrevTime))
                {
                    double delta = (dCurrTime - dPrevTime) / 1000.0; // return as fraction of a second.
                    // set previous time!
                    m_dicPrevTimes[a_szName] = dCurrTime;
                    return delta;
                }

                // set previous time!
                m_dicPrevTimes[a_szName] = dCurrTime;
                return dCurrTime;  // if we get here then there was no previous time so delta =- currtime.
            }

            return 0.0;
        }

        /// <summary> Gets the specified timer as a stopwatch. </summary>
        /// <param name="a_szName"> Name of the Timer. </param>
        /// <returns> The Stopwatch class instacne used for this timer. </returns>
        public Stopwatch GetTimer(string a_szName)
        {
            Stopwatch oTimer;
            if (m_dicTimers.TryGetValue(a_szName, out oTimer))
            {
                return oTimer;
            }

            return null;
        }

    }
}
