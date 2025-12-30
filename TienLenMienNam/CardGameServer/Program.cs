// CardGameServer/Program.cs
using System;
using CardGameServer.Game;
using CardGameServer.Network;

namespace CardGameServer
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            int port = 7777;
            if (args != null && args.Length >= 1)
            {
                int p;
                if (int.TryParse(args[0], out p)) port = p;
            }

            var engine = new GameEngine();
            var server = new GameServer(port, engine);

            Console.Title = "CardGameServer";
            Console.WriteLine("Starting server on port " + port);
            server.Start();

            Console.WriteLine("Press ENTER to stop...");
            Console.ReadLine();

            server.Stop();
            Console.WriteLine("Stopped.");
        }
    }
}

