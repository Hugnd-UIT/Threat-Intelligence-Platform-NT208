using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Repositories
{
    public class IocIngestRepository
    {
        private readonly IArangoDBClient _databaseClient;

        public IocIngestRepository(IArangoDBClient databaseClient)
        {
            _databaseClient = databaseClient;
        }

        public async Task BulkUpsertNodesAsync(List<IocNode> NodesList)
        {
            string Query = @"
                FOR doc IN @nodes
                UPSERT { _key: doc._key } 
                INSERT doc
                UPDATE { RiskScore: doc.RiskScore, UpdatedAt: DATE_ISO8601(DATE_NOW()) } IN IocNodes
            ";
            
            var BindVars = new Dictionary<string, object>
            {
                { "nodes", NodesList }
            };

            await _databaseClient.Cursor.PostCursorAsync<dynamic>(new PostCursorBody 
            { 
                Query = Query, 
                BindVars = BindVars 
            });
        }

        public async Task BulkInsertEdgesAsync(List<dynamic> EdgesList)
        {
            string Query = @"
                FOR edge IN @edges
                INSERT edge INTO IocRelationships OPTIONS { ignoreErrors: true }
            ";
            
            var BindVars = new Dictionary<string, object> 
            { 
                { "edges", EdgesList } 
            };
            
            await _databaseClient.Cursor.PostCursorAsync<dynamic>(new PostCursorBody 
            { 
                Query = Query, 
                BindVars = BindVars 
            });
        }
    }
}