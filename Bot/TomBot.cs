using System;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using TomTelegramBot.SQLite;
using TomTelegramBot.ToM;

namespace TomTelegramBot.Bot
{
    public static class TomBot
    {
        public static readonly TelegramBotClient BotClient =
            new TelegramBotClient("token");

        private const int ServerName = 1;
        private const int Login = 2;
        private const int Password = 3;

        public static void RunBot()
        {
            var botInfo = BotClient.GetMeAsync().Result;
            Console.WriteLine($"BotId: {botInfo.Id}\nBotName: {botInfo.Username}");

            BotClient.OnMessage += BotClientOnMessage;
            BotClient.StartReceiving();

            if (BotClient.IsReceiving)
            {
                Console.WriteLine("Bot is now receiving messages.");
                
                foreach (var user in Database.GetUsers())
                {
                    var handler = new Handler();

                    var thread = new Thread(() => handler.SubscribeOnStart(user));
                    thread.Start();
                }
            }
            Thread.Sleep(int.MaxValue);
        }

        private static void BotClientOnMessage(object sender, MessageEventArgs e)
        {
            var messageText = e.Message.Text;
            
            if (messageText.StartsWith("/subscribe"))
            {
                SubscribeMessage(messageText, e);
            }

            if (messageText.StartsWith("/unsubscribe"))
            {
                UnsubscribeMessage(messageText, e);
            }

            if (messageText.StartsWith("/help"))
            {
                BotClient.SendTextMessageAsync(e.Message.Chat.Id,
                    "You can subscribe by using '/subscribe [server] [login] [password]'\n" +
                    "And unsubscribe by '/unsubscribe [server]'\nFor example, /subscribe https://myserver.ru admin password, /unsubscribe https://myserver.ru");
            }
        }

        private static void SubscribeMessage(string message, MessageEventArgs e)
        {
            var split = message.Split();

            if (split.Length == 4)
            {
                try
                {
                    var user = new SQLite.User
                    {
                        serverName = ValidateUri(split[ServerName]),
                        login = split[Login],
                        password = split[Password],
                        chatId = e.Message.Chat.Id.ToString(),
                    };

                    var handler = new Handler();

                    var thread = new Thread(() => handler.EstablishSubscription(user));
                    thread.Start();
                }
                catch (ArgumentException exception)
                {
                    BotClient.SendTextMessageAsync(chatId: e.Message.Chat.Id, text: exception.Message);
                }
            }
            else
            {
                BotClient.SendTextMessageAsync(chatId: e.Message.Chat.Id, "Some error.");
            }
        }

        private static void UnsubscribeMessage(string message, MessageEventArgs e)
        {
            var split = message.Split();

            if (split.Length == 2)
            {
                var user = new SQLite.User
                {
                    serverName = ValidateUri(split[ServerName]),
                    chatId = e.Message.Chat.Id.ToString()
                };
                try
                {
                    Database.DeleteUser(user);
                    BotClient.SendTextMessageAsync(chatId: user.chatId, text: $"You've been unsubscribed.");
                }
                catch (Exception exception)
                {
                    BotClient.SendTextMessageAsync(chatId: user.chatId, text: $"Deletion error occured.\n{exception.Message}");
                }
            }
            else
            {
                BotClient.SendTextMessageAsync(chatId: e.Message.Chat.Id, "Some error.");
            }
        }

        private static string ValidateUri(string uriString)
        {
            if (!IsValidUri(uriString))
            {
                throw new ArgumentException("Wrong uri.");
            }

            var uri = new Uri(uriString);

            return uri.Scheme == Uri.UriSchemeHttps
                ? uri.AbsoluteUri.Replace("https", "wss")
                : uri.AbsoluteUri.Replace("http", "ws");
        }

        private static bool IsValidUri(string uri)
        {
            return uri.StartsWith("http");
        }
    }
}
