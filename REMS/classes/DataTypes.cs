using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace REMS
{
    public class AnalyzeSynopsisData 
    {
        public string threshold { get; set; }
        public string state { get; set; }
    }

    public class Threshold
    {
        public string name { get; set; }
        public List<ThresholdData> data { get; set; }
    }

    public class ThresholdData
    {
        public string frequency { get; set; }
        public string amplitude { get; set; }
    }
}
