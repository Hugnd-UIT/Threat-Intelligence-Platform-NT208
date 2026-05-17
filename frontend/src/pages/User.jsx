import React, { useEffect, useState } from 'react';
import { useOutletContext } from 'react-router-dom';
import axiosClient from '../api/axiosClient';
import { jwtDecode } from 'jwt-decode';

const User = () => {
    const [users, setUsers] = useState([]);
    const context = useOutletContext();
    const searchText = context ? context[0] : '';
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    
    const [showForm, setShowForm] = useState(false);
    const [isEditing, setIsEditing] = useState(false);
    const [editingKey, setEditingKey] = useState(null);
    const [formData, setFormData] = useState({ username: '', password: '', role: 'User', isLocked: false });

    const getMyUsername = () => {
        const token = localStorage.getItem('token');
        if (!token) return '';
        try {
            const decoded = jwtDecode(token);
            return decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || decoded.unique_name || decoded.name || '';
        } catch (e) {
            return '';
        }
    };
    
    const myUsername = getMyUsername();

    const fetchUsers = async () => {
        setLoading(true);
        try {
            const res = await axiosClient.get('/User');
            const usersData = Array.isArray(res.data) ? res.data : (res.data?.data || res.data?.Result || []);
            setUsers(usersData);
        } catch (err) {
            setError(err.response?.data?.message || err.message || "Server connection error.");
        } finally {
            setLoading(false);
        }
    };

    const handleDelete = async (key) => {
        if (!key) return;
        try {
            await axiosClient.delete(`/User/${key}`);
            fetchUsers();
        } catch (err) {
            alert("Error deleting: " + (err.response?.data?.message || err.message));
        }
    };

    const handleEditClick = (user) => {
        setIsEditing(true);
        setEditingKey(user._key || user.Key);
        setFormData({
            username: user.username || user.Username || '',
            password: '', 
            role: user.role || user.Role || 'User',
            isLocked: user.isLocked === true || user.IsLocked === true
        });
        setShowForm(true);
    };

    const handleOpenAddForm = () => {
        setIsEditing(false);
        setEditingKey(null);
        setFormData({ username: '', password: '', role: 'User', isLocked: false });
        setShowForm(!showForm);
    };

    const handleSaveUser = async (e) => {
        e.preventDefault();
        try {
            if (isEditing) {
                await axiosClient.put(`/User/${editingKey}`, formData);
            } else {
                await axiosClient.post('/User', formData);
            }
            setShowForm(false);
            setFormData({ username: '', password: '', role: 'User', isLocked: false });
            fetchUsers();
        } catch (err) {
            alert("Error saving: " + (err.response?.data?.message || err.message));
        }
    };

    useEffect(() => { fetchUsers(); }, []);

    const getRoleStyle = (role) => {
        const r = String(role || "").toLowerCase();
        if (r === 'admin') return { bg: '#7f1d1d', text: '#fca5a5' };
        if (r === 'user') return { bg: '#1e3a8a', text: '#93c5fd' };
        return { bg: '#334155', text: '#cbd5e1' };
    };

    const filteredUsers = Array.isArray(users) ? users.filter(u => {
        const name = String(u?.username || u?.Username || "");
        const search = String(searchText || "");
        return name.toLowerCase().includes(search.toLowerCase());
    }) : [];

    return (
        <div style={{ backgroundColor: '#0f172a', padding: '25px', borderRadius: '12px', animation: 'fadeIn 0.5s' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
                <h2 style={{ color: '#fff', margin: 0 }}>👥 USER MANAGEMENT</h2>
                <button 
                    onClick={handleOpenAddForm}
                    style={{ backgroundColor: showForm && !isEditing ? '#475569' : '#2563eb', color: '#fff', padding: '10px 20px', borderRadius: '8px', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}>
                    {showForm && !isEditing ? 'Close Form' : '+ Add New User'}
                </button>
            </div>
            
            {showForm && (
                <form onSubmit={handleSaveUser} style={{ backgroundColor: '#1e293b', padding: '20px', borderRadius: '8px', marginBottom: '20px', border: '1px solid #334155' }}>
                    <h3 style={{ color: '#93c5fd', marginTop: 0, marginBottom: '15px' }}>
                        {isEditing ? `✏️ Editing user: ${formData.username}` : '✨ Add new user'}
                    </h3>
                    <div style={{ display: 'flex', gap: '15px', flexWrap: 'wrap', alignItems: 'center' }}>
                        <input 
                            required 
                            placeholder="Username" 
                            value={formData.username} 
                            onChange={e => setFormData({...formData, username: e.target.value})}
                            disabled={isEditing} 
                            style={{ flex: 1, padding: '10px', borderRadius: '6px', backgroundColor: isEditing ? '#334155' : '#0f172a', color: isEditing ? '#94a3b8' : '#fff', border: '1px solid #475569', cursor: isEditing ? 'not-allowed' : 'text' }} 
                        />
                        <input 
                            type="password" placeholder={isEditing ? "(Leave blank to keep current password)" : "Password (required)"} 
                            required={!isEditing} 
                            value={formData.password} onChange={e => setFormData({...formData, password: e.target.value})}
                            style={{ flex: 1, padding: '10px', borderRadius: '6px', backgroundColor: '#0f172a', color: '#fff', border: '1px solid #475569' }} 
                        />
                        {isEditing && (
                            <label style={{ color: '#cbd5e1', display: 'flex', alignItems: 'center', gap: '5px', cursor: 'pointer' }}>
                                <input type="checkbox" checked={formData.isLocked} onChange={e => setFormData({...formData, isLocked: e.target.checked})} />
                                Lock account
                            </label>
                        )}
                        <select 
                            value={formData.role} 
                            onChange={e => setFormData({...formData, role: e.target.value})}
                            style={{ padding: '10px', borderRadius: '6px', backgroundColor: '#0f172a', color: '#fff', border: '1px solid #475569' }}>
                            <option value="User">User</option>
                            <option value="Admin">Admin</option>
                        </select>
                        <button type="submit" style={{ backgroundColor: '#16a34a', color: '#fff', padding: '10px 20px', borderRadius: '6px', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}>
                            {isEditing ? 'Save Changes' : 'Save User'}
                        </button>
                    </div>
                </form>
            )}

            {error && <div style={{ backgroundColor: '#7f1d1d', color: '#fecaca', padding: '10px', borderRadius: '8px', marginBottom: '15px' }}>⚠️ Error: {error}</div>}
            
            {loading ? (
                <div style={{ color: '#38bdf8', fontStyle: 'italic' }}>Loading data...</div>
            ) : (
                <table style={{ width: '100%', color: '#e2e8f0', textAlign: 'left', borderCollapse: 'collapse' }}>
                    <thead>
                        <tr style={{ borderBottom: '2px solid #334155' }}>
                            <th style={{ padding: '12px' }}>Username</th>
                            <th>Role</th>
                            <th>Status</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {filteredUsers.map((u, index) => {
                            const username = u?.username || u?.Username || "[No data]";
                            const role = u?.role || u?.Role || "Unknown";
                            const isLocked = u?.isLocked === true || u?.IsLocked === true;
                            const userKey = u?._key || u?.Key;
                            const roleStyle = getRoleStyle(role);

                            return (
                                <tr key={userKey || index} style={{ borderBottom: '1px solid #1e293b' }}>
                                    <td style={{ padding: '15px 12px', fontWeight: 'bold' }}>{username}</td>
                                    <td><span style={{ backgroundColor: roleStyle.bg, color: roleStyle.text, padding: '4px 8px', borderRadius: '4px', fontSize: '0.8rem', fontWeight: 'bold' }}>{role}</span></td>
                                    <td>{isLocked ? '🔴 Locked' : '🟢 Active'}</td>
                                    <td>
                                        <button onClick={() => handleEditClick(u)} style={{ color: '#eab308', background: 'none', border: 'none', cursor: 'pointer', fontWeight: 'bold', marginRight: '15px' }}>Edit</button>
                                        {username !== myUsername ? (
                                            <button onClick={() => handleDelete(userKey)} style={{ color: '#ef4444', background: 'none', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}>Delete</button>
                                        ) : (
                                            <span style={{ color: '#64748b', fontSize: '0.8rem', fontStyle: 'italic' }}>(Your account)</span>
                                        )}
                                    </td>
                                </tr>
                            );
                        })}
                    </tbody>
                </table>
            )}
        </div>
    );
};

export default User;