using IWANGOEmulator.LobbyServer.Models;
using System;
using System.Collections.Generic;
using System.Threading;

namespace IWANGOEmulator.LobbyServer
{
    class PacketProcessor
    {
        private Server Server;
        private readonly Thread ProcessThread;
        private Queue<Tuple<Player, Packet.Incoming>> ProcessQueue = new Queue<Tuple<Player, Packet.Incoming>>();

        public PacketProcessor(Server server)
        {
            Server = server;
            ProcessThread = new Thread(new ThreadStart(ProcessLoop));
        }

        private void HandlePacket(Player player, Packet.Incoming packet)
        {
            string[] split = packet.DataString.Split(' ');


#if DEBUG
            Program.Log.Debug($"{packet}");
#endif

            switch (packet.Opcode)
            {
                case 0x01: // Login 1
                    Player exists = Server.GetPlayer(split[0]);
                    if (exists != null)
                    {
                        exists.SetName("");
                        exists.Disconnect();
                    }

                    player.SetName(split[0]);
                    player.Send(new Packet.Outgoing(0x11, "0100 0102 Code5"));
                    break;
                case 0x02: // Login 2
                    player.Send(new Packet.Outgoing(0x0C, "LOB 999 999 AAA AAA"));
                    player.Send(new Packet.Outgoing(0x0A, "Welcome to IWANGO Emulator by Ioncannon"));
                    player.Send(new Packet.Outgoing(0xE1));
                    break;
                case 0x03: // SendLogData
                    break;                
                case 0x04: // Join/Create Lobby
                    {
                        if (split.Length == 2)
                        {
                            string lobbyName = split[0];
                            if (ushort.TryParse(split[1], out ushort maxCapacity))
                            {
                                Lobby lobby = Server.GetLobby(lobbyName);
                                if (lobby == null)
                                    lobby = Server.CreateLobby(lobbyName, maxCapacity);
                                if (lobby != null)
                                    player.JoinLobby(lobby);
                            }
                        }
                    }
                    break;
                case 0x05: // Disconnect
                    player.Send(new Packet.Outgoing(0xE3));
                    player.Send(new Packet.Outgoing(0x16));
                    player.Disconnect();
                    break;
                case 0x07: // Get Lobbies
                    Lobby[] lobbies = Server.GetLobbyList();
                    foreach (Lobby lobby in lobbies)
                        player.Send(new Packet.Outgoing(0x18, $"{lobby.Name} {lobby.NumPlayers} {lobby.MaxCapacity} {lobby.Flags} {(lobby.HasSharedMem ? lobby.SharedMem : "#")} #{lobby.Game.Name}"));
                    player.Send(new Packet.Outgoing(0x19));
                    break;
                case 0x08: // Get Games
                    Game[] games = Server.GetGames();
                    foreach (Game game in games)
                        player.Send(new Packet.Outgoing(0x1B, $"1 {game.Name}"));
                    player.Send(new Packet.Outgoing(0x1C));
                    break;
                case 0x09: // Select Game
                    {
                        string gameName = split[0];
                        player.SetGame(Server.GetGame(gameName));
                        player.Send(new Packet.Outgoing(0x1D, $"{player.Name} {player.CurrentGame.Name}"));
                    }
                    break;
                case 0x0A: // Ping
                    player.Send(new Packet.Outgoing(0x00));
                    break;
                case 0x0B: // Search Player
                    break;
                case 0x0C: // Get License?
                    player.Send(new Packet.Outgoing(0x22, "ABCDEFGHI"));
                    break;
                case 0x0F: // Get Teams
                    if (player.CurrentLobby != null)
                    {
                        foreach (Team team in player.CurrentLobby.Teams)
                            player.Send(new Packet.Outgoing(0x32, $"{team.Name} {team.NumPlayers} {team.MaxCapacity} {team.Flags} #{team.SharedMem} # {player.CurrentLobby.Game.Name}"));
                        player.Send(new Packet.Outgoing(0x33));
                    }
                    player.Send(new Packet.Outgoing(0x33));
                    break;
                case 0x10: // Player List Refresh 
                    {
                        // Get all players
                        if (split[0].Length == 0)
                        {
                            foreach (Player p in player.CurrentLobby.Members)
                                player.Send(p.GetSendDataPacket());
                            player.Send(new Packet.Outgoing(0x31));
                        }
                        else // Get specific
                        {
                            string name = split[0];
                            Player p = Server.GetPlayer(name);
                            if (p != null)
                            {
                                player.Send(p.GetSendDataPacket());
                                player.Send(new Packet.Outgoing(0x31));
                            }
                        }
                    }
                    break;
                case 0x11: // Receive Lobby Chat
                    if (player.CurrentLobby != null)
                    {
                        player.CurrentLobby.SendChat(player.Name, packet.DataString.Substring(packet.DataString.IndexOf(' ') + 1));
                    }
                    break;
                case 0x1B: // Player SharedMem        
                    player.SetSharedMem(packet.Data);
                    break;
                case 0x20: // Team SharedMem
                    {
                        string teamName = split[0];
                        string sharedMemStr = split[1];

                        if (player.CurrentTeam != null)
                            player.CurrentTeam.SetSharedMem(sharedMemStr);
                    }
                    break;
                case 0x21: // Leave Team
                    player.LeaveTeam();
                    break;
                case 0x23: // Team Chat
                    if (player.CurrentTeam != null)
                        player.CurrentTeam.SendChat(player.Name, packet.DataString);
                    break;
                case 0x24: // Create Team
                    {
                        if (split.Length == 3 && ushort.TryParse(split[0], out ushort capacity))
                        {
                            string name = split[1];
                            string type = split[2];
                            player.CreateTeam(name, capacity, type);
                        }
                    }
                    break;
                case 0x25: // Join Team
                    {
                        if (split.Length == 1)
                        {
                            string name = split[0];
                            player.JoinTeam(name);
                        }
                    }
                    break;
                case 0x3C: // Leave Lobby
                    player.LeaveLobby();
                    break;
                case 0x2a:
                case 0x2b:
                case 0x2c:
                    Program.Log.Debug(BitConverter.ToString(packet.Data));
                    break;
                default:
                    break;
            }
        }

        #region QueueRelated

        public void StartProcessLoop()
        {
            if (!ProcessThread.IsAlive)
                ProcessThread.Start();
        }

        public void StopProcessLoop()
        {
            ProcessThread.Abort();
        }

        public void Queue(Player conn, Packet.Incoming packet)
        {
            lock (ProcessQueue)
            {
                ProcessQueue.Enqueue(new Tuple<Player, Packet.Incoming>(conn, packet));
            }
        }

        public void Clear()
        {
            lock (ProcessQueue)
            {
                ProcessQueue.Clear();
            }
        }

        private void ProcessLoop()
        {
            Program.Log.Info($"Process loop has started");

            while (ProcessThread.IsAlive)
            {
                bool isEmpty = false;
                lock (ProcessQueue)
                {
                    if (ProcessQueue.Count == 0)
                        isEmpty = true;
                }

                if (isEmpty)
                {
                    Thread.Yield();
                    continue;
                }

                lock (ProcessQueue)
                {
                    while (ProcessQueue.Count > 0)
                    {
                        Tuple<Player, Packet.Incoming> next;
                        ProcessQueue.TryDequeue(out next);
                        if (next == null)
                            break;

                        HandlePacket(next.Item1, next.Item2);
                    }
                }
            }
        }

        #endregion
    }
}