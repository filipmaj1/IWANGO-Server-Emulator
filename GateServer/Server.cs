using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IWANGOEmulator.GateServer
{
    class Server
    {
        public const int PORT = 9500;
        public const int BUFFER_SIZE = 0x200;
        public const int BACKLOG = 100;       

        private enum HandleError { ERROR1, NAME_IN_USE1, NAME_IN_USE2, ERROR2}

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
                    // Something is going wrong, buffer full, gtfo.
                    if (conn.lastPartialSize >= conn.buffer.Length)
                    {
                        conn.Disconnect();
                    }

                    // Grab data and process if correct. Disconnect after.
                    ushort size = BitConverter.ToUInt16(conn.buffer, 0);                    
                    if (size <= conn.lastPartialSize + bytesRead)
                    {
                        string payload = Encoding.ASCII.GetString(conn.buffer, 2, size);
                        Program.Log.Info($"Request: \"{payload}\" from {conn}");
                        ProcessRequest(conn, payload);
                        //conn.Disconnect();
                    }

                    // If somehow not enough data, get more.
                    if (conn.socket.Connected)
                        conn.socket.BeginReceive(conn.buffer, conn.lastPartialSize, conn.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), conn);
                    else
                    {
                        lock (connList)
                        {
                            conn.Disconnect();
                            connList.Remove(conn);
                        }
                    }
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

        //What is 0x3F6 and 0x3FF for?

        private void ProcessRequest(ClientConnection conn, string request)
        {
            string[] split = request.Split(' ');

            if (split.Length == 0)
                return;

            switch (split[0])
            {
                case "REQUEST_FILTER":
                    {
                        if (split.Length < 2)
                        {
                            SendError(conn, HandleError.ERROR1);
                            return;
                        }

                        //List<string> lobbyServers = Database.GetLobbyServers(commodityId);
                        conn.Send(Packet.Create(0x3E8));
                        //foreach (string serverString in lobbyServers)
                        //    conn.Send(Packet.Create(0x3E9, serverString));
                        conn.Send(Packet.Create(0x3E9, "LobbyServer1 10.0.0.249 9501 1"));
                        conn.Send(Packet.Create(0x3EA));
                        break;
                    }
                case "HANDLE_LIST_GET":
                    {
                        if (split.Length < 4)
                        {
                            SendError(conn, HandleError.ERROR1);
                            return;
                        }

                        /*
                        List<string> handles = Database.GetHandles(split[1]);
                        StringBuilder builder = new StringBuilder();
                        for (int i = 0; i < handles.Count; i++)
                            builder.Append($"{i+1}{handles[i]} ");
                        conn.Send(Packet.Create(0x3F2, builder.ToString()));
                        */

                        conn.Send(Packet.Create(0x3F2, "1Ioncannon 2Someguy"));
                        break;
                    }
                case "HANDLE_ADD":
                    {
                        if (split.Length < 5)
                        {
                            SendError(conn, HandleError.ERROR1);
                            return;
                        }

                        /*
                        int result = Database.CreateHandle(split[1], split[4]);
                        if (result == 0)
                            conn.Send(Packet.Create(0x3F3, $"1 {split[4]}"));
                        else if (result == -1)
                            SendError(conn, HandleError.NAME_IN_USE1);
                        else if (result == -2)
                            SendError(conn, HandleError.ERROR1);
                        */
                        conn.Send(Packet.Create(0x3F3, "0 TEST"));
                        break;
                    }
                case "HANDLE_REPLACE":
                    {
                        if (split.Length < 5)
                        {
                            SendError(conn, HandleError.ERROR1);
                            return;
                        }

                        /*
                        int result = Database.ReplaceHandle(split[1], split[3], split[4]);
                        if (result == 0)
                            conn.Send(Packet.Create(0x3F4, $"1 {split[4]}"));
                        else if (result == -1)
                            SendError(conn, HandleError.NAME_IN_USE1);
                        else if (result == -2)
                            SendError(conn, HandleError.ERROR1);
                        */

                        conn.Send(Packet.Create(0x3F4));
                        break;
                    }
                case "HANDLE_DELETE":
                    {
                        if (split.Length < 5)
                        {
                            SendError(conn, HandleError.ERROR1);
                            return;
                        }
                        
                        /*                    
                        if (Database.DeleteHandle(split[1], split[3]))
                            conn.Send(Packet.Create(0x3F5));
                        else
                            SendError(conn, HandleError.ERROR1);
                        */

                        conn.Send(Packet.Create(0x3F5, "1 TEST"));
                        break;
                    }
            }
        }

        private void SendError(ClientConnection conn, HandleError type)
        {
            switch (type)
            {
                case HandleError.ERROR1:
                    conn.Send(Packet.Create(0x3FC));
                    return;
                case HandleError.NAME_IN_USE1:
                    conn.Send(Packet.Create(0x3FD));
                    return;
                case HandleError.NAME_IN_USE2:
                    conn.Send(Packet.Create(0x3FE));
                    return;
                case HandleError.ERROR2:
                    conn.Send(Packet.Create(0x3FF));
                    return;
            }
        }
    }
}
