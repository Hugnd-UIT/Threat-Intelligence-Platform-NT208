using backend.Repositories;
using System.Threading.Tasks;

namespace backend.Services
{
    public class DashboardService
    {
        private readonly DashboardRepository _dashboardRepository;

        public DashboardService(DashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<object> GetSystemStatsAsync()
        {
            var UsersCount = await _dashboardRepository.GetCollectionCountAsync("Users");
            var LogsCount = await _dashboardRepository.GetCollectionCountAsync("AuditLogs");
            var TotalIocs = await _dashboardRepository.GetCollectionCountAsync("IocNodes");
            var TotalEdges = await _dashboardRepository.GetCollectionCountAsync("IocRelationships");
            var IocsToday = await _dashboardRepository.GetIocsTodayCountAsync();
            var TopIocs = await _dashboardRepository.GetTopRiskIocsAsync();

            return new 
            { 
                TotalUsers = UsersCount, 
                TotalLogs = LogsCount,
                TotalIocs = TotalIocs,
                IocsToday = IocsToday,
                TotalEdges = TotalEdges,
                TopIocs = TopIocs 
            };
        }
    }
}