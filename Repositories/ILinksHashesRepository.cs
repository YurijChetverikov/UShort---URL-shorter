namespace UShort.Repositories
{
    public interface ILinksHashesRepository
    {
        bool Init(string? connectionString);
        Task<int> CheckLinkAsync(string link);
        Task AddLinkAsync(string link, int linkId);
    }
}
