using Dapper;
using Npgsql;
using System.Data.Common;
using System.Text.RegularExpressions;
using UShort.Repositories;
using UShort.SQLModels;
using UShort.SQLWriters;

namespace UShort.SQLRepositories
{
    [DapperAot]
    public class EventsRepository : IEventsRepository
    {
        private string _connectionString = "";
        private EventModelCopyWriter _eventCopyWriter = new EventModelCopyWriter();

        /// <summary>
        /// Creates events using COPY instead of INSERS
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task BulkCreateEventsAsync(IEnumerable<EventModel> batch, CancellationToken cancellationToken)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // While QuerySingleAsync opens connection itself, here we must open connection 
            await connection.OpenAsync(cancellationToken);

            const string sql = """
                COPY events (link_id, timestamp, continent_geoname_id, latitude, longitude, accuracy_radius, country_geoname_id, city_geoname_id, event_type, user_agent)
                FROM STDIN(FORMAT BINARY);
                """;

            await using var writer = await connection.BeginBinaryImportAsync(sql, cancellationToken);

            foreach (var ev in batch)
                await _eventCopyWriter.WriteAsync(ev, writer, cancellationToken);

            await writer.CompleteAsync(cancellationToken);
        }

        /// <summary>
        /// Create event
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
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

            _eventCopyWriter = new EventModelCopyWriter();

            return true;
        }
    }
}
