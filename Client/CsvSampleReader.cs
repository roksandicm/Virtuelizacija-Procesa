using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Common;

namespace Client
{
    public class CsvSampleReader : IDisposable
    {
        private TextReader textReader;

        private TextWriter logWriter;

        private bool disposed = false;

        private readonly string path;

        private readonly string logPath;

        public string Path
        {
            get { return path; }
        }

        public CsvSampleReader(
            string path,
            string logPath)
        {
            this.path = path;

            this.logPath = logPath;

            string logDirectory =
                System.IO.Path.GetDirectoryName(
                    logPath);

            if (!string.IsNullOrWhiteSpace(logDirectory)
                &&
                !Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(
                    logDirectory);
            }
        }

        ~CsvSampleReader()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(
            bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (textReader != null)
                    {
                        textReader.Dispose();

                        textReader = null;
                    }

                    if (logWriter != null)
                    {
                        logWriter.Dispose();

                        logWriter = null;
                    }
                }

                disposed = true;
            }
        }

        public List<DroneSample>
            ReadFirstValidSamples(
                int maxRows)
        {
            List<DroneSample> samples =
                new List<DroneSample>(
                    maxRows);

            textReader =
                new StreamReader(path);

            logWriter =
                new StreamWriter(
                    logPath,
                    false);

            string header =
                textReader.ReadLine();

            if (string.IsNullOrWhiteSpace(header))
            {
                throw new InvalidOperationException(
                    "CSV fajl je prazan ili nema header.");
            }

            Dictionary<string, int> indexes =
                CreateIndexMap(header);

            ValidateRequiredHeaders(indexes);

            string line;

            int lineNumber = 1;

            while ((line = textReader.ReadLine()) != null)
            {
                lineNumber++;

                if (samples.Count >= maxRows)
                {
                    logWriter.WriteLine(
                        "RED_VISKA | Line="
                        + lineNumber
                        + " | "
                        + line);

                    continue;
                }

                CsvLineParseResult result =
                    ParseLine(
                        line,
                        lineNumber,
                        indexes);

                if (result.Success)
                {
                    samples.Add(result.Sample);
                }
                else
                {
                    logWriter.WriteLine(
                        "NEVALIDAN_RED | Line="
                        + result.LineNumber
                        + " | "
                        + result.ErrorMessage
                        + " | "
                        + result.RawLine);
                }
            }

            logWriter.Flush();

            return samples;
        }

        private Dictionary<string, int>
            CreateIndexMap(string header)
        {
            string[] parts =
                header.Split(',');

            Dictionary<string, int> indexes =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < parts.Length; i++)
            {
                indexes[parts[i].Trim()] = i;
            }

            return indexes;
        }

        private void ValidateRequiredHeaders(
            Dictionary<string, int> indexes)
        {
            RequireHeader(
                indexes,
                "linear_acceleration_x");

            RequireHeader(
                indexes,
                "linear_acceleration_y");

            RequireHeader(
                indexes,
                "linear_acceleration_z");

            RequireHeader(
                indexes,
                "wind_speed");

            RequireHeader(
                indexes,
                "wind_angle");

            RequireHeader(
                indexes,
                "time");
        }

        private void RequireHeader(
            Dictionary<string, int> indexes,
            string name)
        {
            if (!indexes.ContainsKey(name))
            {
                throw new InvalidOperationException(
                    "CSV nema obaveznu kolonu: "
                    + name);
            }
        }

        private CsvLineParseResult ParseLine(
            string line,
            int lineNumber,
            Dictionary<string, int> indexes)
        {
            CsvLineParseResult result =
                new CsvLineParseResult
                {
                    LineNumber = lineNumber,
                    RawLine = line
                };

            try
            {
                string[] parts =
                    line.Split(',');

                DroneSample sample =
                    new DroneSample
                    {
                        LinearAccelerationX =
                            ReadDouble(
                                parts,
                                indexes,
                                "linear_acceleration_x"),

                        LinearAccelerationY =
                            ReadDouble(
                                parts,
                                indexes,
                                "linear_acceleration_y"),

                        LinearAccelerationZ =
                            ReadDouble(
                                parts,
                                indexes,
                                "linear_acceleration_z"),

                        WindSpeed =
                            ReadDouble(
                                parts,
                                indexes,
                                "wind_speed"),

                        WindAngle =
                            ReadDouble(
                                parts,
                                indexes,
                                "wind_angle"),

                        FlightDuration =
                            ReadDouble(
                                parts,
                                indexes,
                                "time")
                    };

                result.Success = true;

                result.Sample = sample;
            }
            catch (Exception e)
            {
                result.Success = false;

                result.ErrorMessage =
                    e.Message;
            }

            return result;
        }

        private double ReadDouble(
            string[] parts,
            Dictionary<string, int> indexes,
            string columnName)
        {
            string raw =
                GetRawValue(
                    parts,
                    indexes,
                    columnName);

            double value;

            if (!double.TryParse(
                raw,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value))
            {
                throw new FormatException(
                    "Polje "
                    + columnName
                    + " nije validan double: "
                    + raw);
            }

            return value;
        }

        private string GetRawValue(
            string[] parts,
            Dictionary<string, int> indexes,
            string columnName)
        {
            int index = indexes[columnName];

            if (index < 0 || index >= parts.Length)
            {
                throw new FormatException(
                    "Red nema kolonu "
                    + columnName);
            }

            string raw =
                parts[index].Trim();

            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new FormatException(
                    "Polje "
                    + columnName
                    + " je prazno.");
            }

            return raw;
        }
    }
}