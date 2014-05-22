using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace REMS
{
    public class AnalyzeSynopsisData 
    {
        public string threshold { get; set; }
        public string state { get; set; }
    }

    public class Threshold
    {
        public Threshold()
        {
            this.data = new ObservableCollection<ThresholdDetails>();
        }

        public string name { get; set; }
        public ObservableCollection<ThresholdDetails> data { get; set; }
    }

    public class ThresholdDetails
    {
        public ThresholdDetails(string freq, string amp)
        {
            frequency = freq;
            amplitude = amp;
        }

        public ThresholdDetails()
        {
            // do nothing
        }

        public string frequency { get; set; }
        public string amplitude { get; set; }
    }
}
