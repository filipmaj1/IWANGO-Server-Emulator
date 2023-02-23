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
        public Player Host;

        public Team(Lobby parentLobby, string name, ushort capacity, Player host)
        {
            Parent = parentLobby;
            Name = name;
            MaxCapacity = capacity;
            Host = host;
            Members.Add(host);
            host.CurrentTeam = this;
        }

        public void SetSharedMem(string memAsStr)
        {
            SharedMem = memAsStr;
            foreach (Player player in Members)
                player.Send(0x34, $"{Name} {SharedMem}");
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
                    foreach (Player p in player.CurrentLobby.Members)
                        p.Send(0x29, $"{Name} {players.Substring(1)}");
                }                
            }
        }

        public void RemovePlayer(Player player)
        {
            lock (Members)
            {
                Members.Remove(player);

                // Change host
                if (Host.Equals(player) && Members.Count > 0)
                    Host = Members[0];

                // Send Packets
                foreach (Player p in player.CurrentLobby.Members)
                    p.Send(0x3B, $"{Name} {player.Name}");

                // Team deleted?
                if (Members.Count == 0)
                    Parent.DeleteTeam(this);
            }
        }

        public void SendChat(string from, string message)
        {
            foreach (Player player in Members)
                player.Send(0x43, $"{from} {message}");
        }

        public void SendSharedMemPlayer(Player owner, byte[] data)
        {
            foreach (Player player in Members)
                player.Send(0x42, Packet.CreateSharedMemPacket(data, $"{owner.Name}"));
        }

        public void SendGameServer(Player p)
        {
            foreach (Player player in Members)
                player.Send(0x3d, $"{Program.GetServer().GetIp()} {Program.GetServer().GetPort()}");
        }

        public void LaunchGame(Player p)
        {
            string data = $"{NumPlayers}";
            foreach (Player player in Members)
                data += $" {(Host.Equals(player) ? "*" : "")}{player.Name} {player.GetIp()}";

            p.Send(0x3e, data);
        }
    }
}
