using NLog;
using System;
using System.IO;
using System.Threading;

namespace IWANGOEmulator.GateServer
{
    class Program
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            Log.Info("IWANGO Emulator: Gate Server by Ioncannon");

            Server server = new Server();
            server.StartServer();

            while (true)
            {
                string input = Console.ReadLine();
                if (input.StartsWith('@'))
                {
                    try
                    {
                        byte[] data = File.ReadAllBytes(".\\" + input.Substring(1));
                        server.SendAll(data);
                    }
                    catch (IOException) { Console.WriteLine("> File not found"); }
                }
                Thread.Sleep(200);
            };
        }
    }
}
