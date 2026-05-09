using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Globalization;

namespace Service
{
    public class ConfigurationReader
    {
        public double WThreshold { get; private set; }

        public double LThreshold { get; private set; }

        public double AllowedDeviation { get; private set; }

        public string StoragePath { get; private set; }

        public ConfigurationReader()
        {
            WThreshold = ReadDouble("W_threshold", 50);

            LThreshold = ReadDouble("L_threshold", 20);

            AllowedDeviation = ReadDouble("AllowedDeviation", 0.22);

            StoragePath = ReadString("storagePath", "Sessions");
        }

        private double ReadDouble(string key, double defaultValue)
        {
            string raw = ConfigurationManager.AppSettings[key];

            if (string.IsNullOrWhiteSpace(raw))
            {
                return defaultValue;
            }

            double value;

            if (!double.TryParse(
                raw,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value))
            {
                return defaultValue;
            }

            return value;
        }

        private string ReadString(string key, string defaultValue)
        {
            string raw = ConfigurationManager.AppSettings[key];

            return string.IsNullOrWhiteSpace(raw)
                ? defaultValue
                : raw;
        }
    }
}
