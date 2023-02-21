using NLog;
using System;
using System.Collections.Generic;
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
                Thread.Sleep(200);
            };
        }
    }
}
