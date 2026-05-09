using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class SessionMeta
    {
        [DataMember]
        public string[] Headers { get; set; }

        [DataMember]
        public string SourceFileName { get; set; }

        [DataMember]
        public int ExpectedRows { get; set; }

        public SessionMeta()
        {
        }

        public SessionMeta(string sourceFileName, int expectedRows)
        {
            SourceFileName = sourceFileName;
            ExpectedRows = expectedRows;

            Headers = new string[]
            {
                "LinearAccelerationX",
                "LinearAccelerationY",
                "LinearAccelerationZ",
                "WindSpeed",
                "WindAngle",
                "FlightDuration"
            };
        }
    }
}
