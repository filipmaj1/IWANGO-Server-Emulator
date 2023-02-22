using System;
using System.IO;
using System.Text;

namespace IWANGOEmulator.LobbyServer
{
    class Packet
    {
        public static byte[] CreatePlayerPacket(byte[] sharedMemBytes, byte[] ipBytes, string stringData)
        {
            byte[] stringBytes = Encoding.ASCII.GetBytes(stringData);
            byte[] data = new byte[1 + stringBytes.Length + 1 + sharedMemBytes.Length + 4];
            byte strLen = (byte)stringBytes.Length;

            data[0] = strLen;
            Array.Copy(stringBytes, 0, data, 1, strLen);
            data[1 + strLen] = 1;
            Array.Copy(sharedMemBytes, 0, data, 1 + strLen + 1, sharedMemBytes.Length);
            Array.Copy(ipBytes, 0, data, 1 + strLen + 1 + sharedMemBytes.Length, 4);

            return data;
        }

        public static byte[] CreateSharedMemPacket(byte[] sharedMemBytes, string stringData)
        {
            byte[] stringBytes = Encoding.ASCII.GetBytes(stringData);
            byte[] data = new byte[1 + stringBytes.Length + sharedMemBytes.Length];
            byte strLen = (byte)stringBytes.Length;

            data[0] = strLen;
            Array.Copy(stringBytes, 0, data, 1, strLen);
            Array.Copy(sharedMemBytes, 0, data, 1 + strLen, sharedMemBytes.Length);

            return data;
        }
    }
}
