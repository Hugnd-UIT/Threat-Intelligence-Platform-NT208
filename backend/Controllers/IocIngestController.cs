using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class IocIngestController : ControllerBase
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public IocIngestController(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        [HttpPost("sync/alienvault")]
        public IActionResult SyncAlienVault()
        {
            Task.Run(async () =>
            {
                using (var Scope = _scopeFactory.CreateScope())
                {
                    var IngestService = Scope.ServiceProvider.GetRequiredService<IocIngestService>();
                    await IngestService.SyncAlienVaultDataAsync();
                }
            });

            return Accepted(new 
            { 
                Message = "Data sync command received. The system is synchronizing tens of thousands of IOCs in the background. This process may take several minutes, please check the Logs on the Dashboard to track progress." 
            });
        }
    }
}