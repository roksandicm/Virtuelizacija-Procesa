using Common;
using System;
using System.Globalization;
using System.IO;

namespace Service
{
    public class SessionFileWriter : IDisposable
    {
        private readonly StreamWriter measurementWriter;

        private readonly StreamWriter rejectWriter;

        public string SessionDirectoryPath { get; }

        public SessionFileWriter(string storagePath)
        {
            string timestamp =
                DateTime.Now.ToString("yyyyMMdd_HHmmss");

            SessionDirectoryPath = Path.Combine(
                storagePath,
                "Session_" + timestamp);

            Directory.CreateDirectory(SessionDirectoryPath);

            measurementWriter = new StreamWriter(
                Path.Combine(SessionDirectoryPath,
                "measurements.csv"),
                true);

            rejectWriter = new StreamWriter(
                Path.Combine(SessionDirectoryPath,
                "rejected.csv"),
                true);
        }

        public void WriteAcceptedSample(DroneSample sample)
        {
            measurementWriter.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5}",
                    sample.LinearAccelerationX,
                    sample.LinearAccelerationY,
                    sample.LinearAccelerationZ,
                    sample.WindSpeed,
                    sample.WindAngle,
                    sample.FlightDuration));

            measurementWriter.Flush();
        }

        public void WriteRejectedSample(
            DroneSample sample,
            string reason)
        {
            rejectWriter.WriteLine(
                reason);

            rejectWriter.Flush();
        }

        public void WriteLog(string message)
        {
            Console.WriteLine(message);
        }

        public void Dispose()
        {
            measurementWriter?.Dispose();
            rejectWriter?.Dispose();
        }
    }
}