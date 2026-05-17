using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using backend.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Repositories
{
    public class UsersRepository
    {
        private readonly IArangoDBClient _databaseClient;
        private const string CollectionName = "Users";

        public UsersRepository(IArangoDBClient databaseClient)
        {
            _databaseClient = databaseClient;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            string Query = "FOR u IN @@collection RETURN u";
            
            var BindVars = new Dictionary<string, object> 
            { 
                { "@collection", CollectionName } 
            };

            var Response = await _databaseClient.Cursor.PostCursorAsync<User>(new PostCursorBody 
            { 
                Query = Query, 
                BindVars = BindVars 
            });

            return Response.Result;
        }

        public async Task<User?> GetByUsernameAsync(string Username)
        {
            string Query = "FOR u IN @@collection FILTER u.username == @usr RETURN u";
            
            var BindVars = new Dictionary<string, object> 
            { 
                { "@collection", CollectionName },
                { "usr", Username } 
            };

            var Response = await _databaseClient.Cursor.PostCursorAsync<User>(new PostCursorBody 
            { 
                Query = Query, 
                BindVars = BindVars 
            });

            return Response.Result.FirstOrDefault();
        }

        public async Task<User?> GetByUsernameExcludingKeyAsync(string Username, string ExcludedKey)
        {
            string Query = "FOR u IN @@collection FILTER u.username == @usr AND u._key != @key RETURN u";
            
            var BindVars = new Dictionary<string, object> 
            { 
                { "@collection", CollectionName },
                { "usr", Username },
                { "key", ExcludedKey }
            };

            var Response = await _databaseClient.Cursor.PostCursorAsync<User>(new PostCursorBody 
            { 
                Query = Query, 
                BindVars = BindVars 
            });

            return Response.Result.FirstOrDefault();
        }

        public async Task<User?> GetByKeyAsync(string Key)
        {
            string Query = "RETURN DOCUMENT(@@collection, @key)";
            
            var BindVars = new Dictionary<string, object> 
            { 
                { "@collection", CollectionName },
                { "key", Key } 
            };

            var Response = await _databaseClient.Cursor.PostCursorAsync<User>(new PostCursorBody 
            { 
                Query = Query, 
                BindVars = BindVars 
            });

            return Response.Result.FirstOrDefault();
        }

        public async Task CreateUserAsync(object DocumentToInsert)
        {
            string Query = "INSERT @doc INTO @@collection RETURN NEW";
            
            var BindVars = new Dictionary<string, object> 
            { 
                { "@collection", CollectionName },
                { "doc", DocumentToInsert } 
            };

            await _databaseClient.Cursor.PostCursorAsync<object>(new PostCursorBody 
            { 
                Query = Query, 
                BindVars = BindVars 
            });
        }

        public async Task UpdateUserAsync(string UpdateQuery, Dictionary<string, object> BindVars)
        {
            await _databaseClient.Cursor.PostCursorAsync<object>(new PostCursorBody 
            { 
                Query = UpdateQuery, 
                BindVars = BindVars 
            });
        }

        public async Task DeleteUserAsync(string Key)
        {
            string Query = "REMOVE @key IN @@collection";
            
            var BindVars = new Dictionary<string, object> 
            { 
                { "@collection", CollectionName },
                { "key", Key } 
            };

            await _databaseClient.Cursor.PostCursorAsync<object>(new PostCursorBody 
            { 
                Query = Query, 
                BindVars = BindVars 
            });
        }

        public async Task UpdateSessionTokenAsync(string UserKey, string SessionToken)
        {
            string Query = "UPDATE @key WITH { sessionToken: @sessionToken } IN @@collection";
            
            var BindVars = new Dictionary<string, object>
            {
                { "@collection", CollectionName },
                { "key", UserKey },
                { "sessionToken", SessionToken }
            };

            await _databaseClient.Cursor.PostCursorAsync<object>(new PostCursorBody
            {
                Query = Query,
                BindVars = BindVars
            });
        }
    }
}