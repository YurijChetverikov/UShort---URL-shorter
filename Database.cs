using System;
using System.Collections.Generic;
using System.Text;

namespace UShort
{
    internal class Database
    {
        internal static void Init(string host = "localhost", ushort port = 5432, string databaseName = "ushort", string username = "postgres", string password = "admin")
        {
            string connString = $"Host={host}; Port={port}; Database={databaseName}; Username={username}; Password={password};";

            //// 2. Создаем и открываем подключение
            //using (var connection = new NpgsqlConnection(connString))
            //{
            //    connection.Open();

            //    // --- ВСТАВКА (INSERT) с параметрами ---
            //    var newUser = new User { Name = "Иван", Email = "ivan@example.com" };
            //    string insertSql = "INSERT INTO users (name, email) VALUES (@Name, @Email);";

            //    // Dapper сам подставит свойства объекта вместо @Name и @Email
            //    int rowsAffected = connection.Execute(insertSql, newUser);
            //    Console.WriteLine($"Добавлено строк: {rowsAffected}");

            //    // --- ВЫБОРКА СПИСКА (SELECT) ---
            //    string selectSql = "SELECT id, name, email FROM users;";

            //    // Dapper автоматически преобразует строки таблицы в объекты List<User>
            //    IEnumerable<User> users = connection.Query<User>(selectSql);

            //    foreach (var user in users)
            //    {
            //        Console.WriteLine($"{user.Id}: {user.Name} ({user.Email})");
            //    }

            //    // --- ПОЛУЧЕНИЕ ОДНОЙ ЗАПИСИ ---
            //    string singleSql = "SELECT id, name, email FROM users WHERE id = @Id;";
            //    var userById = connection.QueryFirstOrDefault<User>(singleSql, new { Id = 1 });
            //}
        }
    }
}
