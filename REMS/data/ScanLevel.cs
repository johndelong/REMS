using System.ComponentModel;
using System.Windows.Input;
using REMS.lib;
using System.Collections.ObjectModel;

namespace REMS.data
{
    public class ScanLevel : ObservableObject
    {
        int _zPos;
        string _scanState;
        string _pfState;

        public ScanLevel(int aZPos = 0, string aScanState = "Ready", string apfState = "NA")
        {
            _zPos = aZPos;
            _scanState = aScanState;
            _pfState = apfState;
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

        public string ScanState
        {
            get { return _scanState; }
            set
            {
                _scanState = value;
                RaisePropertyChanged("ScanState");
            }
        }

        public string pfState
        {
            get { return _pfState; }
            set
            {
                _pfState = value;
                RaisePropertyChanged("pfState");
            }
        }
    }
}
