using Npgsql;
using NpgsqlTypes;
using UShort.SQLModels;

namespace UShort.SQLWriters
{
    public class EventModelCopyWriter : ICopyWriter<EventModel>
    {
        /// <summary>
        /// Writes EvemtModel fields into NpgsqlBinaryImporter writer
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="writer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task WriteAsync(EventModel ev, NpgsqlBinaryImporter writer, CancellationToken cancellationToken)
        {
            await writer.StartRowAsync(cancellationToken);

            await writer.WriteAsync(ev.LinkId, NpgsqlDbType.Integer, cancellationToken);
            await writer.WriteAsync(ev.Timestamp, NpgsqlDbType.TimestampTz, cancellationToken);

            if (ev.ContinentGeonameId.HasValue)
            {
                await writer.WriteAsync(ev.ContinentGeonameId.Value, NpgsqlDbType.Integer, cancellationToken);
            }
            else
            {
                await writer.WriteNullAsync(cancellationToken);
            }
            if (ev.Latitude.HasValue)
            {
                await writer.WriteAsync(ev.Latitude.Value, NpgsqlDbType.Numeric, cancellationToken);
            }
            else
            {
                await writer.WriteNullAsync(cancellationToken);
            }

            if (ev.Longitude.HasValue)
            {
                await writer.WriteAsync(ev.Longitude.Value, NpgsqlDbType.Numeric, cancellationToken);
            }
            else
            {
                await writer.WriteNullAsync(cancellationToken);
            }

            if (ev.AccuracyRadius.HasValue)
            {
                await writer.WriteAsync(ev.AccuracyRadius.Value, NpgsqlDbType.Integer, cancellationToken);
            }
            else
            {
                await writer.WriteNullAsync(cancellationToken);
            }

            if (ev.CountryGeonameId.HasValue)
            {
                await writer.WriteAsync(ev.CountryGeonameId.Value, NpgsqlDbType.Integer, cancellationToken);
            }
            else
            {
                await writer.WriteNullAsync(cancellationToken);
            }

            if (ev.CityGeonameId.HasValue)
            {
                await writer.WriteAsync(ev.CityGeonameId.Value, NpgsqlDbType.Integer, cancellationToken);
            }
            else
            {
                await writer.WriteNullAsync(cancellationToken);
            }

            await writer.WriteAsync(ev.EventType.ToString(), NpgsqlDbType.Char, cancellationToken);

            if (ev.UserAgent != null)
            {
                await writer.WriteAsync(ev.UserAgent, NpgsqlDbType.Text, cancellationToken);
            }
            else
            {
                await writer.WriteNullAsync(cancellationToken);
            }
        }
    }
}
