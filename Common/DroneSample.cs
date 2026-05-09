using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class DroneSample
    {
        [DataMember]
        public double LinearAccelerationX { get; set; }

        [DataMember]
        public double LinearAccelerationY { get; set; }

        [DataMember]
        public double LinearAccelerationZ { get; set; }

        [DataMember]
        public double WindSpeed { get; set; }

        [DataMember]
        public double WindAngle { get; set; }

        [DataMember]
        public double FlightDuration { get; set; }

        public DroneSample()
        {
        }

        public DroneSample(
            double x,
            double y,
            double z,
            double windSpeed,
            double windAngle,
            double flightDuration)
        {
            LinearAccelerationX = x;
            LinearAccelerationY = y;
            LinearAccelerationZ = z;
            WindSpeed = windSpeed;
            WindAngle = windAngle;
            FlightDuration = flightDuration;
        }
    }
}
