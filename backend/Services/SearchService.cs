using backend.Models;
using backend.Repositories;
using System.Threading.Tasks;

namespace backend.Services
{
    public class SearchService
    {
        private readonly SearchRepository _searchRepository;

        public SearchService(SearchRepository searchRepository)
        {
            _searchRepository = searchRepository;
        }

        public async Task<IocNode?> SearchGlobalAsync(string Keyword)
        {
            // Tầng Service đảm nhận check logic phân loại chuỗi
            string Type = "Unknown";
            
            if (System.Text.RegularExpressions.Regex.IsMatch(Keyword, @"^(\d{1,3}\.){3}\d{1,3}$"))
            {
                Type = "IP";
            }
            else if (Keyword.Contains(".") && Keyword.Length > 3)
            {
                Type = "Domain";
            }
            else if (Keyword.Length == 32 || Keyword.Length == 64)
            {
                Type = "Hash";
            }

            return await _searchRepository.FindMalwareAsync(Keyword);
        }
    }
}