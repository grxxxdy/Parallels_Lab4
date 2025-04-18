namespace Server;

class Program
{
    static void Main(string[] args)
    {
        Server server = new Server();
        server.StartTcp(5000);
    }
}