using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static IWANGOEmulator.LobbyServer.Packet;

namespace IWANGOEmulator.LobbyServer.Models
{
    class Lobby
    {
        public readonly string Name;
        public uint Flags;
        public bool HasSharedMem = false;
        public string SharedMem;
        public readonly Game Game;
        public int NumPlayers { get => Members.Count; }
        public readonly ushort MaxCapacity;
        public readonly List<Player> Members = new List<Player>();
        public readonly List<Team> Teams = new List<Team>();

        public Lobby(Game parentGame, string name, ushort capacity)
        {
            Name = name;
            MaxCapacity = capacity;
            Game = parentGame;
        }

        public void AddPlayer(Player player)
        {
            lock (Members)
            {
                Members.Add(player);

                // Confirm Join Lobby
                player.Send(0x13, $"{Name} {player.Name}");

                // Send player info to all members
                foreach (Player p in Members)
                {
                    if (p.Equals(player))
                        continue;
                    p.Send(0x30, player.GetSendDataPacket());
                }    
            }
        }

        public void RemovePlayer(Player player)
        {
            lock (Members)
            {
                // Remove player from list
                Members.Remove(player);

                // Confirm Leave Lobby
                player.Send(0xCB);

                // Tell all members to remove the player
                foreach (Player p in Members)
                    p.Send(0x2C, $"{player.Name}");
            }
        }

        public void SendChat(string from, string message)
        {
            foreach (Player player in Members)
                player.Send(0x2D, $"{from} {message}");
        }

        public Team CreateTeam(Player creator, string teamName, ushort capacity, string type)
        {
            Team team = new Team(this, teamName, capacity, creator);
            creator.Send(0x28, $"{team.Name} {creator.Name} {capacity} 0 {Game.Name}");
            Teams.Add(team);
            return team;
        }

        public void DeleteTeam(Team team)
        {
            if (Teams.Contains(team))
                Teams.Remove(team);

            // Tell all members to remove team
            foreach (Player p in Members)
                p.Send(0x3A, $"{team.Name}");
        }

        public Team GetTeam(string teamName)
        {
            return Teams.SingleOrDefault(x => x.Name.Equals(teamName));
        }
    }
}
