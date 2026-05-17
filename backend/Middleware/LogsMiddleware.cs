using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace backend.Middlewares 
{
    public class LogsMiddleware 
    {
        private readonly RequestDelegate _next;

        public LogsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IArangoDBClient db)
        {
            await _next(context);

            var method = context.Request.Method;
            
            if (method == "GET" || method == "POST" || method == "PUT" || method == "DELETE" || method == "PATCH")
            {
                var path = context.Request.Path.ToString();

                if (method == "GET" && (path.Contains("/swagger") || path.Contains("/favicon"))) 
                {
                    return;
                }

                var username = context.User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var ipAddress = context.Connection.RemoteIpAddress;
                string ip = "Unknown IP";

                if (ipAddress != null)
                {
                    if (ipAddress.IsIPv4MappedToIPv6) ip = ipAddress.MapToIPv4().ToString();
                    else ip = ipAddress.ToString();
                }

                if (ip == "::1") ip = "127.0.0.1";

                var logEntry = new {
                    Timestamp = DateTime.UtcNow,
                    Action = method,
                    Resource = path,
                    Username = username,    
                    ClientIp = ip,      
                    StatusCode = context.Response.StatusCode
                };
                
                try {
                    await db.Cursor.PostCursorAsync<object>(new PostCursorBody {
                        Query = "INSERT @log INTO AuditLogs",
                        BindVars = new Dictionary<string, object> { { "log", logEntry } }
                    });
                } catch { }
            }
        }
    }
}