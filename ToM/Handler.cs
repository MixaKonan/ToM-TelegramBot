using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using StompLibrary;
using TomTelegramBot.Bot;
using TomTelegramBot.SQLite;
using TomTelegramBot.Stomp;
using TomTelegramBot.Video;
using WebSocketSharp;

namespace TomTelegramBot.ToM
{
    public class Handler
    {
        private User _user;
        private static readonly ConcurrentDictionary<User, WebSocket> UserSocketPairs = new ConcurrentDictionary<User, WebSocket>();
        private static readonly StompMessageSerializer Serializer = new StompMessageSerializer();

        public static void DeleteFromDictionary(User user)
        {
            UserSocketPairs.TryRemove(user, out var ws);
        }

        public void SubscribeOnStart(User user)
        {
            _user = user;

            using var webSocket = new WebSocket(user.serverName);

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
                    TomBot.BotClient.SendTextMessageAsync(user.chatId,$"An error occured.\n{exception.Message}");
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

        public static void CloseConnection(User user)
        {
            UserSocketPairs.TryGetValue(user, out var ws);
            ws?.Close();
        }

        private void WebSocketOnClose(object sender, CloseEventArgs e)
        {
            Console.WriteLine($"---------------\n{DateTime.Now}: Connection has been closed. Status code: {e.Code}");
            
            TomBot.BotClient.SendTextMessageAsync(_user.chatId, $"Connection has been closed, it was clean: {e.WasClean}. Status code: {e.Code}.");
        }

        private void WebSocketOnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine($"---------------\n{DateTime.Now}: {e.Message}");
            
            TomBot.BotClient.SendTextMessageAsync(_user.chatId,e.Message);
        }

        private void WebSocketOnOpen(object sender, EventArgs e)
        {
            Console.WriteLine($"---------------\n{DateTime.Now}: Connection has been established.");

            TomBot.BotClient.SendTextMessageAsync(_user.chatId,"You've been subscribed.");
        }

        private void WebSocketOnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                var deserializedStompMessage = Serializer.Deserialize(e.Data);
                var stompMessageBody = deserializedStompMessage.Body;


                if (stompMessageBody.Contains("ping"))
                {
                    Console.WriteLine("Got ping");
                    UserSocketPairs.TryGetValue(_user, out var webSocket);
                    var pingAnswer = new StompMessage("MESSAGE", "answer") { ["destination"] = "/topic/status"};
                    webSocket.Send(Serializer.Serialize(pingAnswer));
                    return;
                }

                Console.WriteLine($"---------------\n{DateTime.Now}\nUser [{_user.chatId}]:\n{e.Data}");
                
                if (!stompMessageBody.IsNullOrEmpty())
                {
                    try
                    {
                        var json = JsonConvert.DeserializeObject<VideoJson>(stompMessageBody);
                        TomBot.BotClient.SendTextMessageAsync(_user.chatId,     $"Vod ID: {json.vodId}\n" +
                                                                                $"Date: {json.date}\n" +
                                                                                $"UUID: {json.uuid}\n" +
                                                                                $"Started by: {json.startedBy}\n" +
                                                                                $"State: {json.state}\n" +
                                                                                $"twitch.tv/videos/{json.vodId}");
                    }

                    catch (JsonReaderException)
                    {
                        
                    }
                }
            }
            
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}
