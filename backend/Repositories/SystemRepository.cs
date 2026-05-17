using ArangoDBNetStandard;
using System.Threading.Tasks;

namespace backend.Repositories
{
    public class SystemRepository
    {
        private readonly IArangoDBClient _databaseClient;

        public SystemRepository(IArangoDBClient databaseClient) 
        { 
            _databaseClient = databaseClient; 
        }

        public async Task<string> GetDatabaseNameAsync()
        {
            var DatabaseInfo = await _databaseClient.Database.GetCurrentDatabaseInfoAsync();
            return DatabaseInfo.Result.Name;
        }
    }
}