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
            _label.Text = _time.ToString("c");
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void pointScanned()
        {
            if (_stopWatch.IsRunning)
            {
                _stopWatch.Stop();

                _averageTime = new TimeSpan((long)(_averageTime.Ticks * 0.8) + (long)(_stopWatch.ElapsedTicks * 0.2));
            }
            else
            {
                _averageTime = new TimeSpan(_stopWatch.ElapsedTicks);
            }
            _stopWatch.Reset();
            _stopWatch.Start();
            _pointsScanned++;
        }

        private void timer_tick(object sender, EventArgs e)
        {
            _label.Text = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", _time.Days, _time.Hours, _time.Minutes, _time.Seconds);

            if (_time == TimeSpan.Zero)
            {
                Stop();
            }
            else
            {
                long lTicksRemaining = (_scanPoints - _pointsScanned) * _averageTime.Ticks;
                Console.WriteLine("Average time: " + _averageTime.Seconds);
                _time = new TimeSpan(lTicksRemaining);
            }
        }
    }
}
