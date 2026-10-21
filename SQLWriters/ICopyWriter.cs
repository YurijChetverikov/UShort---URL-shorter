using Npgsql;

namespace UShort.SQLWriters
{
    public interface ICopyWriter<T>
    {
        public Task WriteAsync(T item, NpgsqlBinaryImporter writer, CancellationToken cancellationToken);
    }
}
