using System;
using System.Threading;
using TomTelegramBot.Bot;
using TomTelegramBot.Video;
using TomTelegramBot.SQLite;
using StompLibrary;
using WebSocketSharp;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace TomTelegramBot.ToM
{
    public class Handler
    {
        private User _user;
        private static readonly Dictionary<User, WebSocket> UserSocketPairs = new Dictionary<User, WebSocket>();
        private static readonly StompMessageSerializer Serializer = new StompMessageSerializer();

        public void SubscribeOnStart(User user)
        {
            _user = user;

            using var webSocket = new WebSocket(user.serverName);

            webSocket.OnMessage += WebSocketOnMessage;
            webSocket.OnOpen += WebSocketOnOpen;
            webSocket.OnClose += WebSocketOnClose;
            webSocket.OnError += WebSocketOnError;

            UserSocketPairs.Add(user, webSocket);

            Connect(user, webSocket);
        }

        public void EstablishSubscription(User user)
        {
            using var webSocket = new WebSocket(user.serverName);

            _user = user;

            try
            {
                UserSocketPairs.Add(user, webSocket);
                Subscribe_(user, webSocket);
            }

            catch(ArgumentException)
            {
                var Pair = UserSocketPairs.Where(pair => pair.Key.chatId == user.chatId && pair.Key.serverName == user.serverName).First();
                Subscribe_(Pair.Key, Pair.Value);
            }

            webSocket.OnMessage += WebSocketOnMessage;
            webSocket.OnOpen += WebSocketOnOpen;
            webSocket.OnClose += WebSocketOnClose;
            webSocket.OnError += WebSocketOnError;

            
        }

        private void Subscribe_(User user, WebSocket webSocket)
        {
            if (Database.IsPresent(user))
            {
                if (ConnectionIsAlive(user))
                {
                    TomBot.BotClient.SendTextMessageAsync(user.chatId, "You're already subscribed.");
                    return;
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

        private bool ConnectionIsAlive(User user)
        {
            UserSocketPairs.TryGetValue(user, out WebSocket socket);

            return socket.IsAlive == true ? true : false;
        }

        private void Connect(User user, WebSocket webSocket)
        {
            webSocket.SetCredentials(user.login, user.password, true);
            webSocket.Connect();

            var connect = new StompMessage("CONNECT") { ["accept-version"] = "1.2", ["host"] = "" };
            webSocket.Send(Serializer.Serialize(connect));

            var sub = new StompMessage("SUBSCRIBE") { ["id"] = "sub-0", ["destination"] = "/topic/status" };
            webSocket.Send(Serializer.Serialize(sub));

            Thread.Sleep(int.MaxValue);
        }

        private void WebSocketOnClose(object sender, CloseEventArgs e)
        {
            Console.WriteLine($"---------------\n{DateTime.Now}: Connection has been closed. Status code: {e.Code}");
            
            TomBot.BotClient.SendTextMessageAsync(chatId: _user.chatId, text: e.Reason);
        }

        private void WebSocketOnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine($"---------------\n{DateTime.Now}: {e.Message}");
            
            TomBot.BotClient.SendTextMessageAsync(chatId: _user.chatId, text: e.Message);
        }

        private void WebSocketOnOpen(object sender, EventArgs e)
        {
            
            Console.WriteLine($"---------------\n{DateTime.Now}: Connection has been established.");

            TomBot.BotClient.SendTextMessageAsync(chatId: _user.chatId, text: "You've been subscribed.");
        }

        private void WebSocketOnMessage(object sender, MessageEventArgs e)
        {
            Console.WriteLine($"---------------\n{DateTime.Now}: {e.Data}User: {_user.chatId}");

            if (!Serializer.Deserialize(e.Data).Body.IsNullOrEmpty())
            {
                var json = JsonConvert.DeserializeObject<VideoJson>(Serializer.Deserialize(e.Data).Body);
                TomBot.BotClient.SendTextMessageAsync(chatId: _user.chatId, text: $"Vod ID: {json.vodId}\n" +
                                                                                  $"Date: {json.date}\n" +
                                                                                  $"UUID: {json.uuid}\n" +
                                                                                  $"Started by: {json.startedBy}\n" +
                                                                                  $"State: {json.state}");
            }
        }
    }
}