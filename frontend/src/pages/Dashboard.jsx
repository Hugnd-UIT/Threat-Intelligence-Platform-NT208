import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axiosClient from '../api/axiosClient';
import { Chart as ChartJS, ArcElement, Tooltip, Legend } from 'chart.js';
import { Pie } from 'react-chartjs-2';
import { jwtDecode } from 'jwt-decode';

ChartJS.register(ArcElement, Tooltip, Legend);

const Dashboard = () => {
    const [stats, setStats] = useState({ 
        totalUsers: 0, totalLogs: 0, totalIocs: 0, iocsToday: 0, totalEdges: 0, topIocs: [] 
    });
    const [chartData, setChartData] = useState(null);
    const [userRole, setUserRole] = useState('');
    const navigate = useNavigate();

    useEffect(() => {
        const token = localStorage.getItem('token');
        if (token) {
            const decoded = jwtDecode(token);
            setUserRole(decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decoded.role);
        }

        const fetchData = async () => {
            try {
                const res = await axiosClient.get('/Dashboard/stats');
                
                setStats({
                    totalUsers: res.data.totalUsers ?? res.data.TotalUsers ?? 0,
                    totalLogs: res.data.totalLogs ?? res.data.TotalLogs ?? 0,
                    totalIocs: res.data.totalIocs ?? res.data.TotalIocs ?? 0,
                    iocsToday: res.data.iocsToday ?? res.data.IocsToday ?? 0,
                    totalEdges: res.data.totalEdges ?? res.data.TotalEdges ?? 0,
                    topIocs: Array.isArray(res.data.topIocs) ? res.data.topIocs : (res.data.TopIocs || [])
                });

                setChartData({
                    labels: ['Users', 'System Logs', 'IOC Nodes', 'Relationships (Edges)'],
                    datasets: [
                        {
                            data: [
                                res.data.totalUsers ?? res.data.TotalUsers ?? 0, 
                                res.data.totalLogs ?? res.data.TotalLogs ?? 0, 
                                res.data.totalIocs ?? res.data.TotalIocs ?? 0, 
                                res.data.totalEdges ?? res.data.TotalEdges ?? 0
                            ],
                            backgroundColor: ['#3b82f6', '#10b981', '#f59e0b', '#8b5cf6'],
                            borderColor: '#1e293b',
                            borderWidth: 2,
                        },
                    ],
                });
            } catch (error) {
                console.error('Error fetching dashboard stats:', error);
            }
        };

        fetchData();
    }, []);

    return (
        <div style={{ backgroundColor: '#0f172a', padding: '25px', borderRadius: '12px', minHeight: '80vh' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '30px' }}>
                <h2 style={{ color: '#fff', margin: 0 }}>📊 SYSTEM OVERVIEW</h2>
                <span style={{ backgroundColor: '#1e293b', padding: '8px 16px', borderRadius: '20px', color: '#94a3b8', fontSize: '0.9rem', border: '1px solid #334155' }}>
                    Role: <strong style={{ color: userRole === 'Admin' ? '#fca5a5' : '#93c5fd' }}>{userRole || 'User'}</strong>
                </span>
            </div>

            <div style={{ display: 'flex', gap: '20px', marginBottom: '30px', flexWrap: 'wrap' }}>
                <div style={userCardStyle}>
                    <div style={{ color: '#94a3b8', fontSize: '1rem', marginBottom: '10px' }}>Total Users</div>
                    <div style={{ color: '#3b82f6', fontSize: '2.5rem' }}>{stats.totalUsers}</div>
                </div>
                <div style={userCardStyle}>
                    <div style={{ color: '#94a3b8', fontSize: '1rem', marginBottom: '10px' }}>System Logs</div>
                    <div style={{ color: '#10b981', fontSize: '2.5rem' }}>{stats.totalLogs}</div>
                </div>
                <div style={adminCardStyle}>
                    <div style={{ color: '#cbd5e1', fontSize: '1.1rem', marginBottom: '10px' }}>Collected IOCs</div>
                    <div style={{ color: '#fff', fontSize: '3rem', fontWeight: '900' }}>{stats.totalIocs}</div>
                </div>
                <div style={adminCardStyle}>
                    <div style={{ color: '#cbd5e1', fontSize: '1.1rem', marginBottom: '10px' }}>Today's IOCs</div>
                    <div style={{ color: '#f59e0b', fontSize: '3rem', fontWeight: '900' }}>+{stats.iocsToday}</div>
                </div>
                <div style={adminCardStyle}>
                    <div style={{ color: '#cbd5e1', fontSize: '1.1rem', marginBottom: '10px' }}>Total Edges</div>
                    <div style={{ color: '#8b5cf6', fontSize: '3rem', fontWeight: '900' }}>{stats.totalEdges}</div>
                </div>
            </div>

            <div style={{ display: 'flex', gap: '25px', flexWrap: 'wrap' }}>
                <div style={chartBoxStyle}>
                    <h3 style={{ color: '#e2e8f0', marginTop: 0, textAlign: 'center', marginBottom: '20px' }}>Data Distribution</h3>
                    <div style={{ width: '100%', height: '300px', display: 'flex', justifyContent: 'center' }}>
                        {chartData ? (
                            <Pie 
                                data={chartData} 
                                options={{
                                    responsive: true,
                                    maintainAspectRatio: false,
                                    plugins: { legend: { position: 'bottom', labels: { color: '#cbd5e1' } } }
                                }} 
                            />
                        ) : (
                            <p style={{ color: '#64748b' }}>Loading chart...</p>
                        )}
                    </div>
                </div>

                <div style={tableBoxStyle}>
                    <h3 style={{ color: '#fca5a5', marginTop: 0, borderBottom: '1px solid #334155', paddingBottom: '15px' }}>
                        🔥 TOP 10 MOST DANGEROUS IOCs
                    </h3>
                    <table style={{ width: '100%', borderCollapse: 'collapse', color: '#e2e8f0' }}>
                        <thead>
                            <tr style={{ borderBottom: '1px solid #334155', textAlign: 'left', color: '#94a3b8' }}>
                                <th style={{ padding: '10px 0' }}>Type</th>
                                <th style={{ padding: '10px 0' }}>Value</th>
                                <th style={{ padding: '10px 0' }}>Source</th>
                                <th style={{ padding: '10px 0' }}>Risk Score</th>
                            </tr>
                        </thead>
                        <tbody>
                            {stats.topIocs.length > 0 ? (
                                stats.topIocs.map((ioc, index) => {
                                    return (
                                        <tr 
                                            key={ioc._key || ioc.id || index} 
                                            onClick={() => navigate(`/search`)}
                                            style={{ borderBottom: '1px solid #1e293b', cursor: 'pointer', transition: '0.2s' }}
                                            onMouseOver={(e) => e.currentTarget.style.backgroundColor = '#1e293b'}
                                            onMouseOut={(e) => e.currentTarget.style.backgroundColor = 'transparent'}
                                        >
                                            <td style={{ padding: '10px 0' }}>
                                                <span style={{ backgroundColor: '#0f172a', padding: '4px 8px', borderRadius: '4px', border: '1px solid #334155', fontSize: '0.85rem' }}>
                                                    {ioc.type || ioc.Type}
                                                </span>
                                            </td>
                                            <td style={{ padding: '10px 0', wordBreak: 'break-all', paddingRight: '10px' }}>{ioc.value || ioc.Value}</td>
                                            <td style={{ padding: '10px 0', color: '#94a3b8' }}>{ioc.originRef || ioc.OriginRef || 'Unknown'}</td>
                                            <td style={{ padding: '10px 0', color: '#ef4444', fontWeight: 'bold' }}>{ioc.riskScore || ioc.RiskScore || 0}</td>
                                        </tr>
                                    );
                                })
                            ) : (
                                <tr>
                                    <td colSpan="4" style={{ textAlign: 'center', color: '#94a3b8', padding: '20px 0' }}>
                                        No data available
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
};

const adminCardStyle = { background: '#1e293b', padding: '20px', borderRadius: '10px', flex: 1, borderLeft: '5px solid #3b82f6' };
const userCardStyle = { background: '#0f172a', padding: '30px', borderRadius: '12px', fontSize: '1.2rem', fontWeight: 'bold', border: '1px solid #334155' };
const chartBoxStyle = { background: '#1e293b', padding: '25px', borderRadius: '12px', flex: 1, minWidth: '300px', border: '1px solid #334155' };
const tableBoxStyle = { background: '#1e293b', padding: '25px', borderRadius: '12px', flex: 2, minWidth: '400px', border: '1px solid #334155' };

export default Dashboard;