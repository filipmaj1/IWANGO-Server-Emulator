using IWANGOEmulator.LobbyServer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace IWANGOEmulator.LobbyServer
{
    class Server
    {
        public const int PORT = 9501;
        public const int BUFFER_SIZE = 0x200;
        public const int BACKLOG = 100;

        public const string GAMESERVER_IP = "192.168.0.249";
        public const ushort GAMESERVER_PORT = 9502;

        private Socket ServerSocket;
        private readonly PacketProcessor PacketProcessor;

        private readonly List<Game> GameList = new List<Game>();
        private readonly List<Player> PlayerList = new List<Player>();
        private readonly List<Lobby> LobbyList = new List<Lobby>();

        public Server()
        {
            PacketProcessor = new PacketProcessor(this);
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
            return PlayerList.SingleOrDefault(x => x.Name.Equals(playerName));
        }

        #endregion

        #region Socket Handling
        public bool StartServer()
        {
            PacketProcessor.StartProcessLoop();

            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), PORT);
            try
            {
                ServerSocket = new Socket(serverEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                ServerSocket.Bind(serverEndPoint);
                ServerSocket.Listen(BACKLOG);
                ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), ServerSocket);
            }            
            catch (Exception e)
            {
                Program.Log.Error("Could not create server socket: " + e.Message);
            }

            Program.Log.Info("Server has started @ {0}:{1}", (ServerSocket.LocalEndPoint as IPEndPoint).Address, (ServerSocket.LocalEndPoint as IPEndPoint).Port);

            return true;
        }

        public void SendAll(byte[] data)
        {
            foreach (Player conn in PlayerList)
            {
                conn.Send(data);
            }
        }

        public void SendAll(ushort opcode, string data)
        {
            foreach (Player conn in PlayerList)
            {
                conn.Send(new Packet.Outgoing(opcode, data));
            }
        }

        private void AcceptCallback(IAsyncResult result)
        {
            Player conn = null;
            try
            {
                Socket s = (Socket)result.AsyncState;
                conn = new Player
                {
                    socket = s.EndAccept(result),
                    buffer = new byte[BUFFER_SIZE],
                };
                lock (PlayerList)
                {
                    PlayerList.Add(conn);
                }

                conn.socket.BeginReceive(conn.buffer, 0, conn.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), conn);
            }
            catch (SocketException)
            {
                if (conn.socket != null)
                {
                    conn.socket.Close();
                    lock (PlayerList)
                    {
                        PlayerList.Remove(conn);
                    }
                }
            }
            catch (Exception)
            {
                if (conn.socket != null)
                {
                    conn.socket.Close();
                    lock (PlayerList)
                    {
                        PlayerList.Remove(conn);
                    }
                }
            }

            ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), ServerSocket);
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            Player conn = (Player)result.AsyncState;

            try
            {
                int bytesRead = conn.socket.EndReceive(result);
                conn.lastPartialSize += bytesRead;

                if (bytesRead > 0)
                {
                    // Remove if disconnected
                    if (!conn.socket.Connected)
                    {
                        lock (PlayerList)
                        {
                            conn.Disconnect();
                            PlayerList.Remove(conn);
                            return;
                        }
                    }

                    // Something is going wrong, buffer full, gtfo.
                    if (conn.lastPartialSize >= conn.buffer.Length)
                    {
                        conn.Disconnect();
                        Program.Log.Info("{0} has disconnected due to full buffer.", conn.GetAddress());
                    }

                    // Try to process as many packets as possible
                    int bytesParsed = 0;
                    using MemoryStream memStream = new MemoryStream(conn.buffer);
                    using BinaryReader binReader = new BinaryReader(memStream);
                    while (true)
                    {
                        Packet.Incoming packet = Packet.Incoming.Read(binReader, conn.lastPartialSize, ref bytesParsed);
                        if (packet != null)
                        {
                            PacketProcessor.Queue(conn, packet);
                        }
                        else
                        {
                            Array.Copy(conn.buffer, bytesParsed, conn.buffer, 0, conn.lastPartialSize - bytesParsed);
                            conn.lastPartialSize -= bytesParsed;
                            break;
                        }
                    }                  

                    conn.socket.BeginReceive(conn.buffer, conn.lastPartialSize, conn.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), conn);
                }
            }
            catch (SocketException)
            {
                if (conn.socket != null)
                {
                    Program.Log.Info("{0} has disconnected.", conn.GetAddress());

                    lock (PlayerList)
                    {
                        PlayerList.Remove(conn);
                    }
                }
            }
        }

        #endregion
    }
}
