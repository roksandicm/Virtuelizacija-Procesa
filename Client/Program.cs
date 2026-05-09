using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using Common;

namespace Client
{
    class Program
    {
        private const string SimulateDisposeArgument = "simulate-dispose";

        static void Main(string[] args)
        {
            ClientConfiguration configuration =
                new ClientConfiguration();

            ChannelFactory<IDroneService> factory = null;

            IDroneService proxy = null;

            bool serverSessionStarted = false;

            bool endSessionCalled = false;

            bool communicationClosed = false;

            bool shouldAbort = false;

            bool simulateTransferException =
                ShouldSimulateTransferException(args);

            try
            {
                Console.WriteLine(
                    "Ucitavanje CSV fajla: "
                    + configuration.CsvPath);

                if (simulateTransferException)
                {
                    Console.WriteLine(
                        "SIMULACIJA JE UKLJUCENA: klijent ce baciti izuzetak usred prenosa radi provere Dispose mehanizma.");
                }

                List<DroneSample> samples;

                using (CsvSampleReader reader =
                    new CsvSampleReader(
                        configuration.CsvPath,
                        configuration.ClientLogPath))
                {
                    samples =
                        reader.ReadFirstValidSamples(
                            configuration.RowsToSend);
                }

                Console.WriteLine(
                    "Broj validno parsiranih redova za slanje: "
                    + samples.Count);

                factory =
                    new ChannelFactory<IDroneService>(
                        "DroneServiceEndpoint");

                proxy =
                    factory.CreateChannel();

                SessionMeta meta =
                    new SessionMeta(
                        System.IO.Path.GetFileName(
                            configuration.CsvPath),
                        samples.Count);

                SessionResponse startResponse =
                    proxy.StartSession(meta);

                serverSessionStarted = true;

                Console.WriteLine(
                    "SERVER: "
                    + startResponse.Message);

                EnsureLogDirectory(
                    configuration.ServerRejectedLogPath);

                using (TextWriter rejectedLog =
                    new StreamWriter(
                        configuration.ServerRejectedLogPath,
                        false))
                {
                    for (int i = 0; i < samples.Count; i++)
                    {
                        if (simulateTransferException
                            &&
                            i == 10)
                        {
                            throw new InvalidOperationException(
                                "SIMULACIJA: prekid prenosa usred slanja uzoraka.");
                        }

                        SessionResponse response =
                            proxy.PushSample(
                                samples[i]);

                        Console.WriteLine(
                            "Red "
                            + (i + 1)
                            + ": "
                            + response.Message);

                        if (!response.Ack)
                        {
                            rejectedLog.WriteLine(
                                "Line="
                                + (i + 1)
                                + " | "
                                + response.Message
                                + " | "
                                + samples[i]);
                        }
                    }
                }

                SessionResponse endResponse =
                    proxy.EndSession();

                endSessionCalled = true;

                Console.WriteLine(
                    "SERVER: "
                    + endResponse.Message);

                CloseCommunication(
                    proxy,
                    factory);

                communicationClosed = true;
            }
            catch (FaultException<DataFormatFault> e)
            {
                Console.WriteLine(
                    "DataFormatFault: "
                    + e.Detail.Message);

                shouldAbort = true;
            }
            catch (FaultException<ValidationFault> e)
            {
                Console.WriteLine(
                    "ValidationFault: "
                    + e.Detail.Message);

                shouldAbort = true;
            }
            catch (CommunicationException e)
            {
                Console.WriteLine(
                    "CommunicationException: "
                    + e.Message);

                shouldAbort = true;
            }
            catch (TimeoutException e)
            {
                Console.WriteLine(
                    "TimeoutException: "
                    + e.Message);

                shouldAbort = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "Greska: "
                    + e.Message);

                shouldAbort = true;
            }
            finally
            {
                if (serverSessionStarted && !endSessionCalled)
                {
                    TryEndSessionForCleanup(
                        proxy,
                        ref endSessionCalled);
                }

                if (!communicationClosed)
                {
                    CleanupCommunication(
                        proxy,
                        factory,
                        shouldAbort);
                }
            }

            Console.WriteLine(
                "Pritisni ENTER za kraj...");

            Console.ReadLine();
        }

        private static bool ShouldSimulateTransferException(
            string[] args)
        {
            if (args == null)
            {
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(
                    args[i],
                    SimulateDisposeArgument,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void TryEndSessionForCleanup(
            IDroneService proxy,
            ref bool endSessionCalled)
        {
            if (proxy == null)
            {
                return;
            }

            try
            {
                SessionResponse cleanupResponse =
                    proxy.EndSession();

                endSessionCalled = true;

                Console.WriteLine(
                    "FINALLY: EndSession je pozvan nakon greske, resursi na serveru treba da budu zatvoreni. "
                    + cleanupResponse.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "FINALLY: EndSession nije mogao da se pozove nakon greske: "
                    + e.Message);
            }
        }

        private static void CloseCommunication(
            IDroneService proxy,
            ChannelFactory<IDroneService> factory)
        {
            IClientChannel channel =
                proxy as IClientChannel;

            if (channel != null)
            {
                channel.Close();
            }

            if (factory != null)
            {
                factory.Close();
            }
        }

        private static void CleanupCommunication(
            IDroneService proxy,
            ChannelFactory<IDroneService> factory,
            bool shouldAbort)
        {
            if (shouldAbort)
            {
                Abort(proxy, factory);

                return;
            }

            try
            {
                CloseCommunication(
                    proxy,
                    factory);
            }
            catch
            {
                Abort(proxy, factory);
            }
        }

        private static void Abort(
            IDroneService proxy,
            ChannelFactory<IDroneService> factory)
        {
            IClientChannel channel =
                proxy as IClientChannel;

            if (channel != null)
            {
                channel.Abort();
            }

            if (factory != null)
            {
                factory.Abort();
            }
        }

        private static void EnsureLogDirectory(
            string logPath)
        {
            string directory =
                Path.GetDirectoryName(logPath);

            if (!string.IsNullOrWhiteSpace(directory)
                &&
                !Directory.Exists(directory))
            {
                Directory.CreateDirectory(
                    directory);
            }
        }
    }
}
