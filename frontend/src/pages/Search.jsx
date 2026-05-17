import React, { useState, useCallback, useRef, useEffect } from 'react';
import ForceGraph2D from 'react-force-graph-2d';
import axiosClient from '../api/axiosClient';
import jsPDF from 'jspdf';
import html2canvas from 'html2canvas';

function Search() {
  const [searchInput, setSearchInput] = useState('');
  const [searchResult, setSearchResult] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [graphData, setGraphData] = useState({ nodes: [], links: [] });
  const [isExporting, setIsExporting] = useState(false);
  const [selectedNode, setSelectedNode] = useState(null);
  const [hoverNode, setHoverNode] = useState(null);

  const expandStatsRef = useRef({});
  const fgRef = useRef();

  useEffect(() => {
    if (fgRef.current) {
      fgRef.current.d3Force('charge').strength(-800);
      fgRef.current.d3Force('link').distance(150);
      fgRef.current.d3ReheatSimulation();
    }
  }, [graphData]);

  const handleSearch = async (event) => {
    event.preventDefault();
    if (searchInput.trim() === '') return;

    setIsLoading(true);
    setSearchResult(null);
    setSelectedNode(null);
    setGraphData({ nodes: [], links: [] });

    try {
      const textResponse = await axiosClient.get(`/Search/${searchInput}`);
      const textData = textResponse.data;

      setSearchResult({
        iocValue: textData.value || textData.Value,
        type: textData.type || textData.Type,
        riskScore: textData.riskScore || textData.RiskScore,
        country: textData.country || textData.Country || 'Unknown',
        asn: textData.originRef || textData.OriginRef || 'N/A',
        tags: textData.tags || textData.Tags || []
      });

      const searchKey = textData._key || textData.Key || textData.key;

      if (searchKey) {
        const graphResponse = await axiosClient.get(`/IocGraphs/${searchKey}`);
        const realGraphData = graphResponse.data;

        const rawNodes = realGraphData.nodes || realGraphData.Nodes || [];
        const rawLinks = realGraphData.links || realGraphData.Links || [];

        const nodeMap = new Map();

        rawNodes.forEach((n) => {
          const id = n.id || n.Id || n._id || n.ID;
          const type = n.type || n.Type || n.TYPE || 'Node';
          let color = n.color || n.Color || n.COLOR || '#8b949e';
          if (type.toLowerCase() === 'domain' && color !== '#a371f7') color = '#58a6ff';

          if (id) {
            nodeMap.set(id, {
              ...n, id, name: n.name || n.Name || n.NAME || 'Unknown', type,
              val: Number(n.val || n.Val || n.VAL) || 5, color, isExpandable: n.isExpandable || false
            });
          }
        });

        const safeNodes = Array.from(nodeMap.values());
        const safeLinks = rawLinks.map((l) => ({
            ...l, source: l.source || l.Source || l.SOURCE || l._from, target: l.target || l.Target || l.TARGET || l._to, name: l.name || l.Name || l.NAME || ''
          })).filter((l) => l.source && l.target && nodeMap.has(l.source) && nodeMap.has(l.target));

        setGraphData({ nodes: safeNodes, links: safeLinks });
        expandStatsRef.current = {};
        expandStatsRef.current[searchKey] = 50;
      }
    } catch (error) {
      if (error.response && error.response.status === 404) alert('Malware footprint not found!');
      else alert('Error connecting to Backend!');
    } finally {
      setIsLoading(false);
    }
  };

  const handleNodeRightClick = useCallback(async (node) => {
    const nodeKey = String(node.id).includes('/') ? node.id.split('/')[1] : node.id;
    const currentSkip = expandStatsRef.current[nodeKey] || 0;

    try {
      const response = await axiosClient.get(`/IocGraphs/expand/${nodeKey}?skip=${currentSkip}`);
      const expandedData = response.data;
      const newNodes = expandedData.nodes || expandedData.Nodes || [];
      const newLinks = expandedData.links || expandedData.Links || [];

      if (newNodes.length === 0) {
        alert('This node is fully expanded!');
        setGraphData((prevData) => ({ nodes: prevData.nodes.map((n) => n.id === node.id ? { ...n, isFullyExpanded: true } : n), links: prevData.links }));
        return;
      }

      expandStatsRef.current[nodeKey] = currentSkip + 20;

      setGraphData((prevData) => {
        const existingNodeIds = new Set(prevData.nodes.map((n) => n.id));
        const uniqueNewNodes = newNodes.filter((n) => n.id && !existingNodeIds.has(n.id)).map((n) => {
            let color = n.color || '#8b949e';
            if ((n.type || '').toLowerCase() === 'domain') color = '#58a6ff';
            return { ...n, name: n.name || 'Unknown', val: Number(n.val) || 5, color, isExpandable: n.isExpandable || false, x: node.x + (Math.random() - 0.5) * 50, y: node.y + (Math.random() - 0.5) * 50 };
          });

        const existingLinkIds = new Set(prevData.links.map((l) => {
            const sourceId = typeof l.source === 'object' ? l.source.id : l.source;
            const targetId = typeof l.target === 'object' ? l.target.id : l.target;
            return `${sourceId}->${targetId}`;
          }));

        const uniqueNewLinks = newLinks.map((l) => ({ ...l, source: l.source || l._from, target: l.target || l._to })).filter(
            (l) => l.source && l.target && !existingLinkIds.has(`${l.source}->${l.target}`)
          );

        return { nodes: [...prevData.nodes, ...uniqueNewNodes], links: [...prevData.links, ...uniqueNewLinks] };
      });
    } catch (error) {
      console.error('Error expanding graph:', error);
    }
  }, []);

  const handleExportPDF = async () => {
    if (!searchResult) return;
    setIsExporting(true);
    try {
      const pdf = new jsPDF('p', 'mm', 'a4');
      pdf.setFont('helvetica', 'bold');
      pdf.setFontSize(22);
      pdf.setTextColor(220, 38, 38);
      pdf.text('THREAT ANALYSIS REPORT', 105, 20, { align: 'center' });
      pdf.setFont('helvetica', 'normal');
      pdf.setFontSize(12);
      pdf.setTextColor(0, 0, 0);

      let currentY = 40;
      const lineSpacing = 8;
      pdf.text(`IOC: ${searchResult.iocValue}`, 20, currentY); currentY += lineSpacing;
      pdf.text(`Type: ${searchResult.type}`, 20, currentY); currentY += lineSpacing;
      pdf.text(`Risk Score: ${searchResult.riskScore}/100`, 20, currentY); currentY += lineSpacing;
      pdf.text(`Country: ${searchResult.country}`, 20, currentY); currentY += lineSpacing;
      pdf.text(`Origin: ${searchResult.asn}`, 20, currentY); currentY += 15;

      const graphElement = document.querySelector('.graph-panel');
      if (graphElement) {
        const canvas = await html2canvas(graphElement, { scale: 2, backgroundColor: '#0f172a', useCORS: true });
        const imgData = canvas.toDataURL('image/png');
        const imgWidth = 170;
        const imgHeight = (canvas.height * imgWidth) / canvas.width;
        pdf.addImage(imgData, 'PNG', 20, currentY, imgWidth, imgHeight);
      }
      pdf.save(`NexusTIP_Report_${searchResult.iocValue}.pdf`);
    } catch {
      alert('Error exporting PDF!');
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <>
      <style>{`
        .search-page-wrapper{ background:#0d1117; color:#c9d1d9; min-height:100vh; padding:30px; font-family:Inter,sans-serif; }
        .search-header{ text-align:center; margin-bottom:40px; }
        .search-title{ color:#fff; }
        .search-subtitle{ color:#8b949e; margin-bottom:30px; }
        .search-input-group{ display:flex; justify-content:center; gap:15px; max-width:700px; margin:auto; }
        .search-input{ flex:1; padding:16px 24px; font-size:16px; background:#161b22; border:1px solid #30363d; color:#fff; border-radius:8px; outline:none; }
        .search-input:focus{ border-color:#58a6ff; box-shadow:0 0 10px rgba(88,166,255,.2); }
        .btn-search{ padding:0 30px; font-size:16px; font-weight:600; background:#238636; color:#fff; border:none; border-radius:8px; cursor:pointer; }
        .btn-search:hover{ background:#2ea043; }
        .result-container{ display:flex; gap:25px; height:calc(100vh - 200px); }
        .info-panel{ flex:3; background:#161b22; border:1px solid #30363d; border-radius:10px; padding:25px; display:flex; flex-direction:column; word-break:break-all; }
        .info-title{ color:#58a6ff; margin-bottom:20px; }
        .info-row{ display:flex; justify-content:space-between; padding:12px 0; border-bottom:1px solid #21262d; }
        .info-label{ color:#8b949e; }
        .info-value-large{ font-size:18px; }
        .info-risk-high{ color:#ff7b72; font-size:20px; }
        .graph-panel{ flex:7; background:#010409; border:1px solid #30363d; border-radius:10px; position:relative; overflow:hidden; }
      `}</style>
      <div className="search-page-wrapper">
        <div className="search-header">
          <h1 className="search-title">IOC Lookup</h1>
          <p className="search-subtitle">Threat Intelligence Visualization</p>
          <form className="search-input-group" onSubmit={handleSearch}>
            <input type="text" className="search-input" placeholder="Enter IP, Domain or Hash..." value={searchInput} onChange={(e) => setSearchInput(e.target.value)} />
            <button type="submit" className="btn-search" disabled={isLoading}>{isLoading ? 'Scanning...' : 'Search'}</button>
          </form>
        </div>
        {searchResult && (
          <div className="result-container">
            <div className="info-panel">
              <h2 className="info-title">IOC Detail</h2>
              <button onClick={handleExportPDF} disabled={isExporting} style={{ background: '#10b981', color: '#fff', border: 'none', padding: '8px 15px', borderRadius: '6px', cursor: 'pointer', fontWeight: 'bold' }}>
                {isExporting ? 'Exporting...' : '📄 Export PDF'}
              </button>
              <div className="info-row"><span className="info-label">IOC:</span><strong className="info-value-large">{searchResult.iocValue}</strong></div>
              <div className="info-row"><span className="info-label">Type:</span><span>{searchResult.type}</span></div>
              <div className="info-row"><span className="info-label">Risk:</span><strong className="info-risk-high">{searchResult.riskScore}/100</strong></div>
              <div className="info-row"><span className="info-label">Source:</span><span>{searchResult.country} - {searchResult.asn}</span></div>
            </div>
            <div className="graph-panel">
              <ForceGraph2D ref={fgRef} graphData={graphData} width={700} height={500} onNodeClick={(node) => setSelectedNode(node)} onNodeRightClick={handleNodeRightClick} onNodeHover={(node) => setHoverNode(node)} />
            </div>
          </div>
        )}
      </div>
    </>
  );
}

export default Search;