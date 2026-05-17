using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Services;
using System.Threading.Tasks;
using System;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class IocGraphsController : ControllerBase
    {
        private readonly IocGraphsService _iocGraphsService;

        public IocGraphsController(IocGraphsService iocGraphsService)
        {
            _iocGraphsService = iocGraphsService;
        }

        [HttpGet("{NodeKey}")]
        public async Task<IActionResult> GetThreatGraph(string NodeKey)
        {
            try
            {
                var GraphData = await _iocGraphsService.GetThreatGraphDataAsync(NodeKey);

                if (GraphData == null)
                {
                    return NotFound(new 
                    { 
                        Message = "Threat graph data not found!" 
                    });
                }

                return Ok(GraphData);
            }
            catch (Exception ExceptionInstance)
            {
                return StatusCode(500, new 
                { 
                    Message = "Error scanning threat graph", 
                    Error = ExceptionInstance.Message 
                });
            }
        }

        [HttpGet("expand/{NodeKey}")]
        public async Task<IActionResult> ExpandGraph(string NodeKey, [FromQuery] int Skip = 0)
        {
            try
            {
                var GraphData = await _iocGraphsService.ExpandGraphDataAsync(NodeKey, Skip);

                if (GraphData == null)
                {
                    return NotFound(new 
                    { 
                        Message = "No data available!" 
                    });
                }

                return Ok(GraphData);
            }
            catch (Exception ExceptionInstance)
            {
                return StatusCode(500, new 
                { 
                    Message = "Error expanding threat graph", 
                    Error = ExceptionInstance.Message 
                });
            }
        }
    }
}