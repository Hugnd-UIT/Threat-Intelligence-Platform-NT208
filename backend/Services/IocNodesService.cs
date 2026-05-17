using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.DTOs;
using backend.Models;
using backend.Repositories;

namespace backend.Services
{
    public class IocNodesService 
    {
        private readonly IocNodesRepository _repository;

        public IocNodesService(IocNodesRepository repository)
        {
            _repository = repository;
        }

        public async Task<IocNodeResponse?> GetByIdAsync(string id)
        {
            var node = await _repository.GetByIdAsync(id);
            if (node == null) return null;
            return MapToResponse(node);
        }

        public async Task<IocNodeResponse?> GetByValueAsync(string value)
        {
            var node = await _repository.GetByValueAsync(value);
            if (node == null) return null;
            return MapToResponse(node);
        }

        public async Task<IEnumerable<IocNodeResponse>> GetAllAsync(int offset, int limit)
        {
            var nodes = await _repository.GetAllAsync(offset, limit);
            return nodes.Select(MapToResponse);
        }

        public async Task<PagedResult<IocNodeResponse>> GetAllPagedAsync(int offset, int limit, string? type = null, string? keyword = null)
        {
            var nodes = await _repository.GetAllAsync(offset, limit, type, keyword);
            var totalCount = await _repository.GetCountAsync(type, keyword);

            return new PagedResult<IocNodeResponse>
            {
                Items = nodes.Select(MapToResponse),
                TotalCount = totalCount,
                Page = (offset / limit) + 1,
                Limit = limit
            };
        }
        
        public async Task<IocNodeResponse> CreateAsync(CreateIocNodeRequest request)
        {
            var node = new IocNode
            {
                Type = request.Type,
                Value = request.Value.Trim(),
                RiskScore = request.RiskScore,
                Country = request.Country,
                Tags = request.Tags ?? new List<string>(),
                OriginRef = request.OriginRef,
                CreatedAt = DateTime.UtcNow 
            };

            var createdNode = await _repository.CreateAsync(node);
            return MapToResponse(createdNode);
        }

        public async Task<IocNodeResponse?> UpdateAsync(string id, UpdateIocNodeRequest request)
        {
            var existingNode = await _repository.GetByIdAsync(id);
            if (existingNode == null) return null;

            if (request.RiskScore.HasValue) 
                existingNode.RiskScore = request.RiskScore.Value;
            
            if (request.Country != null) 
                existingNode.Country = request.Country;
            
            if (request.Tags != null) 
            {
                existingNode.Tags = existingNode.Tags.Concat(request.Tags).Distinct().ToList();
            }

            if (request.OriginRef != null)
                existingNode.OriginRef = request.OriginRef;

            existingNode.UpdatedAt = DateTime.UtcNow;

            var updatedNode = await _repository.UpdateAsync(id, existingNode);
            return updatedNode != null ? MapToResponse(updatedNode) : null;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<bool> DeleteAllAsync()
        {
            try
            {
                return await _repository.DeleteAllIocsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi ở tầng Service khi xóa toàn bộ IOC: {ex.Message}");
            }
        }

        private IocNodeResponse MapToResponse(IocNode node)
        {
            return new IocNodeResponse
            {
                Id = node._key ?? string.Empty, // ✅ Đã cập nhật thành _key
                Type = node.Type,
                Value = node.Value,
                RiskScore = node.RiskScore,
                Country = node.Country,
                Tags = node.Tags ?? new List<string>(),
                OriginRef = node.OriginRef,
                CreatedAt = node.CreatedAt,
                UpdatedAt = node.UpdatedAt 
            };
        }

        public async Task<bool> CreateRelationshipAsync(CreateRelationshipRequest request)
        {
            var fromNode = await _repository.GetByValueAsync(request.FromValue.Trim());
            if (fromNode == null || string.IsNullOrEmpty(fromNode._key)) // ✅ Cập nhật thành _key
            {
                throw new Exception($"Không tìm thấy Node nguồn với giá trị: {request.FromValue}. Vui lòng thêm IOC này vào hệ thống trước.");
            }

            var toNode = await _repository.GetByValueAsync(request.ToValue.Trim());
            if (toNode == null || string.IsNullOrEmpty(toNode._key)) // ✅ Cập nhật thành _key
            {
                throw new Exception($"Không tìm thấy Node đích với giá trị: {request.ToValue}. Vui lòng thêm IOC này vào hệ thống trước.");
            }

            return await _repository.CreateRelationshipAsync(
                fromNode._key,
                toNode._key,
                request.RelationType,
                "Manual"
            );
        }
    }
}