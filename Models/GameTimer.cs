using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpDX11GameByWinbringer.Models
{
    class GameTimer
    {
        [DllImport("kernel32")]
        private static extern bool QueryPerformanceFrequency(out long perfFreq);

        [DllImport("kernel32")]
        private static extern bool QueryPerformanceCounter(out long perfCount);

        private double _secondsPerCount = 0;

        private long _baseTime = 0;
        private long _pausedTime = 0;
        private long _stopTime = 0;
        private long _currentTime = 0;
        private long _previousTime = 0;

        public double DeltaTime { private set; get; }

        public bool Stopped { private set; get; }

        public GameTimer()
        {
            long countsPerSec;
            QueryPerformanceFrequency(out countsPerSec);
            _secondsPerCount = 1.0 / countsPerSec;
        }

        public void Reset()
        {
            long currentTime;
            QueryPerformanceCounter(out currentTime);
            _baseTime = _previousTime = _stopTime = currentTime;
            Stopped = false;
        }

        public void Start()
        {
            long startTime;
            QueryPerformanceCounter(out startTime);

            //Resuming
            if (Stopped)
            {
                _pausedTime += (startTime - _stopTime);
                _previousTime = startTime;
                _stopTime = 0;
                Stopped = false;
            }
        }

        public void Stop()
        {
            if (Stopped)
            {
                return;
            }

            QueryPerformanceCounter(out _stopTime);
            Stopped = true;
        }

        public void Tick()
        {
            //If the timer isn't running break out early
            if (Stopped)
            {
                DeltaTime = 0;
                return;
            }

            //Grab the current time for this frame
            QueryPerformanceCounter(out _currentTime);

            //Ensure DeltaTime is never negative
            DeltaTime = Math.Max(0, (_currentTime - _previousTime) * _secondsPerCount);

            //prepare for the next frame
            _previousTime = _currentTime;
        }

        public double CalculateGameTime()
        {
            if (Stopped)
            {
                return (_stopTime * _baseTime) * _secondsPerCount;
            }
            else
            {
                return ((_currentTime - _pausedTime) - _baseTime) * _secondsPerCount;
            }
        }
    }
}
