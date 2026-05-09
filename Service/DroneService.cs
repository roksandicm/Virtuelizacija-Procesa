using Common;
using System;
using System.Configuration;
using System.IO;
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

        public DroneService()
        {
            configuration = new ConfigurationReader();

            validator =
                new DroneSampleValidator(
                    configuration);
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

            storage = new SessionFileWriter(configuration.StoragePath);

            sessionStarted = true;
            sampleCount = 0;
            windAngleSum = 0;

            Console.WriteLine("Session started.");

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

            double? currentAverage =
                sampleCount > 0
                ? (double?)(windAngleSum / sampleCount)
                : null;

            string validationError =
                validator.GetValidationError(
                    sample,
                    currentAverage);

            if (validationError != null)
            {
                storage.WriteRejectedSample(
                    sample,
                    validationError);

                Console.WriteLine(
                    "NACK: " + validationError);

                return new SessionResponse(
                    false,
                    validationError,
                    SessionStatus.IN_PROGRESS);
            }

            storage.WriteAcceptedSample(sample);

            sampleCount++;

            windAngleSum += sample.WindAngle;

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

            Console.WriteLine("Session completed.");

            return new SessionResponse(
                true,
                "Session completed.",
                SessionStatus.COMPLETED);
        }
    }
}