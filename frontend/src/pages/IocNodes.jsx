import React, { useEffect, useState, useCallback } from 'react';
import { useOutletContext } from 'react-router-dom';
import axiosClient from '../api/axiosClient';
import { jwtDecode } from 'jwt-decode';

const IocManagement = () => {
    const [iocs, setIocs] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const context = useOutletContext();
    const searchText = context ? context[0] : '';
    const [page, setPage] = useState(1);
    const [limit, setLimit] = useState(10);
    const [totalCount, setTotalCount] = useState(0);
    const [typeFilter, setTypeFilter] = useState('');
    const [inputPage, setInputPage] = useState('1');
    const [showAddForm, setShowAddForm] = useState(false);
    const [newIoc, setNewIoc] = useState({ type: 'IP', value: '', riskScore: 50, country: '' });
    const [showRelationForm, setShowRelationForm] = useState(false);
    const [newRelation, setNewRelation] = useState({ fromValue: '', toValue: '', relationType: 'related_to' });
    const [userRole, setUserRole] = useState('User');

    useEffect(() => {
        const token = localStorage.getItem('token');
        if (token) {
            const decoded = jwtDecode(token);
            setUserRole(decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decoded.role);
        }
    }, []);

    const fetchIocs = useCallback(async () => {
        setLoading(true);
        setError('');
        try {
            const offset = (page - 1) * limit;
            let url = `/IocNodes/paged?offset=${offset}&limit=${limit}`;
            
            if (typeFilter) url += `&type=${typeFilter}`;
            if (searchText) url += `&keyword=${encodeURIComponent(searchText)}`;

            const res = await axiosClient.get(url);
            if (res.data && res.data.items) {
                setIocs(res.data.items);
                setTotalCount(res.data.totalCount || 0);
            } else {
                setIocs([]);
                setTotalCount(0);
            }
        } catch (err) {
            setError(err.response?.data?.message || err.message || "Error fetching IOC data.");
        } finally {
            setLoading(false);
        }
    }, [page, limit, typeFilter, searchText]);

    useEffect(() => {
        setPage(1);
        setInputPage('1');
    }, [searchText, typeFilter]);

    useEffect(() => {
        fetchIocs();
    }, [fetchIocs]);

    const handleInputPageChange = (e) => {
        setInputPage(e.target.value);
    };

    const handleInputPageSubmit = (e) => {
        if (e.key === 'Enter' || e.type === 'blur') {
            const newPage = parseInt(inputPage, 10);
            const maxPage = Math.ceil(totalCount / limit) || 1;
            if (!isNaN(newPage) && newPage >= 1 && newPage <= maxPage) {
                setPage(newPage);
            } else {
                setInputPage(page.toString());
            }
        }
    };

    const handleDelete = async (id) => {
        if (!window.confirm('Are you sure you want to delete this IOC?')) return;
        try {
            await axiosClient.delete(`/IocNodes/${id}`);
            fetchIocs();
        } catch (err) {
            alert('Error deleting: ' + (err.response?.data?.message || err.message));
        }
    };

    const handleDeleteAll = async () => {
        if (!window.confirm('WARNING: This action will permanently delete ALL IOC data and relationships in the database! Are you absolutely sure?')) return;
        try {
            await axiosClient.delete('/IocNodes/all');
            alert('Database cleared successfully!');
            fetchIocs();
        } catch (err) {
            alert('Error clearing database: ' + (err.response?.data?.message || err.message));
        }
    };

    const handleAddIoc = async (e) => {
        e.preventDefault();
        try {
            await axiosClient.post('/IocNodes', newIoc);
            setShowAddForm(false);
            setNewIoc({ type: 'IP', value: '', riskScore: 50, country: '' });
            fetchIocs();
        } catch (err) {
            alert('Error adding IOC: ' + (err.response?.data?.message || err.message));
        }
    };

    const handleAddRelation = async (e) => {
        e.preventDefault();
        try {
            await axiosClient.post('/IocNodes/relationship', newRelation);
            setShowRelationForm(false);
            setNewRelation({ fromValue: '', toValue: '', relationType: 'related_to' });
            alert('Relationship created successfully!');
        } catch (err) {
            alert('Error creating relationship: ' + (err.response?.data?.message || err.message));
        }
    };

    const totalPages = Math.ceil(totalCount / limit) || 1;

    return (
        <div style={{ backgroundColor: '#0f172a', padding: '25px', borderRadius: '12px', minHeight: '80vh' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px', flexWrap: 'wrap', gap: '10px' }}>
                <h2 style={{ color: '#fff', margin: 0 }}>🗃️ IOC DATABASE MANAGEMENT</h2>
                
                <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
                    <button onClick={() => { setShowAddForm(!showAddForm); setShowRelationForm(false); }} style={btnStyle('#2563eb')}>
                        {showAddForm ? 'Close Form' : '+ Add Manual IOC'}
                    </button>
                    <button onClick={() => { setShowRelationForm(!showRelationForm); setShowAddForm(false); }} style={btnStyle('#10b981')}>
                        {showRelationForm ? 'Close Form' : '🔗 Create Edge'}
                    </button>
                    {userRole === 'Admin' && (
                        <button onClick={handleDeleteAll} style={btnStyle('#dc2626')}>
                            ⚠️ Clear Database
                        </button>
                    )}
                </div>
            </div>

            {showAddForm && (
                <form onSubmit={handleAddIoc} style={formContainerStyle}>
                    <h3 style={{ color: '#93c5fd', marginTop: 0 }}>Add New IOC</h3>
                    <div style={{ display: 'flex', gap: '15px', flexWrap: 'wrap' }}>
                        <select value={newIoc.type} onChange={e => setNewIoc({...newIoc, type: e.target.value})} style={inputStyle}>
                            <option value="IP">IP</option>
                            <option value="Domain">Domain</option>
                            <option value="Hash">Hash</option>
                        </select>
                        <input required placeholder="IOC Value" value={newIoc.value} onChange={e => setNewIoc({...newIoc, value: e.target.value})} style={inputStyle} />
                        <input type="number" required placeholder="Risk Score (0-100)" value={newIoc.riskScore} onChange={e => setNewIoc({...newIoc, riskScore: parseInt(e.target.value)})} min="0" max="100" style={inputStyle} />
                        <input placeholder="Country Code (e.g., VN)" value={newIoc.country} onChange={e => setNewIoc({...newIoc, country: e.target.value})} maxLength="2" style={inputStyle} />
                        <button type="submit" style={btnStyle('#16a34a')}>Save IOC</button>
                    </div>
                </form>
            )}

            {showRelationForm && (
                <form onSubmit={handleAddRelation} style={formContainerStyle}>
                    <h3 style={{ color: '#34d399', marginTop: 0 }}>Create Relationship (Edge)</h3>
                    <div style={{ display: 'flex', gap: '15px', flexWrap: 'wrap' }}>
                        <input required placeholder="Source Node Value" value={newRelation.fromValue} onChange={e => setNewRelation({...newRelation, fromValue: e.target.value})} style={inputStyle} />
                        <select value={newRelation.relationType} onChange={e => setNewRelation({...newRelation, relationType: e.target.value})} style={inputStyle}>
                            <option value="related_to">Related To</option>
                            <option value="communicates_with">Communicates With</option>
                            <option value="resolves_to">Resolves To</option>
                            <option value="belongs_to">Belongs To</option>
                            <option value="drops">Drops</option>
                        </select>
                        <input required placeholder="Target Node Value" value={newRelation.toValue} onChange={e => setNewRelation({...newRelation, toValue: e.target.value})} style={inputStyle} />
                        <button type="submit" style={btnStyle('#16a34a')}>Create Link</button>
                    </div>
                </form>
            )}

            <div style={{ display: 'flex', gap: '15px', marginBottom: '20px', alignItems: 'center' }}>
                <span style={{ color: '#94a3b8', fontWeight: 'bold' }}>Filter:</span>
                <select value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)} style={{ padding: '8px', borderRadius: '6px', backgroundColor: '#1e293b', color: '#fff', border: '1px solid #475569' }}>
                    <option value="">All Types</option>
                    <option value="IP">IP</option>
                    <option value="Domain">Domain</option>
                    <option value="Hash">Hash</option>
                </select>
                <select value={limit} onChange={(e) => { setLimit(Number(e.target.value)); setPage(1); }} style={{ padding: '8px', borderRadius: '6px', backgroundColor: '#1e293b', color: '#fff', border: '1px solid #475569' }}>
                    <option value={10}>Show 10 records</option>
                    <option value={20}>Show 20 records</option>
                    <option value={50}>Show 50 records</option>
                </select>
            </div>

            {error && <div style={{ backgroundColor: '#7f1d1d', color: '#fecaca', padding: '10px', borderRadius: '8px', marginBottom: '15px' }}>{error}</div>}

            {loading ? (
                <div style={{ color: '#38bdf8', fontStyle: 'italic', padding: '20px 0' }}>Loading data...</div>
            ) : (
                <div style={{ overflowX: 'auto' }}>
                    <table style={{ width: '100%', color: '#e2e8f0', textAlign: 'left', borderCollapse: 'collapse', minWidth: '800px' }}>
                        <thead>
                            <tr style={{ borderBottom: '2px solid #334155', backgroundColor: '#1e293b' }}>
                                <th style={{ padding: '12px' }}>Value</th>
                                <th style={{ padding: '12px' }}>Type</th>
                                <th style={{ padding: '12px' }}>Risk Score</th>
                                <th style={{ padding: '12px' }}>Country</th>
                                <th style={{ padding: '12px' }}>Source</th>
                                <th style={{ padding: '12px' }}>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {iocs.map((ioc) => (
                                <tr key={ioc.id} style={{ borderBottom: '1px solid #1e293b', transition: '0.2s' }}>
                                    <td style={{ padding: '12px', fontWeight: 'bold', wordBreak: 'break-all' }}>{ioc.value}</td>
                                    <td style={{ padding: '12px' }}>
                                        <span style={{ backgroundColor: '#0f172a', padding: '4px 8px', borderRadius: '4px', border: '1px solid #475569', fontSize: '0.85rem' }}>{ioc.type}</span>
                                    </td>
                                    <td style={{ padding: '12px', color: ioc.riskScore >= 80 ? '#f87171' : '#4ade80', fontWeight: 'bold' }}>{ioc.riskScore}</td>
                                    <td style={{ padding: '12px' }}>{ioc.country || 'N/A'}</td>
                                    <td style={{ padding: '12px', color: '#94a3b8' }}>{ioc.originRef}</td>
                                    <td style={{ padding: '12px' }}>
                                        <button onClick={() => handleDelete(ioc.id)} style={{ color: '#ef4444', background: 'none', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}>Delete</button>
                                    </td>
                                </tr>
                            ))}
                            {iocs.length === 0 && (
                                <tr>
                                    <td colSpan="6" style={{ textAlign: 'center', padding: '30px', color: '#64748b' }}>No IOC data available</td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            )}

            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '20px', paddingTop: '15px', borderTop: '1px solid #1e293b', flexWrap: 'wrap', gap: '10px' }}>
                <span style={{ color: '#94a3b8', fontSize: '0.9rem' }}>Showing total: <strong>{totalCount}</strong> records</span>
                <div style={{ display: 'flex', gap: '10px', alignItems: 'center' }}>
                    <button disabled={page === 1} onClick={() => setPage(page - 1)} style={{ padding: '8px 16px', borderRadius: '6px', border: 'none', backgroundColor: page === 1 ? '#334155' : '#2563eb', color: '#fff', cursor: page === 1 ? 'not-allowed' : 'pointer' }}>Prev</button>
                    <span style={{ display: 'flex', alignItems: 'center', gap: '8px', padding: '4px 16px', backgroundColor: '#1e293b', borderRadius: '6px', color: '#e2e8f0', fontWeight: 'bold' }}>
                        Page
                        <input type="text" value={inputPage} onChange={handleInputPageChange} onKeyDown={handleInputPageSubmit} onBlur={handleInputPageSubmit} style={{ width: '45px', padding: '4px', textAlign: 'center', borderRadius: '6px', border: '1px solid #475569', backgroundColor: '#0f172a', color: '#fff', fontWeight: 'bold' }} />
                        / {totalPages}
                    </span>
                    <button disabled={page >= totalPages} onClick={() => setPage(page + 1)} style={{ padding: '8px 16px', borderRadius: '6px', border: 'none', backgroundColor: page >= totalPages ? '#334155' : '#2563eb', color: '#fff', cursor: page >= totalPages ? 'not-allowed' : 'pointer' }}>Next</button>
                </div>
            </div>
        </div>
    );
};

const inputStyle = { padding: '10px', borderRadius: '6px', backgroundColor: '#0f172a', color: '#fff', border: '1px solid #475569', minWidth: '150px', flex: 1 };
const btnStyle = (color) => ({ backgroundColor: color, color: '#fff', padding: '10px 20px', borderRadius: '8px', border: 'none', cursor: 'pointer', fontWeight: 'bold' });
const formContainerStyle = { backgroundColor: '#1e293b', padding: '20px', borderRadius: '8px', marginBottom: '20px', border: '1px solid #334155' };

export default IocManagement;