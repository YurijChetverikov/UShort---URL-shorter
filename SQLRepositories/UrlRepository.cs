using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using UShort.Repositories;
using UShort.SQLModels;

namespace UShort.SQLRepositories
{
    [DapperAot]
    public class UrlRepository : IUrlRepository
    {
        private string _connectionString = "";

        public bool Init(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return false;

            _connectionString = connectionString;

            return true;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        /// <exception cref="PostgresException"></exception>
        public async Task<int> CreateUrlAsync(string url)
        {
            URLModel model = new URLModel()
            {
                CreatedAt = DateTime.UtcNow,
                Link = url
            };

            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = """
                INSERT INTO links (link, created_at) 
                VALUES (@Link, @CreatedAt) 
                RETURNING id;
                """;

            return await connection.QuerySingleAsync<int>(sql, model);
        }

        public async Task<URLModel?> GetUrlAsync(int id)
        {
            // 1. Создаём новое короткоживущее соединение
            using var connection = new NpgsqlConnection(_connectionString);

            // 2. Dapper сам откроет и закроет его для одного запроса
            const string sql = "SELECT * FROM links WHERE id = @id";
            return await connection.QueryFirstOrDefaultAsync<URLModel>(sql, new { id = id});
        }
    }
}
