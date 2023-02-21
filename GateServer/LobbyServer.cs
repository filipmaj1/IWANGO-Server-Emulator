namespace IWANGOEmulator.GateServer
{
    class LobbyServer
    {
        public readonly string Name;
        public readonly string Ip;
        public readonly ushort Port;

        public LobbyServer(string name, string ip, ushort port)
        {
            Name = name;
            Ip = ip;
            Port = port;
        }
    }
}
