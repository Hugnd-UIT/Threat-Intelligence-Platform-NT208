using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Services;
using System.Threading.Tasks;

namespace backend.Controllers
{    
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class LogsController : ControllerBase
    {
        private readonly LogsService _logsService;

        public LogsController(LogsService logsService) 
        { 
            _logsService = logsService; 
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs()
        {
            var Result = await _logsService.GetAuditLogsAsync();
            return Ok(Result);
        }
    }
}