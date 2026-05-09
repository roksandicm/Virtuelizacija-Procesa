using System;
using System.ServiceModel;

namespace Service
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ServiceHost host =
                    new ServiceHost(
                        typeof(DroneService));

                host.Open();

                Console.WriteLine(
                    "DroneService is running...");

                Console.ReadLine();

                host.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Error: " + ex.Message);

                Console.ReadLine();
            }
        }
    }
}