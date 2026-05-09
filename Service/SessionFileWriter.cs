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

        private bool disposed = false;

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
            ThrowIfDisposed();

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
            ThrowIfDisposed();

            rejectWriter.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} | {1}",
                    reason,
                    sample));

            rejectWriter.Flush();
        }

        public void WriteLog(string message)
        {
            Console.WriteLine(message);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                if (measurementWriter != null)
                {
                    measurementWriter.Dispose();
                }

                if (rejectWriter != null)
                {
                    rejectWriter.Dispose();
                }

                Console.WriteLine(
                    "SessionFileWriter.Dispose(): fajlovi su zatvoreni za sesiju "
                    + SessionDirectoryPath);
            }

            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(
                    "SessionFileWriter");
            }
        }
    }
}
