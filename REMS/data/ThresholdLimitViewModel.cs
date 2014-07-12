using System.ComponentModel;
using System.Windows.Input;
using REMS.lib;
using System.Collections.ObjectModel;
using System;

namespace REMS.data
{
    public class ThresholdLimitViewModel : ObservableObject, IComparable
    {
        //ThresholdLimit _limit;
        double _frequency;
        double _amplitude;

        public ThresholdLimitViewModel(double aFrequency = 0, double aAmplitude = 0)
        {
            //_limit = new ThresholdLimit { Frequency = aFrequency, Amplitude = aAmplitude };

            _frequency = aFrequency;
            _amplitude = aAmplitude;
        }

        /*public ThresholdLimit Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }*/

        public double Frequency
        {
            get { return _frequency; }
            set
            {
                _frequency = value;
                RaisePropertyChanged("Frequency");
            }
        }

        public double Amplitude
        {
            get { return _amplitude; }
            set
            {
                _amplitude = value;
                RaisePropertyChanged("Amplitude");
            }
        }

        public int CompareTo(object o)
        {
            ThresholdLimitViewModel a = this;
            ThresholdLimitViewModel b = (ThresholdLimitViewModel)o;
            return a.Frequency.CompareTo(b.Frequency);
        }
    }
}
