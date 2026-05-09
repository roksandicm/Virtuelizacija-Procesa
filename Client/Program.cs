using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using Common;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            ClientConfiguration configuration =
                new ClientConfiguration();

            ChannelFactory<IDroneService> factory = null;

            IDroneService proxy = null;

            try
            {
                Console.WriteLine(
                    "Ucitavanje CSV fajla: "
                    + configuration.CsvPath);

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

                Console.WriteLine(
                    "SERVER: "
                    + endResponse.Message);

                ((IClientChannel)proxy).Close();

                factory.Close();
            }
            catch (FaultException<DataFormatFault> e)
            {
                Console.WriteLine(
                    "DataFormatFault: "
                    + e.Detail.Message);

                Abort(proxy, factory);
            }
            catch (FaultException<ValidationFault> e)
            {
                Console.WriteLine(
                    "ValidationFault: "
                    + e.Detail.Message);

                Abort(proxy, factory);
            }
            catch (CommunicationException e)
            {
                Console.WriteLine(
                    "CommunicationException: "
                    + e.Message);

                Abort(proxy, factory);
            }
            catch (TimeoutException e)
            {
                Console.WriteLine(
                    "TimeoutException: "
                    + e.Message);

                Abort(proxy, factory);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "Greska: "
                    + e.Message);

                Abort(proxy, factory);
            }

            Console.WriteLine(
                "Pritisni ENTER za kraj...");

            Console.ReadLine();
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