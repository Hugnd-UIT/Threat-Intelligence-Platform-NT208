using backend.Models;
using Microsoft.AspNetCore.Mvc;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SearchController : ControllerBase
    {
        private readonly SearchService _searchService;

        public SearchController(SearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpGet("{Keyword}")]
        public async Task<IActionResult> GlobalSearch(string Keyword)
        {
            try
            {
                var Result = await _searchService.SearchGlobalAsync(Keyword);

                if (Result == null)
                {
                    return NotFound(new 
                    { 
                        Message = "Malware traces not found!" 
                    });
                }

                return Ok(Result);
            }
            catch (Exception ExceptionInstance)
            {
                return StatusCode(500, new 
                { 
                    Message = "System error during lookup", 
                    Error = ExceptionInstance.Message 
                });
            }
        }
    }
}