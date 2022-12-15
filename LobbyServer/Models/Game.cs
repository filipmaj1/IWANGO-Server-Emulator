using System;
using System.Collections.Generic;
using System.Text;

namespace IWANGOEmulator.LobbyServer.Models
{
    class Game
    {
        public string Name { get; }
        public readonly int UnkVal;

        public Game(string gameName)
        {
            Name = gameName;
        }

    }
}
