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
            LOGIN = 0x01,
            LOGIN2 = 0x02,
            SEND_LOG = 0x03,
            ENTR_LOBBY = 0x04,
            DISCONNECT = 0x05,
            GET_LOBBIES = 0x07,
            GET_GAMES = 0x08,
            SELECT_GAME = 0x09,
            PING = 0x0A,
            SEARCH = 0x0B,
            GET_LICENSE = 0x0C,
            GET_TEAMS = 0x0F,
            REFRESH_PLAYERS = 0x10,
            CHAT_LOBBY = 0x11,
            SHAREDMEM_PLAYER = 0x1B,
            SHAREDMEM_TEAM = 0x20,
            LEAVE_TEAM = 0x21,
            GET_EXTRAUSERMEM = 0x29,
            REGIST_EXTRAUSERMEM_START = 0x2A,
            REGIST_EXTRAUSERMEM_TRANSFER = 0x2B,
            REGIST_EXTRAUSERMEM_END = 0x2C,
            RECONNECT = 0x0D,
            LAUNCH_REQUEST = 0x22,
            LAUNCH_GAME = 0x65,
            REFRESH_USERS = 0x67,
            CHAT_TEAM = 0x23,
            CREATE_TEAM = 0x24,
            JOIN_TEAM = 0x25,
            LEAVE_LOBBY = 0x3C

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
            [CLIOpcode.GET_EXTRAUSERMEM] = GetExtraUserMem,
            [CLIOpcode.REGIST_EXTRAUSERMEM_START] = RegisterExtraUserMem,
            [CLIOpcode.REGIST_EXTRAUSERMEM_TRANSFER] = RegisterExtraUserMem,
            [CLIOpcode.REGIST_EXTRAUSERMEM_END] = RegisterExtraUserMem,
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
            [CLIOpcode.REFRESH_USERS] = RefreshUsersCommand,
            [CLIOpcode.RECONNECT] = ReconnectCommand,
            [CLIOpcode.SEARCH] = SearchCommand,
            [CLIOpcode.SEND_LOG] = NullCommand,
        };

        public PacketProcessor(Server server)
        {
            Server = server;
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
                Program.Log.Info($"Received unknown opcode: 0x{opcode:X2} -> {payloadAsString} | {BitConverter.ToString(payload).Replace("-", "")}");
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
                    string sharedMem = team.SharedMem.Length != 0 ? $"*{team.SharedMem}" : "#";
                    string teamMembers = "";

                    foreach (Player player_ in team.Members)
                        teamMembers += $" {(team.Host.Equals(player_) ? "*" : "#")}{player_.Name}";

                    player.Send(0x32, $"{team.Name} {team.NumPlayers} {team.MaxCapacity} {team.Flags} {sharedMem}{teamMembers} {player.CurrentLobby.Game.Name}");
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

        private static void GetExtraUserMem(Player player, byte[] data, string dataAsString)
        {
            string[] split = dataAsString.Split(' ');

            if (split.Length == 3)
            {
                string userName = split[0];
                bool offsetGood = int.TryParse(split[1], out int offset);
                bool lengthGood = int.TryParse(split[2], out int length);

                if (!offsetGood || !lengthGood)
                {
                    player.Disconnect();
                    return;
                }

                byte[] mem = new byte[]
                {
                    0x52, 0x45, 0x47, 0x41, 0x54, 0x45, 0x54, 0x52, 0x49, 0x53, 0x20, 0x31, 0x2E, 0x30, 0x30, 0x00, // SEGATETRIS 1.00
                    0x0C, 0x02, 0x02, 0x00, 0x01, 0x00, 0x04, 0x00, 0x02, 0x00, 0x00, 0x00
                };

                player.SendExtraMem(mem, 0, 0x1C);
            }
            else
                player.Disconnect();
        }

        private static void RegisterExtraUserMem(Player player, byte[] data, string dataAsString)
        {
            player.Send(0x4F);   
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
            player.Disconnect(false);
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

        private static byte[] Test(int num)
        {
            byte[] sharedMem = new byte[0x1E];
            sharedMem[0] = 0xFF;
            sharedMem[4] = 0xFF;
            sharedMem[8] = 0xFF;
            sharedMem[12] = 0xFF;
            sharedMem[16] = 0xFF;
            sharedMem[20] = 0xFF;
            byte[] data1 = Packet.CreateSharedMemPacket(sharedMem, $"0 *AAA{num} 0 0 0");
            byte[] test = new byte[data1.Length + 4];
            Array.Copy(data1, test, data1.Length);
            test[test.Length - 4] = 0xFF;
            return test;
        }

        private static void RefreshUsersCommand(Player player, byte[] data, string dataAsString)
        {
            if (dataAsString.Equals("2P_Red"))
            {
                player.Send(0xD9);
                return;
            }
            if (dataAsString.Equals("2P_Blue"))
            {
                for (int i = 0; i < 1; i++)                
                    player.Send(0xDA, Test(i));                
                player.Send(0xD9);
                return;
            }
            if (dataAsString.Equals("2P_Green"))
            {
                for (int i = 0; i < 2; i++)
                    player.Send(0xDA, Test(i));
                player.Send(0xD9);
                return;
            }
            if (dataAsString.Equals("4P_Yellow"))
            {
                for (int i = 0; i < 3; i++)
                    player.Send(0xDA, Test(i));
                player.Send(0xD9);
                return;
            }
            if (dataAsString.Equals("4P_Purple"))
            {
                for (int i = 0; i < 4; i++)
                    player.Send(0xDA, Test(i));
                player.Send(0xD9);
                return;
            }
            if (dataAsString.Equals("4P_Orange"))
            {
                for (int i = 0; i < 5; i++)
                    player.Send(0xDA, Test(i));
                player.Send(0xD9);
                return;
            }
        }

        private static void SearchCommand(Player player, byte[] data, string dataAsString)
        {
            Player found = Server.GetPlayer(dataAsString);

            if (found != null)
            {
                string lobby = found.CurrentLobby != null ? $"!{found.CurrentLobby.Name}" : "#";
                player.Send(0x07, $"{found.Name} !{Server.GetName()} {lobby}");
            }

            player.Send(0xC9, "1");
        }

        private static void NullCommand(Player player, byte[] data, string dataAsString)
        {

        }

        private static bool IgnoreDebug(CLIOpcode opcode)
        {
            switch (opcode)
            {
                case CLIOpcode.SEND_LOG:
                case CLIOpcode.PING:
                    return true;
                default:
                    return false;
            }    
        }
    }
}