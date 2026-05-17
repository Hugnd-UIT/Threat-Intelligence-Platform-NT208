import React, { useState, useEffect, useRef } from 'react';
import ForceGraph2D from 'react-force-graph-2d';
import axiosClient from '../api/axiosClient';

const IocGraph = () => 
{
    const [searchKey, setSearchKey] = useState('');
    const [graphData, setGraphData] = useState({ nodes: [], links: [] });
    const [selectedNode, setSelectedNode] = useState(null);
    const [isLoading, setIsLoading] = useState(false);
    const [errorMsg, setErrorMsg] = useState('');
    const graphRef = useRef();

    // Hàm gọi API lấy đồ thị gốc dựa trên từ khóa tìm kiếm (IP/Domain/Hash)
    const handleSearchGraph = async (e) => 
    {
        e.preventDefault();
        if (!searchKey.trim()) return;

        setIsLoading(true);
        setErrorMsg('');
        setSelectedNode(null);

        try 
        {
            // Bước 1: Tìm xem Node đó có tồn tại để lấy NodeKey không thông qua API Search toàn cục
            const searchRes = await axiosClient.get(`/Search/${searchKey.trim()}`);
            const nodeKey = searchRes.data.id || searchRes.data._key;

            if (nodeKey) 
            {
                // Bước 2: Gọi API đồ thị mạng nhện từ IocGraphsController mà ta đã tối ưu
                const graphRes = await axiosClient.get(`/IocGraphs/${nodeKey}`);
                setGraphData(graphRes.data);
                
                // Tự động căn giữa đồ thị sau khi tải xong
                setTimeout(() => 
                {
                    if (graphRef.current) 
                    {
                        graphRef.current.zoomToFit(400, 50);
                    }
                }, 500);
            }
        } 
        catch (err) 
        {
            setGraphData({ nodes: [], links: [] });
            setErrorMsg(err.response?.data?.Message || 'Malware footprint or graph data not found!');
        } 
        finally 
        {
            setIsLoading(false);
        }
    };

    // Hàm gọi API mở rộng đồ thị khi người dùng double-click vào một Node có cờ isExpandable
    const handleNodeDoubleClick = async (node) => 
    {
        if (!node.isExpandable) return;

        try 
        {
            const nodeKey = node.id.split('/')[1]; // Tách lấy phần Key từ ID mẫu 'IocNodes/12345'
            const currentSkip = graphData.nodes.filter(n => n.id !== node.id).length;
            
            const expandRes = await axiosClient.get(`/IocGraphs/expand/${nodeKey}?skip=${currentSkip}`);
            
            if (expandRes.data && expandRes.data.nodes.length > 0) 
            {
                // Gom các Node và Link mới vào bộ dữ liệu hiện tại, loại bỏ trùng lặp
                const newNodes = [...graphData.nodes];
                expandRes.data.nodes.forEach(n => 
                {
                    if (!newNodes.some(existNode => existNode.id === n.id)) 
                    {
                        newNodes.push(n);
                    }
                });

                const newLinks = [...graphData.links];
                expandRes.data.links.forEach(l => 
                {
                    if (!newLinks.some(existLink => existLink.source === l.source && existLink.target === l.target)) 
                    {
                        newLinks.push(l);
                    }
                });

                setGraphData({ nodes: newNodes, links: newLinks });
            }
        } 
        catch (err) 
        {
            console.error('Failed to expand graph', err);
        }
    };

    return (
        <div style={{ display: 'flex', flex: 1, gap: '20px', height: 'calc(100vh - 160px)', position: 'relative' }}>
            
            {/* Vùng hiển thị Đồ thị bên trái */}
            <div style={{ flex: 1, backgroundColor: '#0b1329', borderRadius: '12px', border: '1px solid #1e293b', overflow: 'hidden', position: 'relative' }}>
                
                {/* Thanh tìm kiếm nhanh IOC nằm đè trên bản đồ */}
                <form onSubmit={handleSearchGraph} style={{ position: 'absolute', top: '20px', left: '20px', zIndex: 10, display: 'flex', gap: '10px' }}>
                    <input 
                        type="text"
                        placeholder="Enter IP, Domain or Hash to trace..."
                        value={searchKey}
                        onChange={(e) => setSearchKey(e.target.value)}
                        style={{ padding: '10px 16px', backgroundColor: '#020617', border: '1px solid #334155', borderRadius: '8px', color: '#f8fafc', width: '280px', outline: 'none' }}
                    />
                    <button type="submit" style={{ padding: '10px 20px', backgroundColor: '#3b82f6', color: '#ffffff', border: 'none', borderRadius: '8px', cursor: 'pointer', fontWeight: 'bold' }}>
                        {isLoading ? 'Tracing...' : 'Trace'}
                    </button>
                </form>

                {errorMsg && (
                    <div style={{ position: 'absolute', top: '80px', left: '20px', zIndex: 10, backgroundColor: '#7f1d1d', color: '#fca5a5', padding: '8px 16px', borderRadius: '6px', fontSize: '14px', border: '1px solid #b91c1c' }}>
                        {errorMsg}
                    </div>
                )}

                {/* Nhúng Canvas mạng nhện Force Graph */}
                {graphData.nodes.length > 0 ? (
                    <ForceGraph2D
                        ref={graphRef}
                        graphData={graphData}
                        nodeLabel="name"
                        nodeColor={node => node.color}
                        nodeVal={node => node.val}
                        linkDirectionalArrowLength={4}
                        linkDirectionalArrowRelPos={1}
                        linkLabel="name"
                        linkColor={() => '#475569'}
                        linkWidth={() => 1.5}
                        onNodeClick={(node) => setSelectedNode(node)}
                        onNodeRightClick={(node) => handleNodeDoubleClick(node)} // Hỗ trợ cả kích chuột phải để expand cho tiện
                        backgroundColor="#0b1329"
                    />
                ) : (
                    <div style={{ display: 'flex', height: '100%', alignItems: 'center', justifyContent: 'center', color: '#64748b', flexDirection: 'column', gap: '10px' }}>
                        <svg width="48" height="48" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z" />
                        </svg>
                        <span>Enter an IOC value above to map the threat relationship network.</span>
                    </div>
                )}
            </div>

            {/* Bảng Panel hiển thị thông tin điều tra chi tiết bên phải */}
            <div style={{ width: '320px', backgroundColor: '#0f172a', borderRadius: '12px', border: '1px solid #1e293b', padding: '20px', display: 'flex', flexDirection: 'column', gap: '15px' }}>
                <h3 style={{ margin: 0, fontSize: '18px', color: '#3b82f6', borderBottom: '1px solid #1e293b', paddingBottom: '10px' }}>Investigation Panel</h3>
                
                {selectedNode ? (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', fontSize: '14px' }}>
                        <div>
                            <span style={{ color: '#64748b', display: 'block' }}>IOC Value:</span>
                            <strong style={{ color: '#f8fafc', fontSize: '16px', wordBreak: 'break-all' }}>{selectedNode.name}</strong>
                        </div>
                        <div>
                            <span style={{ color: '#64748b', display: 'block' }}>Type:</span>
                            <span style={{ backgroundColor: '#1e293b', padding: '2px 8px', borderRadius: '4px', color: '#38bdf8', fontWeight: 'bold', fontSize: '12px' }}>{selectedNode.type}</span>
                        </div>
                        <div>
                            <span style={{ color: '#64748b', display: 'block' }}>Calculated Risk Score:</span>
                            <strong style={{ color: selectedNode.actualRiskScore >= 80 ? '#f87171' : (selectedNode.actualRiskScore >= 50 ? '#fbbf24' : '#4ade80'), fontSize: '20px' }}>
                                {selectedNode.actualRiskScore} / 100
                            </strong>
                        </div>
                        {selectedNode.isExpandable && (
                            <div style={{ marginTop: '10px', padding: '10px', backgroundColor: '#1d4ed8', color: '#eff6ff', borderRadius: '6px', fontSize: '12px', textAlign: 'center' }}>
                                💡 Right-click this node to expand hidden threat links!
                            </div>
                        )}
                    </div>
                ) : (
                    <div style={{ color: '#475569', fontSize: '14px', textAlign: 'center', marginTop: '40px' }}>
                        Click on any node in the graph network to investigate its micro-intelligence metrics.
                    </div>
                )}
            </div>
        </div>
    );
};

export default IocGraph;