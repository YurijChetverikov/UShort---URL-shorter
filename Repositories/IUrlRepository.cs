using System;
using System.Collections.Generic;
using System.Text;

namespace UShort.Repositories
{
    public interface IUrlRepository
    {
        bool Init(string? connectionString);
        Task<SQLModels.URLModel?> GetUrlAsync(int id);
        Task<int> CreateUrlAsync(string url); 
    }
}
