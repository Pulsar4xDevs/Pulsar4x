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

    public struct PerformanceData
    {
        public double FullIntervalTime = 0;
        public List<double> PartialIntervalTimes = new ();
        public Dictionary<string, (List<double> times, double sum)> TimesById = new ();

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

        if(!_currentData.TimesById.ContainsKey(id))
        {
            var newList = new List<double> { _stopwatch.Elapsed.TotalMilliseconds };
            var sum = _stopwatch.Elapsed.TotalMilliseconds;
            _currentData.TimesById.Add(id, (newList, sum));
        }
        else
        {
            _currentData.TimesById[id].times.Add(_stopwatch.Elapsed.TotalMilliseconds);
            double sum = _currentData.TimesById[id].sum + _stopwatch.Elapsed.TotalMilliseconds;
            (List<double> times, double sum) entry = (_currentData.TimesById[id].times, sum);
            _currentData.TimesById[id] = entry;
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