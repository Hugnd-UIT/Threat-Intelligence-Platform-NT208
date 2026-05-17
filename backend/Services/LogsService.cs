using backend.Models;
using backend.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services
{
    public class LogsService
    {
        private readonly LogsRepository _logsRepository;

        public LogsService(LogsRepository logsRepository) 
        { 
            _logsRepository = logsRepository; 
        }

        public async Task<IEnumerable<Logs>> GetAuditLogsAsync()
        {
            return await _logsRepository.GetAllLogsAsync();
        }
    }
}