import React, { useState } from 'react';
import axiosClient from '../api/axiosClient';

const IocIngest = () => {
    const [isSyncing, setIsSyncing] = useState(false);
    const [syncLog, setSyncLog] = useState([]);

    const handleSyncAlienVault = async () => {
        setIsSyncing(true);
        const startTime = new Date().toLocaleTimeString();
        setSyncLog(prev => [{ time: startTime, msg: 'Connecting to AlienVault OTX server...', type: 'info' }, ...prev]);

        try {
            const res = await axiosClient.post('/IocIngest/sync/alienvault');
            const endTime = new Date().toLocaleTimeString();
            setSyncLog(prev => [{ time: endTime, msg: res.data.message || 'Sync started successfully.', type: 'success' }, ...prev]);
        } catch (err) {
            const errorTime = new Date().toLocaleTimeString();
            const errorMsg = err.response?.data?.message || err.message || "Unknown error during synchronization.";
            setSyncLog(prev => [{ time: errorTime, msg: `Error: ${errorMsg}`, type: 'error' }, ...prev]);
        } finally {
            setIsSyncing(false);
        }
    };

    return (
        <div style={{ backgroundColor: '#0f172a', padding: '25px', borderRadius: '12px', minHeight: '80vh' }}>
            <h2 style={{ color: '#fff', marginTop: 0, marginBottom: '20px' }}>📡 DATA FEEDS MANAGEMENT</h2>

            <div style={{ backgroundColor: '#1e293b', padding: '20px', borderRadius: '8px', border: '1px solid #334155', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div>
                    <h3 style={{ color: '#93c5fd', margin: '0 0 10px 0' }}>👽 AlienVault OTX</h3>
                    <p style={{ color: '#94a3b8', margin: 0, fontSize: '0.9rem' }}>
                        Automatically collect Indicators of Compromise (IP, Domain, Hash) from your subscribed latest Pulses.
                    </p>
                </div>
                
                <button 
                    onClick={handleSyncAlienVault}
                    disabled={isSyncing}
                    style={{ 
                        backgroundColor: isSyncing ? '#475569' : '#2563eb', 
                        color: '#fff', 
                        padding: '12px 24px', 
                        borderRadius: '8px', 
                        border: 'none', 
                        cursor: isSyncing ? 'not-allowed' : 'pointer', 
                        fontWeight: 'bold',
                        minWidth: '180px'
                    }}>
                    {isSyncing ? '⏳ Syncing...' : '🔄 Sync Now'}
                </button>
            </div>

            <div style={{ marginTop: '30px', backgroundColor: '#1e293b', padding: '20px', borderRadius: '8px', border: '1px solid #334155' }}>
                <h4 style={{ color: '#e2e8f0', marginTop: 0, borderBottom: '1px solid #334155', paddingBottom: '10px' }}>📋 Activity Logs</h4>
                
                {syncLog.length === 0 ? (
                    <p style={{ color: '#64748b', fontStyle: 'italic' }}>No synchronization activity recorded yet.</p>
                ) : (
                    <ul style={{ listStyleType: 'none', padding: 0, margin: 0 }}>
                        {syncLog.map((log, index) => (
                            <li key={index} style={{ padding: '10px 0', borderBottom: '1px dashed #334155', color: log.type === 'error' ? '#fca5a5' : log.type === 'success' ? '#86efac' : '#cbd5e1' }}>
                                <span style={{ color: '#64748b', marginRight: '15px', fontFamily: 'monospace' }}>[{log.time}]</span>
                                {log.msg}
                            </li>
                        ))}
                    </ul>
                )}
            </div>
        </div>
    );
};

export default IocIngest;