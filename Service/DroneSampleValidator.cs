using Common;
using System;
using System.Globalization;

namespace Service
{
    public class DroneSampleValidator
    {
        private readonly ConfigurationReader configuration;

        public DroneSampleValidator(
            ConfigurationReader configuration)
        {
            this.configuration = configuration;
        }

        public string GetValidationError(
            DroneSample sample,
            double? currentWindAverage)
        {
            if (sample == null)
            {
                return "DroneSample nije poslat.";
            }

            if (sample.WindSpeed < 0)
            {
                return "WindSpeed mora biti pozitivan.";
            }

            if (Math.Abs(sample.WindSpeed) >
                configuration.WThreshold)
            {
                return "WindSpeed prelazi W_threshold. Vrednost: " +
                       sample.WindSpeed.ToString(
                           CultureInfo.InvariantCulture);
            }

            double linearAccelerationMagnitude =
                Math.Sqrt(
                    sample.LinearAccelerationX * sample.LinearAccelerationX +
                    sample.LinearAccelerationY * sample.LinearAccelerationY +
                    sample.LinearAccelerationZ * sample.LinearAccelerationZ);

            if (linearAccelerationMagnitude >
                configuration.LThreshold)
            {
                return "LinearAcceleration prelazi L_threshold. Magnitude: " +
                       linearAccelerationMagnitude.ToString(
                           CultureInfo.InvariantCulture);
            }

            if (currentWindAverage.HasValue &&
                currentWindAverage.Value > 0)
            {
                double lowerLimit =
                    currentWindAverage.Value *
                    (1 - configuration.AllowedDeviation);

                double upperLimit =
                    currentWindAverage.Value *
                    (1 + configuration.AllowedDeviation);

                if (sample.WindAngle < lowerLimit ||
                    sample.WindAngle > upperLimit)
                {
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "WindAngle odstupa vise od ±{0:P0} od proseka. WindAngle={1}, Average={2}",
                        configuration.AllowedDeviation,
                        sample.WindAngle,
                        currentWindAverage.Value);
                }
            }

            return null;
        }
    }
}