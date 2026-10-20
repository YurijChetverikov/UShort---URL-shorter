using Dapper;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using UShort.Repositories;

namespace UShort.SQLRepositories
{
    [DapperAot]
    public record LinksHashesDTO
    {
        public byte[] Hash { get; set; }
        public int LinkId { get; set; }
    }


    [DapperAot]
    public class LinksHashesRepository : ILinksHashesRepository
    {


        private string _connectionString = "";

        public async Task AddLinkAsync(string link, int linkId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = """
                INSERT INTO links_hashes (hash, link_id) 
                VALUES (@Hash, @LinkId);
                """;

            await connection.QueryAsync(sql, new LinksHashesDTO() { LinkId = linkId, Hash = SHA512.HashData(Encoding.UTF8.GetBytes(link)) });
        }

        public async Task<int> CheckLinkAsync(string link)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = @"
                SELECT link_id
                FROM links_hashes
                WHERE hash = @Hash
                LIMIT 1";

            var result = await connection.ExecuteScalarAsync<int?>(sql, new LinksHashesDTO() { Hash = SHA512.HashData(Encoding.UTF8.GetBytes(link)) });

            return result.HasValue ? result.Value : -1;
        }

        public bool Init(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return false;

            _connectionString = connectionString;

            return true;
        }
    }
}
