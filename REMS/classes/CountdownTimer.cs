﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Diagnostics;

namespace REMS.classes
{
    public class CountdownTimer
    {
        private int _scanPoints = 0;
        private TextBlock _label;

        DispatcherTimer _timer = new DispatcherTimer();
        Stopwatch _stopWatch = new Stopwatch();
        TimeSpan _time;
        private int _pointsScanned = 0;
        private TimeSpan _averageTime;

        public CountdownTimer(TextBlock aLabel, int aScanPoints)
        {
            _label = aLabel;
            _scanPoints = aScanPoints;
            _time = TimeSpan.FromSeconds(_scanPoints);
            _timer.Tick += new EventHandler(timer_tick);
            _timer.Interval = new TimeSpan(0, 0, 1);
        }

        public void Start()
        {
            _timer.Start();
            _stopWatch.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            _stopWatch.Stop();
        }

        public void pointScanned()
        {
            _pointsScanned++;

            if (_stopWatch.IsRunning)
            {
                _averageTime = new TimeSpan(_stopWatch.ElapsedTicks / _pointsScanned);
            }
        }

        private void timer_tick(object sender, EventArgs e)
        {
            long lTicksRemaining = (_scanPoints - _pointsScanned) * _averageTime.Ticks;
            Console.WriteLine("Points Remaining: " + (_scanPoints - _pointsScanned) + " | Average time (ms): " + _averageTime.Milliseconds);
            _time = new TimeSpan(lTicksRemaining);

            _label.Text = _time.ToString(@"dd\:hh\:mm\.ss"); 
        }
    }
}
