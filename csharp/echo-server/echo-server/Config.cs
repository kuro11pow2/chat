namespace chat
{
    class Config
    {
        public LogLevel PrintLevel { get; set; }
        public string ServerAddress { get; set; }
        public int Port { get; set; }

        public Config()
        {
            PrintLevel = LogLevel.DEBUG;
            ServerAddress = "127.0.0.1";
            Port = 7000;
        }

        public override string ToString()
        {
            return $"[Config]\n{nameof(PrintLevel)}: {PrintLevel}\n{nameof(ServerAddress)}: {ServerAddress}\n{nameof(Port)}: {Port}";
        }
    }
}
