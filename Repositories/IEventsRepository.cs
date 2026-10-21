namespace UShort.Repositories
{
    public interface IEventsRepository
    {
        bool Init(string? connectionString);
        Task<SQLModels.EventModel?> GetEventAsync(int id);
        Task<int> CreateEventAsync(SQLModels.EventModel ev);
        Task BulkCreateEventsAsync(IEnumerable<SQLModels.EventModel> ev, CancellationToken cancellationToken);
    }
}
