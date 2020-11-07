using System;

namespace TomTelegramBot.SQLite
{
    public class User
    {
        public string chatId { get; set; }
        public string serverName { get; set; }
        public string login { get; set; }
        public string password { get; set; }

        public override string ToString()
        {
            return chatId + ": " + serverName;
        }

        public override bool Equals(object obj)
        {
            return obj is User user &&
                   chatId == user.chatId &&
                   serverName == user.serverName &&
                   login == user.login &&
                   password == user.password;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(chatId, serverName, login, password);
        }
    }
}