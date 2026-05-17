using Microsoft.AspNetCore.Mvc;
using backend.Services;
using System;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemController : ControllerBase
    {
        private readonly SystemService _systemService;

        public SystemController(SystemService systemService) 
        { 
            _systemService = systemService; 
        }

        [HttpGet("health-check")]
        public async Task<IActionResult> HealthCheck()
        {
            try 
            {
                var Result = await _systemService.GetHealthStatusAsync();
                return Ok(Result);
            } 
            catch (Exception ExceptionInstance) 
            {
                return StatusCode(500, new 
                { 
                    Status = "Dead", 
                    Error = ExceptionInstance.Message 
                });
            }
        }
    }
}