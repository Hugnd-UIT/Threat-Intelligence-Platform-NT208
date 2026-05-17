import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { jwtDecode } from 'jwt-decode'; 
import { useEffect } from 'react';

import MainLayout from './layouts/MainLayout';
import Login from './pages/Auth';
import Dashboard from './pages/Dashboard';
import Search from './pages/Search'; 
import IocNodes from './pages/IocNodes'; 
import IocGraph from './pages/IocGraph';
import IocIngest from './pages/IocIngest'; 
import Logs from './pages/Logs'; 
import User from './pages/User'; 

function App() {
  const isTokenValid = () => {
    const token = localStorage.getItem('token');
    if (!token) return false;
    try {
      const decoded = jwtDecode(token);
      if (decoded.exp * 1000 < Date.now()) { 
        localStorage.removeItem('token');
        return false;
      }
      return true;
    } catch {
      return false;
    }
  };

  const isAuthenticated = isTokenValid();
  
  useEffect(() => {
    const syncLogout = (event) => {
      if (event.key === 'token' && event.newValue === null) {
        window.location.href = '/login'; 
      }
    };
    window.addEventListener('storage', syncLogout);
    return () => window.removeEventListener('storage', syncLogout);
  }, []);

  const getRoleFromToken = () => {
    const token = localStorage.getItem('token');
    if (!token) return null;
    try {
      const decoded = jwtDecode(token);
      return decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decoded.role;
    } catch (error) {
      return null;
    }
  };

  const role = getRoleFromToken();

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        
        <Route path="/" element={isAuthenticated ? <MainLayout /> : <Navigate to="/login" />}>
          <Route index element={<Dashboard />} />
          <Route path="search" element={<Search />}/>
          <Route path="database" element={<IocNodes />} />
          <Route path="ioc-graph" element={<IocGraph />} />
          <Route path="ioc-ingest" element={role === 'Admin' ? <IocIngest /> : <Navigate to='/'/>} />
          <Route path="user" element={role === 'Admin' ? <User /> : <Navigate to='/'/>} />
          <Route path="logs" element={role === 'Admin' ? <Logs /> : <Navigate to='/'/>} />
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;