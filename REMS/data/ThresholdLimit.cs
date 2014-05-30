

namespace REMS.data
{
    public class ThresholdLimit
    {
        string _frequency;
        string _amplitude;

        public string Frequency
        {
            get { return _frequency; }
            set { _frequency = value; }
        }

        public string Amplitude
        {
            get { return _amplitude; }
            set { _amplitude = value; }
        }
    }
}
