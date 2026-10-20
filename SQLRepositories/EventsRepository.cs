using Dapper;
using Npgsql;
using UShort.Repositories;
using UShort.SQLModels;

namespace UShort.SQLRepositories
{
    [DapperAot]
    public class EventsRepository : IEventsRepository
    {
        private string _connectionString = "";

        public async Task<int> CreateEventAsync(EventModel ev)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            const string sql = """
                INSERT INTO events (link_id, timestamp, continent_geoname_id, latitude, longitude, accuracy_radius, country_geoname_id, city_geoname_id, event_type, user_agent) 
                VALUES (@LinkId, @Timestamp, @ContinentGeonameId, @Latitude, @Longitude, @AccuracyRadius, @CountryGeonameId, @CityGeonameId, @EventType::"char", @UserAgent) 
                RETURNING id;
                """;

            return await connection.QuerySingleAsync<int>(sql, ev);
        }

        public Task<EventModel?> GetEventAsync(int id)
        {
            throw new NotImplementedException();
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
