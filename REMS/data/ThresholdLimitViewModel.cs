using System.ComponentModel;
using System.Windows.Input;
using REMS.lib;
using System.Collections.ObjectModel;

namespace REMS.data
{
    public class ThresholdLimitViewModel : ObservableObject
    {
        ThresholdLimit _limit;

        public ThresholdLimitViewModel(string aFrequency = "NA", string aAmplitude = "NA")
        {
            _limit = new ThresholdLimit { Frequency = aFrequency, Amplitude = aAmplitude };
        }

        public ThresholdLimit Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        public string Frequency
        {
            get { return Limit.Frequency; }
            set
            {
                Limit.Frequency = value;
                RaisePropertyChanged("Frequency");
            }
        }

        public string Amplitude
        {
            get { return Limit.Amplitude; }
            set
            {
                Limit.Amplitude = value;
                RaisePropertyChanged("Amplitude");
            }
        }
    }
}
