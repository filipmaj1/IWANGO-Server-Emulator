using System;
using System.Collections.Generic;
using System.Text;

namespace IWANGOEmulator.LobbyServer.Models
{
    class Team
    {
        // Team Info
        public readonly string Name;
        public uint Flags = 0;
        public string SharedMem = "";
        public int NumPlayers { get => Members.Count; }
        public readonly uint MaxCapacity;

        private Lobby Parent;
        public readonly List<Player> Members = new List<Player>();
        public readonly Player Host;

        public Team(Lobby parentLobby, string name, ushort capacity, Player host)
        {
            Parent = parentLobby;
            Name = name;
            MaxCapacity = capacity;
            Host = host;
            Members.Add(host);
        }

        public void SetSharedMem(string memAsStr)
        {
            SharedMem = memAsStr;
            foreach (Player player in Members)
                player.Send(new Packet.Outgoing(0x34, $"{Name} {SharedMem}"));
        }

        public void AddPlayer(Player player)
        {
            lock (Members)
            {
                if (Members.Count < MaxCapacity)
                {
                    Members.Add(player);

                    // Build player string
                    string players = "";
                    foreach (Player p in Members)
                        players += $" {p.Name}";

                    // Send packet to all members
                    foreach (Player p in Members)
                        p.Send(new Packet.Outgoing(0x29, $"{Name} {players.Substring(1)}"));
                }                
            }
        }

        public void RemovePlayer(Player player)
        {
            lock (Members)
            {
                Members.Remove(player);

                // Send Packets
                player.Send(new Packet.Outgoing(0x3B, $"{Name} {player.Name}"));
                foreach (Player p in Members)
                    p.Send(new Packet.Outgoing(0x3B, $"{Name} {player.Name}"));

                // Delete Team if 0 members
                if (NumPlayers == 0)
                    Parent.DeleteTeam(this);
            }
        }

        public void SendChat(string from, string message)
        {
            foreach (Player player in Members)
                player.Send(new Packet.Outgoing(0x43, $"{from} {message}"));
        }

        public void SendSharedMemPlayer(Player owner, byte[] data)
        {
            foreach (Player player in Members)
                player.Send(Packet.Outgoing.CreateSharedMemPacket(0x42, data, $"{owner.Name}"));
        }

        public void SendGameServer()
        {
            foreach (Player player in Members)
                player.Send(new Packet.Outgoing(0x3d, $"{Server.GAMESERVER_IP} {Server.GAMESERVER_PORT}"));
        }

        public void LaunchGame()
        {
            string data = $"{NumPlayers}";
            foreach (Player player in Members)
                data += $" {(Host.Equals(player) ? "*" : "")}{player.Name} {player.GetIpString()}";

            foreach (Player player in Members)
                player.Send(new Packet.Outgoing(0x3e, data));
        }
    }
}
