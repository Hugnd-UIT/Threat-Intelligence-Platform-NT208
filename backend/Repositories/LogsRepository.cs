using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using backend.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Repositories
{
    public class LogsRepository
    {
        private readonly IArangoDBClient _databaseClient;

        public LogsRepository(IArangoDBClient databaseClient) 
        { 
            _databaseClient = databaseClient; 
        }

        public async Task<IEnumerable<Logs>> GetAllLogsAsync()
        {
            var LogCursor = await _databaseClient.Cursor.PostCursorAsync<Logs>(new PostCursorBody 
            { 
                Query = "FOR l IN AuditLogs SORT l.Timestamp DESC RETURN l" 
            });

            return LogCursor.Result;
        }
    }
}