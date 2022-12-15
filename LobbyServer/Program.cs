using NLog;
using System;
using System.IO;
using System.Threading;

namespace IWANGOEmulator.LobbyServer
{
    class Program
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            Log.Info("IWANGO Emulator: Lobby Server by Ioncannon");

            Server server = new Server();

            // Daytona expects 5 lobbies preloaded
            server.AddGame("Daytona");
            server.CreateLobby("TestLobby", 100);
            server.CreateLobby("Daytona_Lobby2", 100);
            server.CreateLobby("Daytona_Lobby3", 100);
            server.CreateLobby("Daytona_Lobby4", 100);
            server.CreateLobby("Daytona_Lobby5", 100);

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
                else if (input.StartsWith('!'))
                {
                    string[] split = input.Split('|');
                    if (split.Length == 1 || split.Length == 2)
                    {
                        if (!ushort.TryParse(split[0].Substring(1), System.Globalization.NumberStyles.HexNumber, null, out ushort opcode))
                            continue;

                        server.SendAll(opcode, split.Length == 2 ? split[1] : "");
                    }
                }
                else if (input.StartsWith('p'))
                {
                    string[] split = input.Split(' ');
                    if (split.Length == 2)
                    {
                        if (!int.TryParse(split[0].Substring(1), System.Globalization.NumberStyles.HexNumber, null, out int val1))
                            continue;

                        if (!int.TryParse(split[1], System.Globalization.NumberStyles.HexNumber, null, out int val2))
                            continue;

                        server.SendPing(val1, val2);
                    }
                }
                else if (input.Equals("clear"))
                    Console.Clear();
                Thread.Sleep(200);
            };
        }
    }
}
