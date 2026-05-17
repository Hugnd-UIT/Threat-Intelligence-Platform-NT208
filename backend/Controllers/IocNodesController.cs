using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IocNodesController : ControllerBase
    {
        private readonly IocNodesService _iocNodesService;

        public IocNodesController(IocNodesService iocNodesService)
        {
            _iocNodesService = iocNodesService;
        }

        [HttpGet]
        [Authorize]        
        public async Task<IActionResult> GetAll([FromQuery] int Offset = 0, [FromQuery] int Limit = 50)
        {
            var Result = await _iocNodesService.GetAllAsync(Offset, Limit);
            return Ok(Result);
        }

        [HttpGet("{Id}")]
        public async Task<IActionResult> GetById([FromRoute] string Id)
        {
            var Result = await _iocNodesService.GetByIdAsync(Id);
            
            if (Result == null)
            {
                return NotFound(new { Message = $"IOC with ID {Id} not found!" });
            }

            return Ok(Result);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateIocNodeRequest Request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var ExistingNode = await _iocNodesService.GetByValueAsync(Request.Value);

                if (ExistingNode != null)
                {
                    return StatusCode(409, new
                    {
                        Message = $"Node '{Request.Value}' already exists in the system!",
                        Source = ExistingNode.OriginRef,
                        ExistingKey = ExistingNode.Id
                    });
                }

                var CurrentUser = User.Identity?.Name ?? "Unknown";
                Request.OriginRef = CurrentUser;

                var Result = await _iocNodesService.CreateAsync(Request);
                return CreatedAtAction(nameof(GetById), new { id = Result.Id }, Result);
            }
            catch (Exception ExceptionInstance)
            {
                return StatusCode(500, new { Message = "System error while creating IOC", Error = ExceptionInstance.Message });
            }
        }

        [HttpPost("relationship")]
        [Authorize]
        public async Task<IActionResult> CreateRelationship([FromBody] CreateRelationshipRequest Request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var Success = await _iocNodesService.CreateRelationshipAsync(Request);
                if (Success) return Ok(new { Message = "Relationship created successfully!" });
                return BadRequest(new { Message = "Could not create relationship. Please check the Keys of the 2 Nodes." });
            }
            catch (System.Exception ExceptionInstance)
            {
                return StatusCode(500, new { Message = $"System error: {ExceptionInstance.Message}" });
            }
        }

        [HttpPut("{Id}")]
        [Authorize]
        public async Task<IActionResult> Update([FromRoute] string Id, [FromBody] UpdateIocNodeRequest Request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var CurrentUser = User.Identity?.Name;
                var IsAdmin = User.IsInRole("Admin");

                var ExistingIoc = await _iocNodesService.GetByIdAsync(Id);
                
                if (ExistingIoc == null) return NotFound(new { Message = $"IOC with ID {Id} not found for update" });

                if (!IsAdmin && ExistingIoc.OriginRef != CurrentUser)
                {
                    return StatusCode(403, new { Message = "Warning: You do not have permission to edit data created by others!" });
                }

                var Result = await _iocNodesService.UpdateAsync(Id, Request);
                return Ok(Result);
            }
            catch (Exception ExceptionInstance)
            {
                return StatusCode(500, new { Message = "System error while updating", Error = ExceptionInstance.Message });
            }
        }

        [HttpDelete("{Id}")]
        [Authorize]
        public async Task<IActionResult> Delete([FromRoute] string Id)
        {
            try
            {
                var CurrentUser = User.Identity?.Name;
                var IsAdmin = User.IsInRole("Admin");

                var ExistingIoc = await _iocNodesService.GetByIdAsync(Id);
                
                if (ExistingIoc == null) return NotFound(new { Message = "IOC not found!" });

                if (!IsAdmin && ExistingIoc.OriginRef != CurrentUser)
                {
                    return StatusCode(403, new { Message = "Warning: You do not have permission to delete data created by others!" });
                }

                await _iocNodesService.DeleteAsync(Id);
                return Ok(new { Message = "IOC deleted successfully!" });
            }
            catch (Exception ExceptionInstance)
            {
                return StatusCode(500, new { Message = "System error", Error = ExceptionInstance.Message });
            }
        }

        [HttpDelete("all")]
        [Authorize] 
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                await _iocNodesService.DeleteAllAsync();
                return Ok(new { Message = "Successfully deleted all IOC data and relationships!" });
            }
            catch (Exception ExceptionInstance)
            {
                return StatusCode(500, new { Message = ExceptionInstance.Message });
            }
        }

        [HttpGet("paged")]
        [Authorize]
        public async Task<IActionResult> GetAllPaged([FromQuery] int Offset = 0, [FromQuery] int Limit = 50, [FromQuery] string? Type = null, [FromQuery] string? Keyword = null)
        {
            var Result = await _iocNodesService.GetAllPagedAsync(Offset, Limit, Type, Keyword);
            return Ok(Result);
        }
    }
}