
using System;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace IWANGOEmulator.LobbyServer.Models
{
    class Player
    {
        // Connection
        private Socket Socket;
        private Server Server;
        private bool Disconnected = false;

        // Traffic Buffer
        private byte[] ReceiveBuffer = new byte[0xFFFF];
        private int ReceiveLength = 0;
        private int ReceiveParsed = 0;

        // Player Info
        public string Name = "";
        public uint Flags = 0;
        public byte[] SharedMem = new byte[0x1E];

        public Lobby CurrentLobby = null;
        public Team CurrentTeam = null;
        public Game CurrentGame = null;

        public Player(Server server, Socket socket)
        {
            Server = server;
            Socket = socket;
        }

        public string GetIp() => ((IPEndPoint)Socket.RemoteEndPoint).Address.ToString();

        public uint GetIpUInt32()
        {
            byte[] ipBytes = IPAddress.Parse(GetIp()).GetAddressBytes();
            Array.Reverse(ipBytes);
            return BitConverter.ToUInt32(ipBytes, 0);
        }

        public byte[] GetIpBytes()
        {
            return IPAddress.Parse(GetIp()).GetAddressBytes();
        }

        public int GetPort() => ((IPEndPoint)Socket.RemoteEndPoint).Port;

        public void Disconnect(bool sendDCPacket = true)
        {
            if (Disconnected)
                return;

            Disconnected = true;

            // Tell client to d/c if actually still connected
            if (sendDCPacket)
                Send(0x17);

            // Remove player from everything
            if (CurrentTeam != null)
                CurrentTeam.RemovePlayer(this);
            if (CurrentLobby != null)
                CurrentLobby.RemovePlayer(this);

            // Close if need be
            if (Socket.Available != 0)
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Disconnect(false);
            }
        }

        public void SetName(string name)
        {
            Name = name;
        }

        public void SetGame(Game game)
        {
            CurrentGame = game;
        }

        public void SetSharedMem(byte[] data)
        {
            if (data.Length == 0x1E)
            {
                Array.Copy(data, SharedMem, 0x1E);
                if (CurrentTeam != null)
                    CurrentTeam.SendSharedMemPlayer(this, SharedMem);
            }
        }

        public byte[] GetSendDataPacket()
        {
            string strData = $"{(CurrentLobby != null ? CurrentLobby.Name : "#")} {(CurrentTeam != null && CurrentTeam.Host.Equals(this) ? "*" : "")}{Name} {Flags} {(CurrentTeam != null ? "*" + CurrentTeam.Name : "#")} {(CurrentGame != null ? "*" + CurrentGame.Name : "#")}";

            byte[] stringBytes = System.Text.Encoding.ASCII.GetBytes(strData);
            byte[] data = new byte[1 + stringBytes.Length + 1 + SharedMem.Length + 4];
            byte strLen = (byte)stringBytes.Length;

            data[0] = strLen;
            Array.Copy(stringBytes, 0, data, 1, strLen);
            data[1 + strLen] = 1;
            Array.Copy(SharedMem, 0, data, 1 + strLen + 1, SharedMem.Length);
            Array.Copy(GetIpBytes(), 0, data, 1 + strLen + 1 + SharedMem.Length, 4);

            return data;
        }

        public void JoinLobby(Lobby lobby)
        {
            if (lobby != null)
            {
                CurrentLobby = lobby;
                lobby.AddPlayer(this);
            }
            else
            {
                // Some Error
            }
        }

        public void LeaveLobby()
        {
            if (CurrentLobby != null)
            {
                CurrentLobby.RemovePlayer(this);
                CurrentLobby = null;
            }
            else
            {
                // Some Error
            }
        }
        
        public void CreateTeam(string name, ushort capacity, string type)
        {
            if (CurrentLobby != null)
            {
                if (CurrentLobby.GetTeam(name) == null)
                {
                    Team newTeam = CurrentLobby.CreateTeam(this, name, capacity, type);
                    CurrentTeam = newTeam;
                    return;
                }
                else
                    Send(0x03); // Name already in use                
            }
            else
            {
                Send(0x03);
            }
        }

        public void JoinTeam(string name)
        {
            if (CurrentLobby != null)
            {
                Team team = CurrentLobby.GetTeam(name);
                if (team == null)
                {
                    return; // Some Error, team didn't exist
                }

                if (team.NumPlayers >= team.MaxCapacity)
                {
                    return;
                }

                CurrentTeam = team;
                team.AddPlayer(this);
            }
            else
            {
                // Some Error
            }
        }

        public void LeaveTeam()
        {
            if (CurrentLobby != null && CurrentTeam != null)
            {
                CurrentTeam.RemovePlayer(this);
                CurrentTeam = null;
            }
            else
            {
                // Some Error
            }
        }

        public void SendExtraMem(byte[] extraMem, int offset, int length)
        {
            if (extraMem.Length < offset + length)
                return;

            byte[] payload = new byte[length - offset + 2];
            BitConverter.TryWriteBytes(payload, (ushort)length);
            Array.Copy(extraMem, offset, payload, 2, length);

            Send(0x50);
            Send(0x51, payload);
            Send(0x52);
        }

        #region Socket and Packet Handling
        public int ReceiveData()
        {
            int received = Socket.Receive(ReceiveBuffer, ReceiveLength, ReceiveBuffer.Length - ReceiveLength, SocketFlags.None);
            ReceiveLength += received;
            return received;
        }

        public void FinishReceive()
        {
            Array.Copy(ReceiveBuffer, ReceiveParsed, ReceiveBuffer, 0, ReceiveLength - ReceiveParsed);
            ReceiveLength -= ReceiveParsed;
            ReceiveParsed = 0;
        }

        public int Send(ushort opcode, byte[] payload = null)
        {
            byte[] data = MakePacket(opcode, payload);

            try
            {
                return Socket.Send(data);
            }
            catch (SocketException)
            {
                return 0;
            }
        }

        public int Send(ushort opcode, string payload)
        {
            byte[] payloadBytes = System.Text.Encoding.ASCII.GetBytes(payload);
            return Send(opcode, payloadBytes);
        }

        public int DebugSendPacket(byte[] data)
        {
            return Socket.Send(data);
        }

        public int GetPacket(out ushort outOpcode, out byte[] outPayload)
        {
            int bytesParsed = ParsePacket(ReceiveBuffer, ReceiveParsed, ReceiveLength, out ushort opcode, out byte[] payload);
            ReceiveParsed += bytesParsed;
            outOpcode = opcode;
            outPayload = payload;
            return bytesParsed;
        }

        private int ParsePacket(byte[] data, int offset, int maxLength, out ushort outOpcode, out byte[] outPayload)
        {
            outOpcode = 0;
            outPayload = null;

            // Is there a header?
            if (maxLength - offset < 0xA)
                return 0;

            // Get Size
            ushort payloadSize = (ushort)(BitConverter.ToUInt16(data, offset) - 8);

            // Is the full packet here?
            if (maxLength - offset - 0xA < payloadSize)
                return 0;

            // Get ??? and Sequence
            ushort unk1 = BitConverter.ToUInt16(data, offset + 2);
            ushort sequence = BitConverter.ToUInt16(data, offset + 4);
            ushort unk2 = BitConverter.ToUInt16(data, offset + 6);

            // Get Opcode
            ushort opcode = BitConverter.ToUInt16(data, offset + 8);

            // Get Payload
            byte[] payload = new byte[payloadSize];
            Array.Copy(data, offset + 0xA, payload, 0, payloadSize);

            // Done!
            outOpcode = opcode;
            outPayload = payload;
            return payloadSize + 0xA;
        }

        private byte[] MakePacket(ushort opcode, byte[] payload)
        {
            int size = (payload?.Length ?? 0) + 0x2;
            byte[] result = new byte[(payload?.Length ?? 0) + 0x4];

            // Size
            result[0] = (byte)(size & 0xFF);
            result[1] = (byte)((size >> 0x8) & 0xFF);

            // Opcode
            result[2] = (byte)(opcode & 0xFF);
            result[3] = (byte)((opcode >> 0x8) & 0xFF);

            // Payload
            if (payload != null)
                Array.Copy(payload, 0, result, 4, payload.Length);

            return result;
        }
        #endregion

        public override string ToString()
        {
            string lobbyData = "";
            string teamData = "";

            if (CurrentLobby != null)
                lobbyData = $"[Lobby:{CurrentLobby.Name}] ";
            if (CurrentTeam != null)
                teamData = $"[Team:{CurrentTeam.Name}]";

            return $"{Name}@{GetIp()} {lobbyData} {teamData}";
        }
    }
}
