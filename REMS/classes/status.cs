using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace REMS.classes
{
    class status
    {
        /*
         * Initial,
            Ready,
            Stopped,
            Scanning,
            Done
         */

        public static readonly string Initial = "Click and drag to determine scan area";
        public static readonly string Ready = "Ready to start scanning";
        public static readonly string Stopped = "Scanning paused";
        public static readonly string Scanning = "Scanning...";
        public static readonly string Done = "Scan has finished";
    }
}
