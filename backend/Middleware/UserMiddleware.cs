using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace backend.Middlewares 
{
    public class UserMiddleware 
    {
        private readonly RequestDelegate _next;

        public UserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IArangoDBClient db)
        {
            var username = context.User.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                var cursor = await db.Cursor.PostCursorAsync<dynamic>(new PostCursorBody {
                    Query = "FOR u IN Users FILTER u.username == @usr RETURN u",
                    BindVars = new System.Collections.Generic.Dictionary<string, object> { { "usr", username } }
                });
                var user = cursor.Result.FirstOrDefault();
                if (user != null && (bool)(user.isLocked ?? false))
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { message = "Your account has been locked!" });
                    return;
                }
            }
            await _next(context);
        }
    }
}