using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using WebSocketSharp;

namespace TomTelegramBot.SQLite
{
    public static class Database
    {
        private static readonly SQLiteConnection Connection = new SQLiteConnection("Data Source=MyDatabase.sqlite;Version=3");
        public static void CreateDb()
        {
            if (!File.Exists("./MyDatabase.sqlite"))
            {
                SQLiteConnection.CreateFile("MyDatabase.sqlite");
            }

            Connection.Open();
            
            var sql = "create table if not exists servers" + 
                      "(chatId varchar(255), serverName varchar(255), login varchar(255), password varchar(255), primary key(chatId, serverName))";
            
            var command = new SQLiteCommand(sql, Connection);

            command.ExecuteNonQuery();
            
            Connection.Close();
        }

        public static bool IsPresent(User user)
        {
            var sql = $"select * from servers where chatId = '{user.chatId}' and serverName = '{user.serverName}'";
            var serverName = "";

            Connection.Open();

            var command = new SQLiteCommand(sql, Connection);
            var reader = command.ExecuteReader();

            while(reader.Read())
            {
                serverName = reader["serverName"].ToString();
            }

            Connection.Close();

            if(serverName.IsNullOrEmpty())
            {
                return false;
            }

            return true;
        }

        public static void WriteUser(User user)
        {
            var sql = "insert into servers (chatId, serverName, login, password) " +
                     $"values ('{user.chatId}', '{user.serverName}', '{user.login}', '{user.password}')";
            
            Connection.Open();
            
            var command = new SQLiteCommand(sql, Connection);
            command.ExecuteNonQuery();
            
            Connection.Close();
        }

        public static List<User> GetUsers()
        {
            var sql = "select * from servers";
            var users = new List<User>();
            
            
            Connection.Open();
            
            var command = new SQLiteCommand(sql, Connection);
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var user = new User()
                {
                    chatId = reader["chatId"].ToString(),
                    serverName = reader["serverName"].ToString(),
                    login = reader["login"].ToString(),
                    password = reader["password"].ToString()
                };
                users.Add(user);
            }
            Connection.Close();

            return users;
        }

        public static void DeleteUser(User user)
        {
            var sql = $"delete from servers where chatId = '{user.chatId}' and serverName = '{user.serverName}'";
            Connection.Open();
            
            var command = new SQLiteCommand(sql, Connection);
            command.ExecuteNonQuery();
            
            Connection.Close();
        
            Console.WriteLine("User deleted.");
        }
    }
}