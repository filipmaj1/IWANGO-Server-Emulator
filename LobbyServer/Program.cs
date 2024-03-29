﻿using NLog;
using System;
using System.IO;
using System.Net;
using System.Threading;

namespace IWANGOEmulator.LobbyServer
{
    class Program
    {
        public static string GAMESERVER_NAME = "Daytona_USA_Emu_#1";
        public static string GAMESERVER_IP = "0.0.0.0";
        public static ushort GAMESERVER_PORT = 9501;

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static Server Server;

        static void Main(string[] args)
        {
            Log.Info("IWANGO Emulator: Lobby Server by Ioncannon");

            // Parse args
            for (int i = 0; i < args.Length; i++)
            { 
                if (args[i].Equals("-name") && args.Length - i >= 2)
                {
                    GAMESERVER_NAME = args[++i];                    
                }
                if (args[i].Equals("-ip") && args.Length - i >= 2)
                {
                    GAMESERVER_IP = args[++i];
                    if (IPAddress.TryParse(GAMESERVER_IP, out IPAddress ip))
                    {
                        Log.Error("Invalid ip provided.");
                        return;
                    }
                }
                if (args[i].Equals("-port") && args.Length - i >= 2)
                {
                    if (!ushort.TryParse(args[++i], out GAMESERVER_PORT))
                    {
                        Log.Error("Invalid port provided.");
                        return;
                    }
                }
            }

            // Setup server. Daytona expects 5 lobbies preloaded
            Server = new Server(GAMESERVER_NAME, GAMESERVER_IP, GAMESERVER_PORT);
            Server.AddGame("Daytona");
            Server.CreateLobby("2P_Red", 100);
            Server.CreateLobby("4P_Yellow", 100);
            Server.CreateLobby("2P_Blue", 100);
            Server.CreateLobby("2P_Green", 100);
            Server.CreateLobby("4P_Purple", 100);
            Server.CreateLobby("4P_Orange", 100);

            if (Server.StartServer() == -1)
                return;

            while (true)
            {
                string input = Console.ReadLine();
                if (input.StartsWith('@'))
                {
                    try
                    {
                        byte[] data = File.ReadAllBytes(".\\" + input.Substring(1));
                        Server.DebugSendToAll(data);
                        Console.WriteLine("Sent!");
                    }
                    catch (IOException) { Console.WriteLine("> File not found"); }
                }
                else if (input.Equals("clear"))
                    Console.Clear();
                Thread.Sleep(200);
            };
        }

        public static Server GetServer()
        {
            return Server;
        }
    }
}
