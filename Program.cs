using lab2_ver2;

class Program
{
    static void Main(string[] args)
    {
        using (Game game = new Game(800, 600))
        {
            game.Run();
        }
    }
}

