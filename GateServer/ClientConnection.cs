using System;
using System.Net;
using System.Net.Sockets;

namespace IWANGOEmulator.GateServer
{
    class ClientConnection
    {
        //Connection stuff
        public Socket socket;
        public byte[] buffer = new byte[0xffff];
        public int lastPartialSize = 0;

        public void Send(byte[] packetBytes)
        {
            try
            {
                socket.Send(packetBytes);
            } catch (Exception _)
            {
                Disconnect();
            }
        }

        public String GetAddress()
        {
            return String.Format("{0}:{1}", (socket.RemoteEndPoint as IPEndPoint).Address, (socket.RemoteEndPoint as IPEndPoint).Port);
        }

        public void Disconnect()
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Disconnect(false);
        }

        public override string ToString()
        {
            return GetAddress();
        }
    }
}
