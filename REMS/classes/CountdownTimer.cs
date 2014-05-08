using System;
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

        public void start()
        {
            _timer.Start();
        }

        public void stop()
        {
            _timer.Stop();
        }

        private void timer_tick(object sender, EventArgs e)
        {
            _label.Text = _time.ToString("c");

            if (_time == TimeSpan.Zero)
            {
                _timer.Stop();
            }
            else
            {
                _time = _time.Add(TimeSpan.FromSeconds(-1));
            }
        }        
    }
}
