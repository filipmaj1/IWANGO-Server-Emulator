using System.IO;
using System.Text;

namespace IWANGOEmulator.GateServer
{
    class Packet
    {
        public static byte[] Create(short opcode, string data = "")
        {
            return Create(opcode, Encoding.ASCII.GetBytes(data));
        }

        public static byte[] Create(short opcode, byte[] data)
        {
            byte[] toReturn = new byte[data.Length + 4];
            using (MemoryStream memStream = new MemoryStream(toReturn))
            using (BinaryWriter writer = new BinaryWriter(memStream))
            {
                writer.Write((ushort)(data.Length + 2));
                writer.Write((ushort)opcode);
                writer.Write(data);
            }
            return toReturn;
        }
    }
}
