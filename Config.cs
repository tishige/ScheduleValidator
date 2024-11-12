using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleValidator
{

    public class GcSettings
    {
        public string GcProfileFileName { get; set; }
        public int PageSize { get; set; }
        public int MaxRetryTimeSec { get; set; }
        public int RetryMax { get; set; }

    }

    public class ProxySettings
    {
        public bool UseProxy { get; set; }
        public string ProxyServerAddress { get; set; }

    }

    public class AppSettings
    {
        public bool ConvertToE164 { get; set; }
        public string CountryCode { get; set; }
        public string RemoveFirstDigitIfStartWith { get; set; }




	}



}
