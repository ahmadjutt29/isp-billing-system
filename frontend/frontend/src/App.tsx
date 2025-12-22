import { useState } from 'react';
import { Routes, Route, Navigate, Outlet, useNavigate } from 'react-router-dom';
import { jwtDecode } from 'jwt-decode';
import Login from './components/Login';
import AdminDashboard from './components/AdminDashboard';
import ClientDashboard from './components/ClientDashboard';
import './App.css';

interface JwtPayload {
  role: string;
  exp: number;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string;
}

// Check if user is authenticated
const isAuthenticated = (): boolean => {
  const token = localStorage.getItem('token');
  if (!token) return false;

  try {
    const decoded = jwtDecode<JwtPayload>(token);
    if (decoded.exp * 1000 < Date.now()) {
      localStorage.removeItem('token');
      return false;
    }
    return true;
  } catch {
    localStorage.removeItem('token');
    return false;
  }
};

// Get user role from token
const getUserRole = (): string | null => {
  const token = localStorage.getItem('token');
  if (!token) return null;

  try {
    const decoded = jwtDecode<JwtPayload>(token);
    return decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || null;
  } catch {
    return null;
  }
};

// Navbar Component
const Navbar = () => {
  const navigate = useNavigate();
  const authenticated = isAuthenticated();
  const role = getUserRole();
  const [menuOpen, setMenuOpen] = useState(false);

  const handleLogout = () => {
    localStorage.removeItem('token');
    navigate('/login');
  };

  if (!authenticated) return null;

  return (
    <nav style={navStyles.navbar}>
      <div style={navStyles.navContainer}>
        <div style={navStyles.brand}>
          <span style={navStyles.brandIcon}>📡</span>
          <span style={navStyles.brandText}>ISP Billing</span>
        </div>
        
        {/* Desktop menu */}
        <div style={navStyles.desktopMenu}>
          {role === 'Admin' && (
            <button onClick={() => navigate('/admin')} style={navStyles.navLink}>
              Dashboard
            </button>
          )}
          {role === 'Admin' && (
            <button onClick={() => navigate('/client')} style={navStyles.navLink}>
              Client View
            </button>
          )}
          <span style={navStyles.role}>{role}</span>
          <button onClick={handleLogout} style={navStyles.logoutBtn}>
            Logout
          </button>
        </div>

        {/* Mobile menu button */}
        <button 
          style={navStyles.mobileMenuBtn}
          onClick={() => setMenuOpen(!menuOpen)}
        >
          ☰
        </button>
      </div>

      {/* Mobile dropdown */}
      {menuOpen && (
        <div style={navStyles.mobileMenu}>
          {role === 'Admin' && (
            <button onClick={() => { navigate('/admin'); setMenuOpen(false); }} style={navStyles.mobileNavLink}>
              Dashboard
            </button>
          )}
          {role === 'Admin' && (
            <button onClick={() => { navigate('/client'); setMenuOpen(false); }} style={navStyles.mobileNavLink}>
              Client View
            </button>
          )}
          <div style={navStyles.mobileRole}>Role: {role}</div>
          <button onClick={handleLogout} style={navStyles.mobileLogoutBtn}>
            Logout
          </button>
        </div>
      )}
    </nav>
  );
};

const navStyles: { [key: string]: React.CSSProperties } = {
  navbar: {
    backgroundColor: '#1f2937',
    color: '#ffffff',
    position: 'sticky',
    top: 0,
    zIndex: 1000,
    boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
  },
  navContainer: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '0.75rem 1.5rem',
    maxWidth: '1400px',
    margin: '0 auto',
  },
  brand: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.5rem',
  },
  brandIcon: {
    fontSize: '1.5rem',
  },
  brandText: {
    fontSize: '1.25rem',
    fontWeight: 'bold',
  },
  desktopMenu: {
    display: 'flex',
    alignItems: 'center',
    gap: '1rem',
  },
  navLink: {
    background: 'transparent',
    border: 'none',
    color: '#d1d5db',
    padding: '0.5rem 1rem',
    cursor: 'pointer',
    fontSize: '0.875rem',
    borderRadius: '4px',
    transition: 'color 0.2s',
  },
  role: {
    backgroundColor: '#3b82f6',
    padding: '0.375rem 0.75rem',
    borderRadius: '4px',
    fontSize: '0.75rem',
    fontWeight: '500',
  },
  logoutBtn: {
    backgroundColor: '#dc2626',
    color: '#ffffff',
    border: 'none',
    padding: '0.5rem 1rem',
    borderRadius: '4px',
    cursor: 'pointer',
    fontSize: '0.875rem',
    fontWeight: '500',
    transition: 'background-color 0.2s',
  },
  mobileMenuBtn: {
    display: 'none',
    background: 'transparent',
    border: 'none',
    color: '#ffffff',
    fontSize: '1.5rem',
    cursor: 'pointer',
    padding: '0.5rem',
  },
  mobileMenu: {
    display: 'none',
    flexDirection: 'column',
    padding: '1rem',
    borderTop: '1px solid #374151',
    gap: '0.5rem',
  },
  mobileNavLink: {
    background: 'transparent',
    border: 'none',
    color: '#d1d5db',
    padding: '0.75rem',
    textAlign: 'left',
    cursor: 'pointer',
    fontSize: '1rem',
    borderRadius: '4px',
  },
  mobileRole: {
    padding: '0.75rem',
    color: '#9ca3af',
    fontSize: '0.875rem',
  },
  mobileLogoutBtn: {
    backgroundColor: '#dc2626',
    color: '#ffffff',
    border: 'none',
    padding: '0.75rem',
    borderRadius: '4px',
    cursor: 'pointer',
    fontSize: '1rem',
    fontWeight: '500',
  },
};

// Protected Route Component
interface ProtectedRouteProps {
  allowedRoles?: string[];
}

const ProtectedRoute = ({ allowedRoles }: ProtectedRouteProps) => {
  if (!isAuthenticated()) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles) {
    const role = getUserRole();
    if (!role || !allowedRoles.includes(role)) {
      return <Navigate to="/login" replace />;
    }
  }

  return <Outlet />;
};

// Layout Component with Navbar
const Layout = () => {
  return (
    <>
      <Navbar />
      <Outlet />
    </>
  );
};

// Home Redirect based on role
const HomeRedirect = () => {
  if (!isAuthenticated()) {
    return <Navigate to="/login" replace />;
  }
  const role = getUserRole();
  if (role === 'Admin') {
    return <Navigate to="/admin" replace />;
  }
  return <Navigate to="/client" replace />;
};

function App() {
  return (
    <Routes>
      {/* Public Route */}
      <Route path="/login" element={<Login />} />

      {/* Protected Routes with Layout */}
      <Route element={<Layout />}>
        {/* Admin Routes */}
        <Route element={<ProtectedRoute allowedRoles={['Admin']} />}>
          <Route path="/admin" element={<AdminDashboard />} />
        </Route>

        {/* Client Routes */}
        <Route element={<ProtectedRoute allowedRoles={['Client', 'Admin']} />}>
          <Route path="/client" element={<ClientDashboard />} />
        </Route>
      </Route>

      {/* Default redirect */}
      <Route path="/" element={<HomeRedirect />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default App;

