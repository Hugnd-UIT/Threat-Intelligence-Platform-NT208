using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using backend.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace backend.Repositories
{
    public class IocGraphsRepository
    {
        private readonly IArangoDBClient _databaseClient;

        public IocGraphsRepository(IArangoDBClient databaseClient)
        {
            _databaseClient = databaseClient;
        }

        public async Task<GraphDataResponse?> GetThreatGraphDataAsync(string NodeKey)
        {
            string Query = @"
                LET startNodeId = CONCAT('IocNodes/', @nodeKey)
                
                LET campaigns = (FOR v IN 1..1 ANY startNodeId IocRelationships FILTER v.Type == 'Campaign' RETURN v._id)
                
                LET siblings = (
                    FOR campId IN campaigns
                        FOR v IN 1..1 ANY campId IocRelationships
                        FILTER v.Type != 'Campaign' AND v._id != startNodeId
                        LIMIT 50 
                        RETURN v
                )
                
                LET directNeighbors = (FOR v IN 1..1 ANY startNodeId IocRelationships FILTER v.Type != 'Campaign' RETURN v)
                
                LET allNodes = UNIQUE(APPEND(siblings, directNeighbors))
                
                // Lấy danh sách ID của tất cả Node sẽ hiển thị để làm mốc so sánh
                LET allVisibleIds = APPEND(allNodes[*]._id, [startNodeId])
                
                LET nodes = (
                    FOR v IN allNodes 
                    LET daysOld = HAS(v, 'CreatedAt') ? DATE_DIFF(v.CreatedAt, DATE_NOW(), 'day') : 0
                    LET decayAmount = FLOOR(daysOld / 7) * 5
                    LET dynamicScore = MAX([0, v.RiskScore - decayAmount])
                    
                    // Kiểm tra xem Node này có Edge nào nối ra 'người lạ' không
                    LET hiddenEdges = (
                        FOR e IN IocRelationships
                        FILTER (e._from == v._id AND e._to NOT IN allVisibleIds) OR (e._to == v._id AND e._from NOT IN allVisibleIds)
                        LIMIT 1 RETURN 1
                    )
                    
                    RETURN DISTINCT {
                        id: v._id,
                        name: v.Value,
                        type: v.Type,
                        val: (dynamicScore / 10) + 1, 
                        color: dynamicScore >= 80 ? '#ff7b72' : (dynamicScore >= 50 ? '#d29922' : '#238636'),
                        actualRiskScore: dynamicScore,
                        isExpandable: LENGTH(hiddenEdges) > 0 
                    }
                )
                
                LET realLinks = (FOR e IN IocRelationships FILTER e._from IN allVisibleIds AND e._to IN allVisibleIds AND e.RelationType != 'belongs_to' RETURN DISTINCT { source: e._from, target: e._to, name: e.RelationType })
                LET virtualLinks = (FOR n IN allNodes RETURN { source: startNodeId, target: n._id, name: 'shared_campaign' })
                LET links = UNIQUE(APPEND(realLinks, virtualLinks))
                
                LET rootNode = DOCUMENT(startNodeId)
                LET rootDaysOld = HAS(rootNode, 'CreatedAt') ? DATE_DIFF(rootNode.CreatedAt, DATE_NOW(), 'day') : 0
                LET rootScore = MAX([0, rootNode.RiskScore - (FLOOR(rootDaysOld / 7) * 5)])
                
                LET rootHiddenEdges = (
                    FOR e IN IocRelationships
                    FILTER (e._from == rootNode._id AND e._to NOT IN allVisibleIds) OR (e._to == rootNode._id AND e._from NOT IN allVisibleIds)
                    LIMIT 1 RETURN 1
                )
                
                LET rootNodeFormatted = { 
                    id: rootNode._id, 
                    name: rootNode.Value, 
                    type: rootNode.Type, 
                    val: (rootScore / 10) + 3,
                    color: '#a371f7',
                    actualRiskScore: rootScore,
                    isExpandable: LENGTH(rootHiddenEdges) > 0
                }
                
                RETURN { nodes: APPEND(nodes, [rootNodeFormatted], true), links: links }";

            var BindVars = new Dictionary<string, object> 
            { 
                { "nodeKey", NodeKey } 
            };

            var Response = await _databaseClient.Cursor.PostCursorAsync<GraphDataResponse>(new PostCursorBody 
            { 
                Query = Query, 
                BindVars = BindVars 
            });

            return Response.Result.FirstOrDefault();
        }

        public async Task<GraphDataResponse?> ExpandGraphDataAsync(string NodeKey, int Skip)
        {
            string Query = @"
                LET startNodeId = CONCAT('IocNodes/', @nodeKey)
                LET campaigns = (FOR v IN 1..1 ANY startNodeId IocRelationships FILTER v.Type == 'Campaign' RETURN v._id)
                LET siblings = (FOR campId IN campaigns FOR v IN 1..1 ANY campId IocRelationships FILTER v.Type != 'Campaign' AND v._id != startNodeId RETURN v)
                LET directNeighbors = (FOR v IN 1..1 ANY startNodeId IocRelationships FILTER v.Type != 'Campaign' RETURN v)
                
                LET combined = UNIQUE(APPEND(siblings, directNeighbors))
                LET allNodes = (FOR v IN combined SORT v._id ASC LIMIT @skip, 20 RETURN v)
                LET allVisibleIds = APPEND(allNodes[*]._id, [startNodeId])
                
                LET nodes = (
                    FOR v IN allNodes 
                    LET daysOld = HAS(v, 'CreatedAt') ? DATE_DIFF(v.CreatedAt, DATE_NOW(), 'day') : 0
                    LET dynamicScore = MAX([0, v.RiskScore - (FLOOR(daysOld / 7) * 5)])
                    
                    LET hiddenEdges = (
                        FOR e IN IocRelationships
                        FILTER (e._from == v._id AND e._to NOT IN allVisibleIds) OR (e._to == v._id AND e._from NOT IN allVisibleIds)
                        LIMIT 1 RETURN 1
                    )
                    
                    RETURN DISTINCT {
                        id: v._id,
                        name: v.Value,
                        type: v.Type,
                        val: (dynamicScore / 10) + 1, 
                        color: dynamicScore >= 80 ? '#ff7b72' : (dynamicScore >= 50 ? '#d29922' : '#238636'),
                        actualRiskScore: dynamicScore,
                        isExpandable: LENGTH(hiddenEdges) > 0
                    }
                )
                
                LET realLinks = (FOR e IN IocRelationships FILTER e._from IN allVisibleIds AND e._to IN allVisibleIds AND e.RelationType != 'belongs_to' RETURN DISTINCT { source: e._from, target: e._to, name: e.RelationType })
                LET virtualLinks = (FOR n IN allNodes RETURN { source: startNodeId, target: n._id, name: 'shared_campaign' })
                
                RETURN { nodes: nodes, links: UNIQUE(APPEND(realLinks, virtualLinks)) }";

            var BindVars = new Dictionary<string, object> 
            { 
                { "nodeKey", NodeKey }, 
                { "skip", Skip } 
            };

            var Response = await _databaseClient.Cursor.PostCursorAsync<GraphDataResponse>(new PostCursorBody 
            { 
                Query = Query, 
                BindVars = BindVars 
            });

            return Response.Result.FirstOrDefault();
        }
    }
}