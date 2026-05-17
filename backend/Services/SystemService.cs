using backend.Repositories;
using System;
using System.Threading.Tasks;

namespace backend.Services
{
    public class SystemService
    {
        private readonly SystemRepository _systemRepository;

        public SystemService(SystemRepository systemRepository) 
        { 
            _systemRepository = systemRepository; 
        }

        public async Task<object> GetHealthStatusAsync()
        {
            var DatabaseName = await _systemRepository.GetDatabaseNameAsync();
            
            return new 
            { 
                Status = "Healthy", 
                ActiveDatabase = DatabaseName, 
                Timestamp = DateTime.UtcNow 
            };
        }
    }
}