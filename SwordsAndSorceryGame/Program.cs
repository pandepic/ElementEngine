using System;

namespace SwordsAndSorceryGame
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var game = new Game())
                game.Run();
        }
    }
}
