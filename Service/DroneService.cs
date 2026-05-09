using Common;
using System;
using System.Globalization;
using System.ServiceModel;

namespace Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class DroneService : IDroneService
    {
        private readonly ConfigurationReader configuration;
        private readonly DroneSampleValidator validator;

        private SessionFileWriter storage;

        private bool sessionStarted = false;
        private int sampleCount = 0;
        private double windAngleSum = 0;
        private double? previousWindAngle = null;

        public event TransferStartedEventHandler OnTransferStarted;
        public event SampleReceivedEventHandler OnSampleReceived;
        public event TransferCompletedEventHandler OnTransferCompleted;
        public event WarningRaisedEventHandler OnWarningRaised;

        public DroneService()
        {
            configuration = new ConfigurationReader();
            validator = new DroneSampleValidator(configuration);

            OnTransferStarted += LogTransferStarted;
            OnSampleReceived += LogSampleReceived;
            OnTransferCompleted += LogTransferCompleted;
            OnWarningRaised += LogWarningRaised;
        }

        public SessionResponse StartSession(SessionMeta sessionMeta)
        {
            if (sessionMeta == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault(
                        "SessionMeta je null.",
                        "SessionMeta",
                        0));
            }

            if (sessionMeta.Headers == null ||
                sessionMeta.Headers.Length == 0)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault(
                        "Headers nisu ispravni.",
                        "Headers",
                        0));
            }

            string missingHeader = GetMissingRequiredHeader(sessionMeta.Headers);

            if (missingHeader != null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault(
                        "Nedostaje obavezno meta-zaglavlje: " + missingHeader,
                        missingHeader,
                        0));
            }

            if (sessionMeta.ExpectedRows <= 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault(
                        "ExpectedRows mora biti veći od 0.",
                        "ExpectedRows",
                        sessionMeta.ExpectedRows.ToString(CultureInfo.InvariantCulture)));
            }

            storage = new SessionFileWriter(configuration.StoragePath);

            sessionStarted = true;
            sampleCount = 0;
            windAngleSum = 0;
            previousWindAngle = null;

            RaiseTransferStarted("Prenos je pokrenut.");
            Console.WriteLine("Status: prenos u toku...");

            return new SessionResponse(
                true,
                "Session started successfully.",
                SessionStatus.IN_PROGRESS);
        }

        public SessionResponse PushSample(DroneSample sample)
        {
            if (!sessionStarted)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault(
                        "Session nije pokrenut.",
                        "Session",
                        "NOT_STARTED"));
            }

            if (sample == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault(
                        "DroneSample je null.",
                        "DroneSample",
                        0));
            }

            string validationError = validator.GetValidationError(sample);

            if (validationError != null)
            {
                storage.WriteRejectedSample(sample, validationError);

                Console.WriteLine("NACK: " + validationError);

                throw new FaultException<ValidationFault>(
                    new ValidationFault(
                        validationError,
                        "DroneSample",
                        "INVALID"));
            }

            storage.WriteAcceptedSample(sample);
            RaiseSampleReceived(sample, sampleCount + 1);

            AnalyzeWindDirectionShift(sample);
            AnalyzeWindAngleOutOfBand(sample);
            AnalyzeLateralAsymmetry(sample);

            sampleCount++;
            windAngleSum += sample.WindAngle;
            previousWindAngle = sample.WindAngle;

            Console.WriteLine("ACK: Sample accepted.");

            return new SessionResponse(
                true,
                "Sample accepted.",
                SessionStatus.IN_PROGRESS);
        }

        public SessionResponse EndSession()
        {
            if (!sessionStarted)
            {
                return new SessionResponse(
                    false,
                    "Session nije pokrenut.",
                    SessionStatus.COMPLETED);
            }

            storage?.Dispose();
            storage = null;
            sessionStarted = false;

            RaiseTransferCompleted("Prenos je završen.", sampleCount);
            Console.WriteLine("Status: završen prenos.");

            return new SessionResponse(
                true,
                "Session completed.",
                SessionStatus.COMPLETED);
        }

        private string GetMissingRequiredHeader(string[] headers)
        {
            string[] requiredHeaders =
            {
                "LinearAccelerationX",
                "LinearAccelerationY",
                "LinearAccelerationZ",
                "WindSpeed",
                "WindAngle",
                "FlightDuration"
            };

            for (int i = 0; i < requiredHeaders.Length; i++)
            {
                if (!ContainsHeader(headers, requiredHeaders[i]))
                {
                    return requiredHeaders[i];
                }
            }

            return null;
        }

        private bool ContainsHeader(string[] headers, string requiredHeader)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                if (string.Equals(
                    headers[i],
                    requiredHeader,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void AnalyzeWindDirectionShift(DroneSample sample)
        {
            if (!previousWindAngle.HasValue)
            {
                return;
            }

            double deltaWindAngle = CalculateWindAngleDelta(sample.WindAngle, previousWindAngle.Value);

            if (Math.Abs(deltaWindAngle) > configuration.WThreshold)
            {
                string direction = deltaWindAngle > 0
                    ? "u smeru kazaljke"
                    : "suprotno od kazaljke";

                RaiseWarning(
                    "WindDirectionShift",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Nagla promena smera vetra. DeltaWindAngle={0}, prag={1}, smer={2}.",
                        deltaWindAngle,
                        configuration.WThreshold,
                        direction),
                    direction,
                    deltaWindAngle,
                    configuration.WThreshold);
            }
        }

        private static double CalculateWindAngleDelta(double currentAngle, double previousAngle)
        {
            double delta = currentAngle - previousAngle;

            if (delta > 180)
            {
                delta -= 360;
            }
            else if (delta < -180)
            {
                delta += 360;
            }

            return delta;
        }

        private void AnalyzeWindAngleOutOfBand(DroneSample sample)
        {
            if (sampleCount == 0)
            {
                return;
            }

            double windAngleMean = windAngleSum / sampleCount;

            if (Math.Abs(windAngleMean) < double.Epsilon)
            {
                return;
            }

            double lowerLimit = windAngleMean * (1 - configuration.AllowedDeviation);
            double upperLimit = windAngleMean * (1 + configuration.AllowedDeviation);

            if (sample.WindAngle < lowerLimit || sample.WindAngle > upperLimit)
            {
                string direction = sample.WindAngle < lowerLimit
                    ? "ispod očekivane vrednosti"
                    : "iznad očekivane vrednosti";

                RaiseWarning(
                    "OutOfBandWarning",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "WindAngle odstupa od tekućeg proseka. WindAngle={0}, prosek={1}, dozvoljeno={2:P0}, smer={3}.",
                        sample.WindAngle,
                        windAngleMean,
                        configuration.AllowedDeviation,
                        direction),
                    direction,
                    sample.WindAngle,
                    windAngleMean);
            }
        }

        private void AnalyzeLateralAsymmetry(DroneSample sample)
        {
            double x = sample.LinearAccelerationX;
            double y = sample.LinearAccelerationY;
            double z = sample.LinearAccelerationZ;

            double anorm = Math.Sqrt(x * x + y * y + z * z);

            if (Math.Abs(anorm) < double.Epsilon)
            {
                return;
            }

            double wasym = Math.Abs(x) / anorm;

            if (wasym > configuration.LThreshold)
            {
                string direction = x < 0
                    ? "nagib na levu stranu"
                    : "nagib na desnu stranu";

                RaiseWarning(
                    "LateralAsymmetryWarning",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Bočna asimetrija ubrzanja. Wasym={0}, prag={1}, smer={2}.",
                        wasym,
                        configuration.LThreshold,
                        direction),
                    direction,
                    wasym,
                    configuration.LThreshold);
            }
        }

        private void RaiseTransferStarted(string message)
        {
            OnTransferStarted?.Invoke(
                this,
                new TransferStartedEventArgs(message, DateTime.Now));
        }

        private void RaiseSampleReceived(DroneSample sample, int ordinalNumber)
        {
            OnSampleReceived?.Invoke(
                this,
                new SampleReceivedEventArgs(sample, ordinalNumber, DateTime.Now));
        }

        private void RaiseTransferCompleted(string message, int totalSamples)
        {
            OnTransferCompleted?.Invoke(
                this,
                new TransferCompletedEventArgs(message, totalSamples, DateTime.Now));
        }

        private void RaiseWarning(
            string warningType,
            string message,
            string direction,
            double value,
            double threshold)
        {
            OnWarningRaised?.Invoke(
                this,
                new WarningRaisedEventArgs(
                    warningType,
                    message,
                    direction,
                    value,
                    threshold,
                    DateTime.Now));
        }

        private void LogTransferStarted(object sender, TransferStartedEventArgs e)
        {
            Console.WriteLine("EVENT OnTransferStarted: " + e.Message);
        }

        private void LogSampleReceived(object sender, SampleReceivedEventArgs e)
        {
            Console.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "EVENT OnSampleReceived: #{0}, WindAngle={1}",
                    e.OrdinalNumber,
                    e.Sample.WindAngle));
        }

        private void LogTransferCompleted(object sender, TransferCompletedEventArgs e)
        {
            Console.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "EVENT OnTransferCompleted: {0} Ukupno uzoraka: {1}",
                    e.Message,
                    e.TotalSamples));
        }

        private void LogWarningRaised(object sender, WarningRaisedEventArgs e)
        {
            Console.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "EVENT OnWarningRaised [{0}]: {1}",
                    e.WarningType,
                    e.Message));
        }
    }

    public delegate void TransferStartedEventHandler(
        object sender,
        TransferStartedEventArgs e);

    public delegate void SampleReceivedEventHandler(
        object sender,
        SampleReceivedEventArgs e);

    public delegate void TransferCompletedEventHandler(
        object sender,
        TransferCompletedEventArgs e);

    public delegate void WarningRaisedEventHandler(
        object sender,
        WarningRaisedEventArgs e);

    public class TransferStartedEventArgs : EventArgs
    {
        public string Message { get; private set; }
        public DateTime TimeStamp { get; private set; }

        public TransferStartedEventArgs(string message, DateTime timeStamp)
        {
            Message = message;
            TimeStamp = timeStamp;
        }
    }

    public class SampleReceivedEventArgs : EventArgs
    {
        public DroneSample Sample { get; private set; }
        public int OrdinalNumber { get; private set; }
        public DateTime TimeStamp { get; private set; }

        public SampleReceivedEventArgs(
            DroneSample sample,
            int ordinalNumber,
            DateTime timeStamp)
        {
            Sample = sample;
            OrdinalNumber = ordinalNumber;
            TimeStamp = timeStamp;
        }
    }

    public class TransferCompletedEventArgs : EventArgs
    {
        public string Message { get; private set; }
        public int TotalSamples { get; private set; }
        public DateTime TimeStamp { get; private set; }

        public TransferCompletedEventArgs(
            string message,
            int totalSamples,
            DateTime timeStamp)
        {
            Message = message;
            TotalSamples = totalSamples;
            TimeStamp = timeStamp;
        }
    }

    public class WarningRaisedEventArgs : EventArgs
    {
        public string WarningType { get; private set; }
        public string Message { get; private set; }
        public string Direction { get; private set; }
        public double Value { get; private set; }
        public double Threshold { get; private set; }
        public DateTime TimeStamp { get; private set; }

        public WarningRaisedEventArgs(
            string warningType,
            string message,
            string direction,
            double value,
            double threshold,
            DateTime timeStamp)
        {
            WarningType = warningType;
            Message = message;
            Direction = direction;
            Value = value;
            Threshold = threshold;
            TimeStamp = timeStamp;
        }
    }
}
