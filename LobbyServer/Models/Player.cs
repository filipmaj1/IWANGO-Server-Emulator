
using System;
using System.Net;
using System.Net.Sockets;

namespace IWANGOEmulator.LobbyServer.Models
{
    class Player
    {
        // Connection stuff
        public Socket socket;
        public Socket pingSocket;
        public byte[] buffer = new byte[0xffff];
        public byte[] pingBuffer = new byte[0x100];
        public int lastPartialSize = 0;

        // Player Info
        public string Name = "";
        public uint Flags = 0;
        public byte[] SharedMem = new byte[0x1E];

        public Lobby CurrentLobby = null;
        public Team CurrentTeam = null;
        public Game CurrentGame = null;

        public void Send(Packet.Outgoing packet)
        {
            Send(packet.GetBytes());

#if DEBUG
            Program.Log.Debug(packet.ToString());
#endif
        }

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

        public void SendPing(bool withData = false, int playerKey = 0, int time = 0)
        {
            IPEndPoint endPoint = new IPEndPoint((socket.RemoteEndPoint as IPEndPoint).Address, 50000);

            if (withData)
            {
                pingSocket.SendTo(BitConverter.GetBytes((int)1), endPoint);
                pingSocket.SendTo(BitConverter.GetBytes((int)playerKey), endPoint);
                pingSocket.SendTo(BitConverter.GetBytes((int)time), endPoint);
            }
            else
            {
                EndPoint receiveEP = (EndPoint)endPoint;
                byte[] received = new byte[0xC];
                pingSocket.SendTo(received, endPoint);
                pingSocket.ReceiveFrom(received, ref receiveEP);
            }
        }

        public String GetAddress()
        {
            return String.Format("{0}:{1}", (socket.RemoteEndPoint as IPEndPoint).Address, (socket.RemoteEndPoint as IPEndPoint).Port);
        }

        public byte[] GetIpBytes()
        {
            return (socket.RemoteEndPoint as IPEndPoint).Address.GetAddressBytes();
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

        public Packet.Outgoing GetSendDataPacket()
        {
            uint teamPos = 0;
            if (CurrentTeam != null)
                teamPos = (uint)CurrentTeam.NumPlayers;
            string strData = $"{(CurrentLobby != null ? CurrentLobby.Name : "#")} {(CurrentTeam != null && CurrentTeam.Host.Equals(this) ? "*" : "")}{Name} {Flags} {(CurrentTeam != null ? "*" + CurrentTeam.Name : "#")} {(CurrentGame != null ? "*" + CurrentGame.Name : "#")}";
            return Packet.Outgoing.CreatePlayerPacket(0x30, SharedMem, GetIpBytes(), strData);
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
                }
                else
                {
                    // Name taken
                }
            }
            else
            {
                // Some Error
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
    }
}
