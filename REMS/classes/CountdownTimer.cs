﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Controls;

namespace REMS.classes
{
    public class CountdownTimer
    {
        private int _scanPoints = 0;
        private TextBlock _label;
        private int _averageScanTime;

        DispatcherTimer _timer = new DispatcherTimer();
        TimeSpan _time;
        //int numOfScanPoints = 0;
        //int pointsScanned = 0;
        
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
            Enabled = true;
        }

        public void Stop()
        {
            _timer.Stop();
            Enabled = false;
        }

        private void timer_tick(object sender, EventArgs e)
        {
            _label.Text = _time.ToString("c");

            if (_time == TimeSpan.Zero)
            {
                Stop();
            }
            else
            {
                _time = _time.Add(TimeSpan.FromSeconds(-1));
            }
        }

        public bool Enabled { get; set; }
    }
}
