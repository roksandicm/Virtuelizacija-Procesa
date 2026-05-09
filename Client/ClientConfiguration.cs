using System;
using System.Configuration;

namespace Client
{
    public class ClientConfiguration
    {
        public string CsvPath { get; private set; }

        public int RowsToSend { get; private set; }

        public string ClientLogPath { get; private set; }

        public string ServerRejectedLogPath { get; private set; }

        public ClientConfiguration()
        {
            CsvPath = ReadString(
                "csvPath",
                @"Data\drone_samples.csv");

            RowsToSend = ReadInt(
                "rowsToSend",
                120);

            ClientLogPath = ReadString(
                "clientLogPath",
                @"Logs\client_csv_log.txt");

            ServerRejectedLogPath = ReadString(
                "serverRejectedLogPath",
                @"Logs\server_rejected_responses.txt");
        }

        private string ReadString(
            string key,
            string defaultValue)
        {
            string raw =
                ConfigurationManager.AppSettings[key];

            return string.IsNullOrWhiteSpace(raw)
                ? defaultValue
                : raw;
        }

        private int ReadInt(
            string key,
            int defaultValue)
        {
            string raw =
                ConfigurationManager.AppSettings[key];

            int value;

            return int.TryParse(raw, out value)
                ? value
                : defaultValue;
        }
    }
}