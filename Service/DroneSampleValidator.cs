using Common;
using System;

namespace Service
{
    public class DroneSampleValidator
    {
        private readonly ConfigurationReader configuration;

        public DroneSampleValidator(ConfigurationReader configuration)
        {
            this.configuration = configuration;
        }

        public string GetValidationError(DroneSample sample)
        {
            if (sample == null)
            {
                return "DroneSample nije poslat.";
            }

            if (!IsValidNumber(sample.LinearAccelerationX))
            {
                return "LinearAccelerationX nije ispravan broj.";
            }

            if (!IsValidNumber(sample.LinearAccelerationY))
            {
                return "LinearAccelerationY nije ispravan broj.";
            }

            if (!IsValidNumber(sample.LinearAccelerationZ))
            {
                return "LinearAccelerationZ nije ispravan broj.";
            }

            if (!IsValidNumber(sample.WindSpeed))
            {
                return "WindSpeed nije ispravan broj.";
            }

            if (!IsValidNumber(sample.WindAngle))
            {
                return "WindAngle nije ispravan broj.";
            }

            if (!IsValidNumber(sample.FlightDuration))
            {
                return "FlightDuration nije ispravan broj.";
            }

            if (sample.WindSpeed <= 0)
            {
                return "WindSpeed mora biti veći od 0 m/s.";
            }

            if (sample.WindAngle < 0 || sample.WindAngle >= 360)
            {
                return "WindAngle mora biti u opsegu od 0 do 359 stepeni.";
            }

            if (sample.FlightDuration < 0)
            {
                return "FlightDuration ne sme biti negativan.";
            }

            return null;
        }

        private bool IsValidNumber(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
