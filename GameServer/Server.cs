using System;
using System.Net;
using System.Net.Sockets;

namespace IWANGOEmulator.GameServer
{
    class Server
    {
        public const int UDP_PORT = 0x2F2F;
        public const int BUFFER_SIZE = 0x200;
        public const int BACKLOG = 100;

        private Socket UdpSocket;
        private EndPoint RemoteEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0x2F2F);
        private byte[] Buffer = new byte[0xFFFF];

        private Object state = new Object();

        public Server()
        {
        }

        #region Socket Handling
        public bool StartServer()
        {
            try
            {
                UdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                UdpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
                UdpSocket.Bind(new IPEndPoint(IPAddress.Parse("0.0.0.0"), UDP_PORT));
               // UdpSocket.BeginReceiveFrom(Buffer, 0, Buffer.Length, SocketFlags.None, ref RemoteEP, ReceiveCallback, state);
                return true;
            }
            catch (SocketException e)
            {
                Program.Log.Error("There was an issue setting up the socket");
            }
            return false;
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            Object conn = result.AsyncState;

            int bytes = UdpSocket.EndReceiveFrom(result, ref RemoteEP);
            UdpSocket.BeginReceiveFrom(Buffer, 0, Buffer.Length, SocketFlags.None, ref RemoteEP, ReceiveCallback, state);
            Program.Log.Info($"Received {bytes} bytes");
        }

        #endregion

        public void SendAll(byte[] data)
        {
                UdpSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, RemoteEP, (result) => {
                Object obj = result.AsyncState;
                int bytes = UdpSocket.EndSend(result);
                Program.Log.Info($"Sent {bytes} bytes");
            }, state);
        }
    }
}
