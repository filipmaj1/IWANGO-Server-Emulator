using IWANGOEmulator.LobbyServer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace IWANGOEmulator.LobbyServer
{
    class Server
    {
        private const int BACKLOG = 100;

        private Socket ServerSocket;
        private readonly string ServerName;
        private readonly string ServerIp;
        private readonly ushort ServerPort;
        private bool IsAlive = false;
        private Thread ServerLoopThread;

        private readonly Dictionary<Socket, Player> Clients = new Dictionary<Socket, Player>();
        private readonly PacketProcessor PacketProcessor;

        private readonly List<Game> GameList = new List<Game>();
        private readonly List<Lobby> LobbyList = new List<Lobby>();

        public Server(string name, string ip, ushort port)
        {
            ServerName = name;
            ServerIp = ip;
            ServerPort = port;
            PacketProcessor = new PacketProcessor(this);
        }

        public string GetName()
        {
            return ServerName;
        }

        public string GetIp()
        {
            return ServerIp;
        }

        public ushort GetPort()
        {
            return ServerPort;
        }

        #region Lobby Server 

        public void AddGame(string gameName)
        {
            GameList.Add(new Game(gameName));
        }

        public Game[] GetGames()
        {
            return GameList.ToArray();
        }

        public Game GetGame(string gameName)
        {
            return GameList.SingleOrDefault(x => x.Name.Equals(gameName));
        }

        public Lobby CreateLobby(string lobbyName, ushort maxCapacity)
        {
            if (GetLobby(lobbyName) == null && maxCapacity > 0)
            {
                Lobby lobby = new Lobby(GetGames()[0], lobbyName, maxCapacity);
                LobbyList.Add(lobby);
                return lobby;
            }

            return null;
        }

        public void DeleteLobby(string lobbyName)
        {
            Lobby toRemove = LobbyList.SingleOrDefault(x => x.Name.Equals(lobbyName));
            if (toRemove != null)
                LobbyList.Remove(toRemove);
        }

        public Lobby GetLobby(string lobbyName)
        {
            return LobbyList.SingleOrDefault(x => x.Name.Equals(lobbyName));
        }

        public Lobby[] GetLobbyList()
        {
            return LobbyList.ToArray();
        }

        public Player GetPlayer(string playerName)
        {
            return Clients.Values.SingleOrDefault(x => x.Name.Equals(playerName));
        }

        public Player IsIPUnique(Player me)
        {
            return Clients.Values.SingleOrDefault(x => x.GetIp().Equals(me.GetIp()) && !x.Equals(me));
        }

        #endregion

        #region Socket Handling
        public int StartServer()
        {
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(ServerIp), ServerPort);
            try
            {
                ServerSocket = new Socket(serverEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                ServerSocket.Bind(serverEndPoint);
                ServerSocket.Listen(BACKLOG);

                Program.Log.Info("Server socket created...");
            }
            catch (Exception)
            {
                Program.Log.Error($"There was an issue binding port {ServerPort}.");
                return -1;
            }

            IsAlive = true;

            ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), ServerSocket);

            ServerLoopThread = new Thread(MainLoop);
            ServerLoopThread.Start();

            Program.Log.Info("Server has started @ {0}:{1}", (ServerSocket.LocalEndPoint as IPEndPoint).Address, (ServerSocket.LocalEndPoint as IPEndPoint).Port);
            return 0;
        }

        public void StopServer()
        {
            IsAlive = false;
        }

        public void DebugSendToAll(byte[] data)
        {
            foreach (Player client in Clients.Values)
            {
                client.DebugSendPacket(data);
            }
        }

        private void AcceptCallback(IAsyncResult result)
        {
            Socket serverSocket = (Socket)result.AsyncState;
            Socket s = serverSocket.EndAccept(result);
            Player client = new Player(this, s);

            if (!Clients.ContainsKey(s))
                Clients.Add(s, client);
            else
            {
                Clients.Remove(s);
                Clients.Add(s, client);
            }

            serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), serverSocket);
        }

        private void MainLoop()
        {
            List<Socket> socketsToRemove = new List<Socket>();

            while (IsAlive)
            {
                var socketList = new List<Socket>(Clients.Keys);

                if (socketList.Count == 0)
                    continue;

                Socket.Select(socketList, null, null, 1000);
                foreach (Socket s in socketList)
                {
                    // Is this socket disconnected?
                    if ((s.Poll(1000, SelectMode.SelectRead) && (s.Available == 0)) || !s.Connected)
                    {
                        socketsToRemove.Add(s);
                        continue;
                    }

                    // Read socket
                    try
                    { 
                        Player client = Clients[s];

                        // Receive and process packets
                        int bytesReceived = client.ReceiveData();
                        while (true)
                        {
                            int bytesParsed = client.GetPacket(out ushort opcode, out byte[] payload);
                            if (bytesParsed == 0)
                                break;

                            if (payload != null)
                                PacketProcessor.HandlePacket(client, opcode, payload);
                            else
                            {
                                Program.Log.Info($"{client} sent a bad packet and was kicked.");
                                client.Disconnect();
                                socketsToRemove.Add(s);
                                break;
                            }
                        }
                        client.FinishReceive();
                    }
                    catch (SocketException)
                    {
                        socketsToRemove.Add(s);
                    }
                }

                // Clean up all removed sockets
                foreach (Socket s in socketsToRemove)
                {
                    if (!Clients.ContainsKey(s))
                        continue;

                    Player disconnectedClient = Clients[s];
                    disconnectedClient.Disconnect();

                    // Remove socket
                    Clients.Remove(s);
                }

                socketsToRemove.Clear();
            }

            foreach (Socket s in Clients.Keys)
                Clients[s].Disconnect();

            ServerSocket.Close(5);

            Clients.Clear();
            GC.Collect();
        }
        #endregion
    }
}
