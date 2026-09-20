using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Engine;

public class PerformanceStopwatch
{
    Stopwatch _stopwatch = new Stopwatch();
    Stopwatch _fullIntervalStopwatch = new Stopwatch();
    Stopwatch _subIntervalStopwatch = new Stopwatch();
    string _currentId;
    SafeList<PerformanceData> _history = new ();
    PerformanceData _currentData = new PerformanceData();
    private readonly object _lock = new object();

    public struct PerformanceData
    {
        public double FullIntervalTime = 0;
        public SafeList<double> PartialIntervalTimes = new ();
        public SafeDictionary<string, (List<double> times, double sum)> TimesById = new ();

        public PerformanceData()
        {
        }
    }

    public void Start(string id)
    {
        _currentId = id;
        _stopwatch.Restart();
    }

    public void Stop(string id)
    {
        _stopwatch.Stop();
        var elapsed = _stopwatch.Elapsed.TotalMilliseconds;

        lock (_lock)
        {
            if(!_currentData.TimesById.ContainsKey(id))
            {
                var newList = new List<double> { elapsed };
                _currentData.TimesById.Add(id, (newList, elapsed));
            }
            else
            {
                _currentData.TimesById[id].times.Add(elapsed);
                double sum = _currentData.TimesById[id].sum + elapsed;
                (List<double> times, double sum) entry = (_currentData.TimesById[id].times, sum);
                _currentData.TimesById[id] = entry;
            }
        }
    }

    public void BeginInterval()
    {
        _currentData = new PerformanceData();
        _fullIntervalStopwatch.Restart();
    }

    public void EndInterval()
    {
        _fullIntervalStopwatch.Stop();
        _currentData.FullIntervalTime = _fullIntervalStopwatch.ElapsedMilliseconds;
        _history.Add(_currentData);
    }

    public void BeingSubInterval()
    {
        _subIntervalStopwatch.Restart();
    }

    public void EndSubInterval()
    {
        _subIntervalStopwatch.Stop();
        _currentData.PartialIntervalTimes.Add(_subIntervalStopwatch.ElapsedMilliseconds);
    }

    public IEnumerable GetHistory()
    {
        return _history;
    }

    public PerformanceData GetLatestEntry()
    {
        return _history.Last();
    }
}