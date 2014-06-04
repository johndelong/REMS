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
        string _frequency;
        string _amplitude;

        public ThresholdLimitViewModel(string aFrequency = "0", string aAmplitude = "0")
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

        public string Frequency
        {
            get { return _frequency; }
            set
            {
                _frequency = value;
                RaisePropertyChanged("Frequency");
            }
        }

        public string Amplitude
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
            return Convert.ToInt32(a.Frequency).CompareTo(Convert.ToInt32(b.Frequency));
        }
    }
}
