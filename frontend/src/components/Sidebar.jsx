import React from 'react';
import { jwtDecode } from "jwt-decode";
import { Link } from 'react-router-dom';

const Sidebar = () => {
    const getRoleSecurely = () => {
        const token = localStorage.getItem('token');
        if (!token) return null;
        try {
            const decoded = jwtDecode(token);
            let userRole = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] 
                        || decoded.role 
                        || decoded.Role;
            if (Array.isArray(userRole)) {
                userRole = userRole[0];
            }
            return userRole;
        } catch (error) {
            return null;
        }
    };
    
    const role = getRoleSecurely();
    const linkStyle = { color: '#94a3b8', textDecoration: 'none', padding: '15px 20px', display: 'block', borderBottom: '1px solid #1e293b' };
    
    return (
        <div style={{ width: '250px', backgroundColor: '#0f172a', borderRight: '1px solid #1e293b' }}>
            <h2 style={{ color: '#fff', textAlign: 'center', padding: '20px 0', borderBottom: '1px solid #1e293b' }}>🛡️ IOC SYSTEM</h2>
            <nav>
                <Link to="/" style={linkStyle}>📊 Dashboard</Link>
                <Link to="/search" style={linkStyle}>🔍 IOC Lookup</Link>
                <Link to="/ioc-graph" style={linkStyle}>🕸️ Threat Graph</Link>
                <Link to="/database" style={linkStyle}>🗃️ IOC Database</Link>
                
                {role === 'Admin' && (
                <>
                <Link to="/ioc-ingest" style={linkStyle}>⚙️ Data Feeds</Link>
                <Link to="/user" style={linkStyle}>👥 User Management</Link>
                <Link to="/logs" style={linkStyle}>📜 System Logs</Link>
                </>
                )}
            </nav>
        </div>
    );
};

export default Sidebar;