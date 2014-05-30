using System.Collections.ObjectModel;

namespace REMS.data
{
    public class Threshold
    {
        string _name;
        ObservableCollection<ThresholdLimitViewModel> _limits;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public ObservableCollection<ThresholdLimitViewModel> Limits
        {
            get { return _limits; }
            set { _limits = value; }
        }
    }
}
