using backend.Repositories;
using System.Threading.Tasks;

namespace backend.Services
{
    public class IocGraphsService
    {
        private readonly IocGraphsRepository _iocGraphsRepository;

        public IocGraphsService(IocGraphsRepository iocGraphsRepository)
        {
            _iocGraphsRepository = iocGraphsRepository;
        }

        public async Task<object?> GetThreatGraphDataAsync(string NodeKey)
        {
            return await _iocGraphsRepository.GetThreatGraphDataAsync(NodeKey);
        }

        public async Task<object?> ExpandGraphDataAsync(string NodeKey, int Skip)
        {
            return await _iocGraphsRepository.ExpandGraphDataAsync(NodeKey, Skip);
        }
    }
}