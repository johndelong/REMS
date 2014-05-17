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

    public class ThresholdList : ObservableCollection<Threshold>
    {
        public ThresholdList() : base()
        {
            Threshold someData = new Threshold();
            someData.name = "Test";
            someData.data.Add(new ThresholdDetails("123", "456"));
            this.Add(someData);
        }
    }

    public class Threshold
    {
        public Threshold()
        {
            this.data = new List<ThresholdDetails>();
        }

        public string name { get; set; }
        public List<ThresholdDetails> data { get; set; }
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
