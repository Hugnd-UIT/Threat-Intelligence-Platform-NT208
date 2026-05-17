using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using backend.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Repositories
{
    public class SearchRepository
    {
        private readonly IArangoDBClient _databaseClient;

        public SearchRepository(IArangoDBClient databaseClient)
        {
            _databaseClient = databaseClient;
        }

        public async Task<IocNode?> FindMalwareAsync(string Keyword)
        {
            string Query = @"
                FOR node IN IocNodes
                FILTER node.Value == @keyword
                OR LIKE(node.Value, CONCAT('%', @keyword, '%'), true)
                SORT node.RiskScore DESC
                LIMIT 1
                RETURN node
            ";

            var BindVars = new Dictionary<string, object>
            {
                { "keyword", Keyword }
            };

            var Response = await _databaseClient.Cursor.PostCursorAsync<IocNode>(new PostCursorBody
            {
                Query = Query,
                BindVars = BindVars
            });

            return Response.Result.FirstOrDefault();
        }
    }
}