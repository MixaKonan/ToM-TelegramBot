using System.Threading.Tasks;
using TomTelegramBot.Bot;
using TomTelegramBot.SQLite;

namespace TomTelegramBot
{
    internal static class Program
    {
        private static void Main()
        {
            Parallel.Invoke(TomBot.RunBot, Database.CreateDb);
        }
    }
}