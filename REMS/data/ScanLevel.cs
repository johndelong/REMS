using System.ComponentModel;
using System.Windows.Input;
using REMS.lib;
using System.Collections.ObjectModel;

namespace REMS.data
{
    public class ScanLevel : ObservableObject
    {
        int _zPos;
        string _state;

        public ScanLevel(int aZPos = 0, string aState = "Ready")
        {
            _zPos = aZPos;
            _state = aState;
        }

        public int ZPos
        {
            get { return _zPos; }
            set
            {
                _zPos = value;
                RaisePropertyChanged("ZPos");
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
