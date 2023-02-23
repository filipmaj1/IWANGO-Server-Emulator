using IWANGOEmulator.LobbyServer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace IWANGOEmulator.LobbyServer
{
    class PacketProcessor
    {
        private enum CLIOpcode : ushort
        {
            LOGIN               = 0x01,
            LOGIN2              = 0x02,
            SEND_LOG            = 0x03,
            ENTR_LOBBY          = 0x04,
            DISCONNECT          = 0x05,
            GET_LOBBIES         = 0x07,
            GET_GAMES           = 0x08,
            SELECT_GAME         = 0x09,
            PING                = 0x0A,
            SEARCH              = 0x0B,
            GET_LICENSE         = 0x0C,
            GET_TEAMS           = 0x0F,
            REFRESH_PLAYERS     = 0x10,
            CHAT_LOBBY          = 0x11,
            SHAREDMEM_PLAYER    = 0x1B,
            SHAREDMEM_TEAM      = 0x20,
            LEAVE_TEAM          = 0x21,
            RECONNECT           = 0x0D,
            LAUNCH_REQUEST      = 0x22,
            LAUNCH_GAME         = 0x65,
            CHAT_TEAM           = 0x23,
            CREATE_TEAM         = 0x24,
            JOIN_TEAM           = 0x25,
            LEAVE_LOBBY         = 0x3C

        }

        private enum SRVOpcode : ushort
        {

        }

        private static Server Server;
        private static readonly Dictionary<CLIOpcode, Action<Player, byte[], string>> CommandHandlers = new Dictionary<CLIOpcode, Action<Player, byte[], string>>
        {
            [CLIOpcode.LOGIN] = LoginCommand,
            [CLIOpcode.LOGIN2] = Login2Command,
            [CLIOpcode.REFRESH_PLAYERS] = RefreshPlayersCommand,
            [CLIOpcode.GET_LOBBIES] = RefreshLobbiesCommand,
            [CLIOpcode.ENTR_LOBBY] = CreateOrJoinLobby,
            [CLIOpcode.LEAVE_LOBBY] = LeaveLobbyCommand,
            [CLIOpcode.GET_TEAMS] = RefreshTeamsCommand,
            [CLIOpcode.CREATE_TEAM] = CreateTeamCommand,
            [CLIOpcode.JOIN_TEAM] = JoinTeamCommand,
            [CLIOpcode.LEAVE_TEAM] = LeaveTeamCommand,
            [CLIOpcode.GET_GAMES] = RefreshGamesCommand,
            [CLIOpcode.SELECT_GAME] = SelectGameCommand,
            [CLIOpcode.GET_LICENSE] = GetLicenseCommand,
            [CLIOpcode.CHAT_LOBBY] = ChatLobbyCommand,
            [CLIOpcode.CHAT_TEAM] = ChatTeamCommand,
            [CLIOpcode.SHAREDMEM_PLAYER] = SharedMemPlayerCommand,
            [CLIOpcode.SHAREDMEM_TEAM] = SharedMemTeamCommand,
            [CLIOpcode.PING] = PingCommand,
            [CLIOpcode.DISCONNECT] = DisconnectCommand,
            [CLIOpcode.LAUNCH_REQUEST] = LaunchRequestCommand,
            [CLIOpcode.LAUNCH_GAME] = LaunchGameCommand,
            [CLIOpcode.RECONNECT] = ReconnectCommand,
        };

        public PacketProcessor(Server server)
        {
            Server = server;
        }

        private static void LoginCommand(Player player, byte[] data, string dataAsString)
        {
            string[] split = dataAsString.Split(' ');

            // Is this handle already in the server? Handle is used as a key and HAS to be unique.
            Player exists = Server.GetPlayer(split[0]);
            if (exists != null)
            {
                exists.SetName("");
                exists.Disconnect();
            }

            // Is this IP already in the server? IP is assumed to be a WAN IP due to dial-up days. Disabled when debugging.
#if !DEBUG
                    exists = Server.IsIPUnique(player);
                    if (exists != null)
                    {
                        exists.SetName("");
                        exists.Disconnect();
                    }
#endif

            // We are good to continue
            player.SetName(split[0]);
            DateTime currentTime = DateTime.Now;
            player.Send(0x11, $"0100 0102 {currentTime.Year}:{currentTime.Month}:{currentTime.Day}:{currentTime.Hour}:{currentTime.Minute}:{currentTime.Second}");
        }

        private static void Login2Command(Player player, byte[] data, string dataAsString)
        {
            string[] split = dataAsString.Split(' ');
            player.Send(0x0C, "LOB 999 999 AAA AAA");
            player.Send(0x0A, "Welcome to IWANGO Emulator by Ioncannon");
            player.Send(0xE1);
        }

        private static void RefreshPlayersCommand(Player player, byte[] data, string dataAsString)
        {
            string[] split = dataAsString.Split(' ');
            // Get all players
            if (split[0].Length == 0)
            {
                foreach (Player p in player.CurrentLobby.Members)
                    player.Send(0x30, p.GetSendDataPacket());
            }
            // Get specific
            else
            {
                string name = split[0];
                Player p = Server.GetPlayer(name);
                if (p != null)
                {
                    player.Send(0x30, p.GetSendDataPacket());
                }
            }
            player.Send(0x31);
        }

        private static void RefreshLobbiesCommand(Player player, byte[] data, string dataAsString)
        {
            Lobby[] lobbies = Server.GetLobbyList();
            foreach (Lobby lobby in lobbies)
                player.Send(0x18, $"{lobby.Name} {lobby.NumPlayers} {lobby.MaxCapacity} {lobby.Flags} {(lobby.HasSharedMem ? lobby.SharedMem : "#")} #{lobby.Game.Name}");
            player.Send(0x19);
        }

        private static void CreateOrJoinLobby(Player player, byte[] data, string dataAsString)
        {
            string[] split = dataAsString.Split(' ');
            if (split.Length == 2)
            {
                string lobbyName = split[0];
                if (ushort.TryParse(split[1], out ushort maxCapacity))
                {
                    Lobby lobby = Server.GetLobby(lobbyName);
                    if (lobby == null)
                        lobby = Server.CreateLobby(lobbyName, maxCapacity);
                    if (lobby != null)
                        player.JoinLobby(lobby);
                }
            }
        }
        private static void LeaveLobbyCommand(Player player, byte[] data, string dataAsString)
        {
            player.LeaveLobby();
        }

        private static void RefreshTeamsCommand(Player player, byte[] data, string dataAsString)
        {
            if (player.CurrentLobby != null)
            {
                foreach (Team team in player.CurrentLobby.Teams)
                {
                    string teamMembers = "";

                    foreach (Player player_ in team.Members)
                        teamMembers += $" {(team.Host.Equals(player_) ? "*" : "#")}{player_.Name}";

                    player.Send(0x32, $"{team.Name} {team.NumPlayers} {team.MaxCapacity} {team.Flags} #{team.SharedMem} # {player.CurrentLobby.Game.Name}{teamMembers}");
                }
            }
            player.Send(0x33);
        }

        private static void CreateTeamCommand(Player player, byte[] data, string dataAsString)
        {
            string[] split = dataAsString.Split(' ');
            if (split.Length == 3 && ushort.TryParse(split[0], out ushort capacity))
            {
                string name = split[1];
                string type = split[2];
                if (player.CurrentLobby != null)
                    player.CurrentLobby.CreateTeam(player, name, capacity, type);
                else
                    player.Disconnect();
            }
        }

        private static void JoinTeamCommand(Player player, byte[] data, string dataAsString)
        {
            string[] split = dataAsString.Split(' ');
            if (split.Length == 1)
            {
                string name = split[0];
                player.JoinTeam(name);
            }
        }
        private static void LeaveTeamCommand(Player player, byte[] data, string dataAsString)
        {
            player.LeaveTeam();
        }

        private static void RefreshGamesCommand(Player player, byte[] data, string dataAsString)
        {
            Game[] games = Server.GetGames();
            foreach (Game game in games)
                player.Send(0x1B, $"1 {game.Name}");
            player.Send(0x1C);
        }

        private static void SelectGameCommand(Player player, byte[] data, string dataAsString)
        {
            string[] split = dataAsString.Split(' ');
            string gameName = split[0];
            player.SetGame(Server.GetGame(gameName));
            player.Send(0x1D, $"{player.Name} {player.CurrentGame.Name}");
        }

        private static void GetLicenseCommand(Player player, byte[] data, string dataAsString)
        {
            player.Send(0x22, "ABCDEFGHI");
        }


        private static void ChatLobbyCommand(Player player, byte[] data, string dataAsString)
        {
            if (player.CurrentLobby != null)
                player.CurrentLobby.SendChat(player.Name, dataAsString.Substring(dataAsString.IndexOf(' ') + 1));
        }

        private static void ChatTeamCommand(Player player, byte[] data, string dataAsString)
        {
            if (player.CurrentTeam != null)
                player.CurrentTeam.SendChat(player.Name, dataAsString);
        }
                
        private static void SharedMemPlayerCommand(Player player, byte[] data, string dataAsString)
        {
            player.SetSharedMem(data);
        }

        private static void SharedMemTeamCommand(Player player, byte[] data, string dataAsString)
        {
            string[] split = dataAsString.Split(' ');
            string teamName = split[0];
            string sharedMemStr = split[1];

            if (player.CurrentTeam != null)
                player.CurrentTeam.SetSharedMem(sharedMemStr);
        }
        private static void PingCommand(Player player, byte[] data, string dataAsString)
        {
            player.Send(0x00);
        }
        private static void DisconnectCommand(Player player, byte[] data, string dataAsString)
        {
            player.Send(0xE3);
            player.Send(0x16);
            player.Disconnect();
        }


        private static void ReconnectCommand(Player player, byte[] data, string dataAsString)
        {
            player.Send(0x1f);
        }

        private static void LaunchRequestCommand(Player player, byte[] data, string dataAsString)
        {
            player.CurrentTeam.SendGameServer(player);
        }

        private static void LaunchGameCommand(Player player, byte[] data, string dataAsString)
        {
            player.CurrentTeam.LaunchGame(player);
        }

        public void HandlePacket(Player player, ushort opcode, byte[] payload)
        {
            string payloadAsString = Encoding.ASCII.GetString(payload);
            string[] split = payloadAsString.Split(' ');

            if (typeof(CLIOpcode).IsEnumDefined(opcode) && CommandHandlers.ContainsKey((CLIOpcode)opcode))
            {
#if DEBUG
                if (!IgnoreDebug((CLIOpcode)opcode))
                    Program.Log.Debug($"Received: 0x{opcode:X2} -> {payloadAsString}");
#endif
                CommandHandlers[(CLIOpcode)opcode](player, payload, payloadAsString);
            }
            else
                Program.Log.Info($"Received unknown opcode: 0x{opcode:X2}");
        }

        private static bool IgnoreDebug(CLIOpcode opcode)
        {
            switch (opcode)
            {
                case CLIOpcode.PING:
                    return true;
                default:
                    return false;
            }    
        }
    }
}