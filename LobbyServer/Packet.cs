using System;
using System.IO;
using System.Text;

namespace IWANGOEmulator.LobbyServer
{
    class Packet
    {
        public class Incoming
        {
            public readonly ushort Unk1;
            public readonly ushort Unk2;
            public readonly ushort Sequence;
            public readonly ushort Opcode;
            public readonly byte[] Data;

            public string DataString { get => Encoding.ASCII.GetString(Data); }

            private Incoming(ushort unk1, ushort unk2, ushort sequence, ushort opcode, byte[] data)
            {
                Unk1 = unk1;
                Unk2 = unk2;
                Sequence = sequence;
                Opcode = opcode;
                Data = data;
            }

            public static Incoming Read(BinaryReader stream, int length, ref int totalRead)
            {
                // Check if we have any data
                if (totalRead >= length)
                    return null;

                // Check if full packet is here
                long startingPos = stream.BaseStream.Position;
                ushort size = stream.ReadUInt16();
                if (startingPos + size + 2 > length)
                {
                    stream.BaseStream.Seek(startingPos, SeekOrigin.Begin);
                    return null;
                }

                // Read in rest
                ushort unk1 = stream.ReadUInt16();
                ushort sequence = stream.ReadUInt16();
                ushort unk2 = stream.ReadUInt16();
                ushort opcode = stream.ReadUInt16();
                byte[] data = stream.ReadBytes(size - 8);
                totalRead += size + 2;
                return new Incoming(unk1, unk2, sequence, opcode, data);                
            }

            public override string ToString()
            {
                if (DataString.Length == 0)
                    return $"INCOMING[0x{Opcode:X2}]";
                else
                    return $"INCOMING[0x{Opcode:X2}] - '{DataString}'";
            }
        }

        public class Outgoing
        {
            public readonly ushort Opcode;
            public readonly ushort Size;
            private readonly byte[] Packet;
            private readonly string DataString;

            public Outgoing(ushort opcode, string data = "") : this(opcode, Encoding.ASCII.GetBytes(data)) { }

            public Outgoing(ushort opcode, byte[] data)
            {
                Opcode = opcode;
                Size = (ushort)(data.Length + 4);

                Packet = new byte[data.Length + 4];
                byte[] opcodeBytes = BitConverter.GetBytes(opcode);
                byte[] sizeBytes = BitConverter.GetBytes(data.Length + 2);

                Array.Copy(sizeBytes, 0, Packet, 0, 2);
                Array.Copy(opcodeBytes, 0, Packet, 2, 2);
                Array.Copy(data, 0, Packet, 4, data.Length);

                DataString = Encoding.ASCII.GetString(data);
            }

            public static Outgoing CreatePlayerPacket(ushort opcode, byte[] sharedMemBytes, byte[] ipBytes,  string stringData)
            {
                byte[] stringBytes = Encoding.ASCII.GetBytes(stringData);
                byte[] data = new byte[1 + stringBytes.Length + 1 + sharedMemBytes.Length + 4];
                byte strLen = (byte)stringBytes.Length;

                data[0] = strLen;
                Array.Copy(stringBytes, 0, data, 1, strLen);
                data[1 + strLen] = 1;
                Array.Copy(sharedMemBytes, 0, data, 1 + strLen + 1, sharedMemBytes.Length);
                Array.Copy(ipBytes, 0, data, 1 + strLen + 1 + sharedMemBytes.Length, 4);

                return new Outgoing(opcode, data);
            }

            public static Outgoing CreateSharedMemPacket(ushort opcode, byte[] sharedMemBytes, string stringData)
            {
                byte[] stringBytes = Encoding.ASCII.GetBytes(stringData);
                byte[] data = new byte[1 + stringBytes.Length + sharedMemBytes.Length];
                byte strLen = (byte)stringBytes.Length;

                data[0] = strLen;
                Array.Copy(stringBytes, 0, data, 1, strLen);
                Array.Copy(sharedMemBytes, 0, data, 1 + strLen, sharedMemBytes.Length);

                return new Outgoing(opcode, data);
            }

            public byte[] GetBytes()
            {
                return Packet;
            }

            public override string ToString()
            {
                if (DataString.Length == 0)
                    return $"OUTGOING[0x{Opcode:X2}]";
                else
                    return $"OUTGOING[0x{Opcode:X2}] - '{DataString}'";
            }
        }
    }
}
