using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IWANGOEmulator.GameServer
{
    class Server
    {
        public const int PORT = 9502;
        public const int BUFFER_SIZE = 0x200;
        public const int BACKLOG = 100;

        private Socket serverSocket;
        private List<ClientConnection> connList = new List<ClientConnection>();

        public Server()
        {
        }

        #region Socket Handling
        public bool StartServer()
        {
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), PORT);

            try
            {
                serverSocket = new Socket(serverEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(serverEndPoint);
                serverSocket.Listen(BACKLOG);
                serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), serverSocket);
            }
            catch (Exception e)
            {
                Program.Log.Error("Could not create server socket: " + e.Message);
            }

            Program.Log.Info("Server has started @ {0}:{1}", (serverSocket.LocalEndPoint as IPEndPoint).Address, (serverSocket.LocalEndPoint as IPEndPoint).Port);

            return true;
        }

        public void SendAll(byte[] data)
        {
            foreach (ClientConnection conn in connList)
            {
                conn.Send(data);
            }
        }

        private void AcceptCallback(IAsyncResult result)
        {
            ClientConnection conn = null;
            try
            {
                Socket s = (Socket)result.AsyncState;
                conn = new ClientConnection
                {
                    socket = s.EndAccept(result),
                    buffer = new byte[BUFFER_SIZE]
                };
                lock (connList)
                {
                    connList.Add(conn);
                    Program.Log.Info($"Got a connection from {conn.GetAddress()}");
                }

                conn.socket.BeginReceive(conn.buffer, 0, conn.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), conn);
            }
            catch (SocketException)
            {
                if (conn.socket != null)
                {
                    conn.socket.Close();
                    lock (connList)
                    {
                        connList.Remove(conn);
                    }
                }
            }
            catch (Exception)
            {
                if (conn.socket != null)
                {
                    conn.socket.Close();
                    lock (connList)
                    {
                        connList.Remove(conn);
                    }
                }
            }

            serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), serverSocket);
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            ClientConnection conn = (ClientConnection)result.AsyncState;

            try
            {
                int bytesRead = conn.socket.EndReceive(result);
                bytesRead += conn.lastPartialSize;

                if (bytesRead > 0)
                {
                    Program.Log.Info($"Read {bytesRead} bytes");
                }
            }
            catch (SocketException)
            {
                if (conn.socket != null)
                {
                    Program.Log.Info("{0} has disconnected.", conn.GetAddress());

                    lock (connList)
                    {
                        connList.Remove(conn);
                    }
                }
            }
        }

        #endregion
    }
}
