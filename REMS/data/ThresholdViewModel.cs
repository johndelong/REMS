using System.ComponentModel;
using System.Windows.Input;
using REMS.lib;
using System.Collections.ObjectModel;

namespace REMS.data
{
    public class ThresholdViewModel : ObservableObject
    {
        string _name;
        ObservableCollection<ThresholdLimitViewModel> _limits;

        string _state;

        public ThresholdViewModel()
        {
            //_threshold = new Threshold { Name = "Empty", Limits = new ObservableCollection<ThresholdLimitViewModel>() };
            _state = "Pending";
            _name = "Empty";
            _limits = new ObservableCollection<ThresholdLimitViewModel>();
        }

        /*public Threshold Threshold
        {
            get { return _threshold; }
            set { _threshold = value; }
        }*/

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                RaisePropertyChanged("Name");
            }
        }

        public ObservableCollection<ThresholdLimitViewModel> Limits
        {
            get { return _limits; }
            set
            {
                _limits = value;
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
