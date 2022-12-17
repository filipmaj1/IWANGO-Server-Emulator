using NLog;
using System;
using System.IO;

namespace IWANGOEmulator.GameServer
{
    class Program
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            Log.Info("IWANGO Game Server");
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
            }
        }
    }
}

