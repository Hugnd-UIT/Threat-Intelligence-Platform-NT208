using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using backend.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Repositories
{
    public class DashboardRepository
    {
        private readonly IArangoDBClient _databaseClient;

        public DashboardRepository(IArangoDBClient databaseClient) 
        { 
            _databaseClient = databaseClient; 
        }

        public async Task<int> GetCollectionCountAsync(string CollectionName)
        {
            var Response = await _databaseClient.Cursor.PostCursorAsync<int>(new PostCursorBody 
            { 
                Query = $"RETURN LENGTH({CollectionName})" 
            });

            return Response.Result.FirstOrDefault();
        }

        public async Task<int> GetIocsTodayCountAsync()
        {
            string Query = "RETURN LENGTH(FOR doc IN IocNodes FILTER LEFT(doc.CreatedAt, 10) == LEFT(DATE_ISO8601(DATE_NOW()), 10) RETURN doc)";
            
            var Response = await _databaseClient.Cursor.PostCursorAsync<int>(new PostCursorBody 
            { 
                Query = Query 
            });

            return Response.Result.FirstOrDefault();
        }

        public async Task<IEnumerable<IocNode>> GetTopRiskIocsAsync()
        {
            string Query = @"
                FOR doc IN IocNodes 
                SORT doc.RiskScore DESC, doc.riskScore DESC 
                LIMIT 10 
                RETURN doc";

            var Response = await _databaseClient.Cursor.PostCursorAsync<IocNode>(new PostCursorBody 
            { 
                Query = Query 
            });

            return Response.Result;
        }
    }
}