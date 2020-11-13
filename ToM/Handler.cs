using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using StompLibrary;
using TomTelegramBot.Bot;
using TomTelegramBot.Logs;
using TomTelegramBot.SQLite;
using TomTelegramBot.Stomp;
using TomTelegramBot.Video;
using WebSocketSharp;

namespace TomTelegramBot.ToM
{
    public class Handler
    {
        private User _user;
        private Logs.BotLogger _logger = new Logs.BotLogger();
        private static readonly ConcurrentDictionary<User, WebSocket> UserSocketPairs = new ConcurrentDictionary<User, WebSocket>();
        private static readonly StompMessageSerializer Serializer = new StompMessageSerializer();

        public void SubscribeOnStart(User user)
        {
            _user = user;

            using var webSocket = new WebSocket(user.serverName);

            webSocket.OnMessage += WebSocketOnMessage;
            webSocket.OnOpen += WebSocketOnOpen;
            webSocket.OnClose += WebSocketOnClose;
            webSocket.OnError += WebSocketOnError;

            UserSocketPairs.TryAdd(user, webSocket);

            Connect(user, webSocket);
        }

        public void EstablishSubscription(User user)
        {
            using var webSocket = new WebSocket(user.serverName);

            _user = user;

            try
            {
                UserSocketPairs.TryAdd(user, webSocket);
                Subscribe(user, webSocket);
            }

            catch(ArgumentException)
            {
                var (key, value) = UserSocketPairs.First(pair => pair.Key.chatId == user.chatId && pair.Key.serverName == user.serverName);
                Subscribe(key, value);
            }

            webSocket.OnMessage += WebSocketOnMessage;
            webSocket.OnOpen += WebSocketOnOpen;
            webSocket.OnClose += WebSocketOnClose;
            webSocket.OnError += WebSocketOnError;
        }

        private void Subscribe(User user, WebSocket webSocket)
        {
            if (Database.IsPresent(user))
            {
                if (ConnectionIsAlive(user))
                {
                    TomBot.BotClient.SendTextMessageAsync(user.chatId, "You're already subscribed.");
                }

                else
                {
                    Connect(user, webSocket);
                }
            }

            else
            {
                try
                {
                    Database.WriteUser(user);
                    Connect(user, webSocket);
                }

                catch (Exception exception)
                {
                    TomBot.BotClient.SendTextMessageAsync(chatId: user.chatId, text: $"An error occured.\n{exception.Message}");
                }
            }
        }

        private static bool ConnectionIsAlive(User user)
        {
            UserSocketPairs.TryGetValue(user, out var socket);

            return socket != null && socket.IsAlive;
        }

        private void Connect(User user, WebSocket webSocket)
        {
            webSocket.OnMessage += WebSocketOnMessage;
            webSocket.OnOpen += WebSocketOnOpen;
            webSocket.OnClose += WebSocketOnClose;
            webSocket.OnError += WebSocketOnError;
            
            webSocket.SetCredentials(user.login, user.password, true);
            webSocket.Connect();
            
            Console.WriteLine(webSocket.IsAlive);

            var connect = new StompMessage("CONNECT") { ["accept-version"] = "1.2", ["host"] = "" };
            webSocket.Send(Serializer.Serialize(connect));

            var sub = new StompMessage("SUBSCRIBE") { ["id"] = "sub-0", ["destination"] = "/topic/status" };
            webSocket.Send(Serializer.Serialize(sub));

            Thread.Sleep(int.MaxValue);
        }

        private void WebSocketOnClose(object sender, CloseEventArgs e)
        {
            Console.WriteLine($"---------------\n{DateTime.Now}: Connection has been closed. Status code: {e.Code}");
            
            TomBot.BotClient.SendTextMessageAsync(chatId: _user.chatId, text: e.ToString());
            
            BotLogger.Log($"CLOSEMESSAGE\t{DateTime.Now}\n{e.Reason}, {e.Code}\n");
        }

        private void WebSocketOnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine($"---------------\n{DateTime.Now}: {e.Message}");
            
            TomBot.BotClient.SendTextMessageAsync(chatId: _user.chatId, text: e.Message);
            
            BotLogger.Log($"ERRORMESSAGE\t{DateTime.Now}\n{e.Message}\n");
        }

        private void WebSocketOnOpen(object sender, EventArgs e)
        {
            Console.WriteLine($"---------------\n{DateTime.Now}: Connection has been established.");

            TomBot.BotClient.SendTextMessageAsync(chatId: _user.chatId, text: "You've been subscribed.");
            
            BotLogger.Log($"OPENMESSAGE\t{DateTime.Now}\nConnection established.\n");
        }

        private void WebSocketOnMessage(object sender, MessageEventArgs e)
        {
            Console.WriteLine($"---------------\n{DateTime.Now}: {e.Data}\nUser: {_user.chatId}");

            if (!Serializer.Deserialize(e.Data).Body.IsNullOrEmpty())
            {
                var json = JsonConvert.DeserializeObject<VideoJson>(Serializer.Deserialize(e.Data).Body);
                TomBot.BotClient.SendTextMessageAsync(chatId: _user.chatId, text: $"Vod ID: {json.vodId}\n" +
                                                                                  $"Date: {json.date}\n" +
                                                                                  $"UUID: {json.uuid}\n" +
                                                                                  $"Started by: {json.startedBy}\n" +
                                                                                  $"State: {json.state}");
            }
            
            BotLogger.Log($"MESSAGE\t{DateTime.Now}\n{e.Data}\n");
        }
    }
}