namespace UShort.Repositories
{
    public interface IUrlRepository
    {
        bool Init(string? connectionString);
        Task<SQLModels.URLModel?> GetUrlAsync(int id);
        Task<int> CreateUrlAsync(string url); 
    }
}
