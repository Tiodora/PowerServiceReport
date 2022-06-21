using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Axpo;

namespace PowerServiceReports
{
    public class PowerServiceReportsConfig
    {
        public List<PowerTrade> trades { get; set; }
        public Dictionary<string, double> tradesAggregate { get; set; } = new Dictionary<string, double>();
        public int? interval { get; set; } = null;
        public string folderPath { get; set; } = string.Empty;
    }
}
