using System.ComponentModel;
using System.Windows.Input;
using REMS.lib;
using System.Collections.ObjectModel;

namespace REMS.data
{
    public class ThresholdViewModel : ObservableObject
    {
        Threshold _threshold;
        string _state;

        public ThresholdViewModel()
        {
            _threshold = new Threshold { Name = "Empty", Limits = new ObservableCollection<ThresholdLimitViewModel>() };
            _state = "Pending";
        }

        public Threshold Threshold
        {
            get { return _threshold; }
            set { _threshold = value; }
        }

        public string Name
        {
            get { return Threshold.Name; }
            set
            {
                Threshold.Name = value;
                RaisePropertyChanged("Name");
            }
        }

        public ObservableCollection<ThresholdLimitViewModel> Limits
        {
            get { return Threshold.Limits; }
            set
            {
                Threshold.Limits = value;
                RaisePropertyChanged("Limits");
            }
        }

        public string State
        {
            get { return _state; }
            set
            {
                _state = value;
                RaisePropertyChanged("State");
            }
        }
    }
}
