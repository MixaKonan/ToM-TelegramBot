using System;
using System.IO;


namespace TomTelegramBot.Logs
{
    public class BotLogger
    {
        public static async void Log(string text)
        {
            const string path = "logs.txt";

            if (File.Exists(path))
            {
                await File.AppendAllLinesAsync(path, new[] { text });
            }
            else
            {
                File.Create(path);
                await File.AppendAllLinesAsync(path, new[] { text });
            }
        }
    }
}