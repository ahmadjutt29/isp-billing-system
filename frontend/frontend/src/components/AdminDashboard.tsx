import { useState, useEffect, type FormEvent } from 'react';
import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';

const API_BASE = '/api';

// API helper with JWT
const api = axios.create({
  baseURL: API_BASE,
});

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Types
interface User {
  id: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

interface Fee {
  id: number;
  userId: number;
  username: string;
  amount: number;
  dueDate: string;
  paid: boolean;
  paymentDate: string | null;
  description: string;
  createdAt: string;
}

interface IncomeSummary {
  totalPaidAmount: number;
  totalUnpaidAmount: number;
  totalAmount: number;
  paidFeesCount: number;
  unpaidFeesCount: number;
  totalFeesCount: number;
  overdueFeesCount: number;
  overdueAmount: number;
  collectionRate: number;
  generatedAt: string;
}

interface CreateUserForm {
  username: string;
  password: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
}

interface CreateFeeForm {
  userId: number;
  amount: number;
  dueDate: string;
  description: string;
}

const AdminDashboard = () => {
  const [activeTab, setActiveTab] = useState<'users' | 'fees' | 'reports'>('users');
  const [users, setUsers] = useState<User[]>([]);
  const [fees, setFees] = useState<Fee[]>([]);
  const [incomeSummary, setIncomeSummary] = useState<IncomeSummary | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  // User form state
  const [userForm, setUserForm] = useState<CreateUserForm>({
    username: '',
    password: '',
    email: '',
    firstName: '',
    lastName: '',
    phoneNumber: '',
  });

  // Fee form state
  const [feeForm, setFeeForm] = useState<CreateFeeForm>({
    userId: 0,
    amount: 0,
    dueDate: new Date().toISOString().split('T')[0],
    description: '',
  });

  // Fetch users
  const fetchUsers = async () => {
    try {
      setLoading(true);
      const response = await api.get<User[]>('/users');
      setUsers(response.data);
    } catch (err) {
      handleError(err, 'Failed to fetch users');
    } finally {
      setLoading(false);
    }
  };

  // Fetch fees
  const fetchFees = async () => {
    try {
      setLoading(true);
      const response = await api.get<Fee[]>('/fees');
      setFees(response.data);
    } catch (err) {
      handleError(err, 'Failed to fetch fees');
    } finally {
      setLoading(false);
    }
  };

  // Fetch income summary
  const fetchIncomeSummary = async () => {
    try {
      setLoading(true);
      const response = await api.get<IncomeSummary>('/reports/income');
      setIncomeSummary(response.data);
    } catch (err) {
      handleError(err, 'Failed to fetch income summary');
    } finally {
      setLoading(false);
    }
  };

  // Create user
  const handleCreateUser = async (e: FormEvent) => {
    e.preventDefault();
    try {
      setLoading(true);
      setError('');
      await api.post('/users', { ...userForm, role: 'Client' });
      setSuccess('User created successfully!');
      setUserForm({
        username: '',
        password: '',
        email: '',
        firstName: '',
        lastName: '',
        phoneNumber: '',
      });
      fetchUsers();
    } catch (err) {
      handleError(err, 'Failed to create user');
    } finally {
      setLoading(false);
    }
  };

  // Create fee
  const handleCreateFee = async (e: FormEvent) => {
    e.preventDefault();
    try {
      setLoading(true);
      setError('');
      await api.post('/fees', feeForm);
      setSuccess('Fee created successfully!');
      setFeeForm({
        userId: 0,
        amount: 0,
        dueDate: new Date().toISOString().split('T')[0],
        description: '',
      });
      fetchFees();
    } catch (err) {
      handleError(err, 'Failed to create fee');
    } finally {
      setLoading(false);
    }
  };

  // Mark fee as paid
  const handleMarkPaid = async (feeId: number) => {
    try {
      setLoading(true);
      await api.put(`/fees/${feeId}/pay`, {});
      setSuccess('Fee marked as paid!');
      fetchFees();
      if (activeTab === 'reports') fetchIncomeSummary();
    } catch (err) {
      handleError(err, 'Failed to mark fee as paid');
    } finally {
      setLoading(false);
    }
  };

  // Download invoice PDF
  const handleDownloadInvoice = async (feeId: number) => {
    try {
      const response = await api.get(`/reports/invoice/${feeId}`, {
        responseType: 'blob',
      });
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `Invoice_${feeId}.pdf`);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch (err) {
      handleError(err, 'Failed to download invoice');
    }
  };

  // Delete user
  const handleDeleteUser = async (userId: number) => {
    if (!confirm('Are you sure you want to delete this user?')) return;
    try {
      setLoading(true);
      await api.delete(`/users/${userId}`);
      setSuccess('User deleted successfully!');
      fetchUsers();
    } catch (err) {
      handleError(err, 'Failed to delete user');
    } finally {
      setLoading(false);
    }
  };

  // Delete fee
  const handleDeleteFee = async (feeId: number) => {
    if (!confirm('Are you sure you want to delete this fee?')) return;
    try {
      setLoading(true);
      await api.delete(`/fees/${feeId}`);
      setSuccess('Fee deleted successfully!');
      fetchFees();
    } catch (err) {
      handleError(err, 'Failed to delete fee');
    } finally {
      setLoading(false);
    }
  };

  // Error handler
  const handleError = (err: unknown, defaultMessage: string) => {
    if (err instanceof AxiosError && err.response?.data?.message) {
      setError(err.response.data.message);
    } else {
      setError(defaultMessage);
    }
  };

  // Clear messages after 3 seconds
  useEffect(() => {
    if (success || error) {
      const timer = setTimeout(() => {
        setSuccess('');
        setError('');
      }, 3000);
      return () => clearTimeout(timer);
    }
  }, [success, error]);

  // Initial data fetch
  useEffect(() => {
    fetchUsers();
  }, []);

  // Fetch data on tab change
  useEffect(() => {
    if (activeTab === 'users') fetchUsers();
    else if (activeTab === 'fees') {
      fetchUsers(); // Need users for dropdown
      fetchFees();
    } else if (activeTab === 'reports') {
      fetchIncomeSummary();
      fetchFees();
    }
  }, [activeTab]);

  return (
    <div style={styles.container}>
      <h1 style={styles.title}>Admin Dashboard</h1>

      {/* Messages */}
      {error && <div style={styles.error}>{error}</div>}
      {success && <div style={styles.success}>{success}</div>}

      {/* Tabs */}
      <div style={styles.tabs}>
        <button
          style={activeTab === 'users' ? styles.activeTab : styles.tab}
          onClick={() => setActiveTab('users')}
        >
          Users
        </button>
        <button
          style={activeTab === 'fees' ? styles.activeTab : styles.tab}
          onClick={() => setActiveTab('fees')}
        >
          Fees
        </button>
        <button
          style={activeTab === 'reports' ? styles.activeTab : styles.tab}
          onClick={() => setActiveTab('reports')}
        >
          Reports
        </button>
      </div>

      {loading && <div style={styles.loading}>Loading...</div>}

      {/* Users Tab */}
      {activeTab === 'users' && (
        <div style={styles.section}>
          {/* Create User Form */}
          <div style={styles.card}>
            <h2 style={styles.cardTitle}>Create New Client</h2>
            <form onSubmit={handleCreateUser} style={styles.form}>
              <div style={styles.formGrid}>
                <input
                  type="text"
                  placeholder="Username *"
                  value={userForm.username}
                  onChange={(e) => setUserForm({ ...userForm, username: e.target.value })}
                  required
                  style={styles.input}
                />
                <input
                  type="password"
                  placeholder="Password *"
                  value={userForm.password}
                  onChange={(e) => setUserForm({ ...userForm, password: e.target.value })}
                  required
                  style={styles.input}
                />
                <input
                  type="email"
                  placeholder="Email *"
                  value={userForm.email}
                  onChange={(e) => setUserForm({ ...userForm, email: e.target.value })}
                  required
                  style={styles.input}
                />
                <input
                  type="text"
                  placeholder="First Name"
                  value={userForm.firstName}
                  onChange={(e) => setUserForm({ ...userForm, firstName: e.target.value })}
                  style={styles.input}
                />
                <input
                  type="text"
                  placeholder="Last Name"
                  value={userForm.lastName}
                  onChange={(e) => setUserForm({ ...userForm, lastName: e.target.value })}
                  style={styles.input}
                />
                <input
                  type="text"
                  placeholder="Phone Number"
                  value={userForm.phoneNumber}
                  onChange={(e) => setUserForm({ ...userForm, phoneNumber: e.target.value })}
                  style={styles.input}
                />
              </div>
              <button type="submit" style={styles.button} disabled={loading}>
                Create Client
              </button>
            </form>
          </div>

          {/* Users Table */}
          <div style={styles.card}>
            <h2 style={styles.cardTitle}>All Users</h2>
            <div style={styles.tableContainer}>
              <table style={styles.table}>
                <thead>
                  <tr>
                    <th style={styles.th}>ID</th>
                    <th style={styles.th}>Username</th>
                    <th style={styles.th}>Email</th>
                    <th style={styles.th}>Name</th>
                    <th style={styles.th}>Role</th>
                    <th style={styles.th}>Status</th>
                    <th style={styles.th}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((user) => (
                    <tr key={user.id}>
                      <td style={styles.td}>{user.id}</td>
                      <td style={styles.td}>{user.username}</td>
                      <td style={styles.td}>{user.email}</td>
                      <td style={styles.td}>{`${user.firstName || ''} ${user.lastName || ''}`.trim() || '-'}</td>
                      <td style={styles.td}>
                        <span style={user.role === 'Admin' ? styles.badgeAdmin : styles.badgeClient}>
                          {user.role}
                        </span>
                      </td>
                      <td style={styles.td}>
                        <span style={user.isActive ? styles.badgeActive : styles.badgeInactive}>
                          {user.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td style={styles.td}>
                        {user.role !== 'Admin' && (
                          <button
                            onClick={() => handleDeleteUser(user.id)}
                            style={styles.deleteBtn}
                          >
                            Delete
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* Fees Tab */}
      {activeTab === 'fees' && (
        <div style={styles.section}>
          {/* Create Fee Form */}
          <div style={styles.card}>
            <h2 style={styles.cardTitle}>Create New Fee</h2>
            <form onSubmit={handleCreateFee} style={styles.form}>
              <div style={styles.formGrid}>
                <select
                  value={feeForm.userId}
                  onChange={(e) => setFeeForm({ ...feeForm, userId: parseInt(e.target.value) })}
                  required
                  style={styles.input}
                >
                  <option value={0}>Select User *</option>
                  {users.filter(u => u.role === 'Client').map((user) => (
                    <option key={user.id} value={user.id}>
                      {user.username} - {user.email}
                    </option>
                  ))}
                </select>
                <input
                  type="number"
                  placeholder="Amount *"
                  value={feeForm.amount || ''}
                  onChange={(e) => setFeeForm({ ...feeForm, amount: parseFloat(e.target.value) })}
                  required
                  min="0.01"
                  step="0.01"
                  style={styles.input}
                />
                <input
                  type="date"
                  value={feeForm.dueDate}
                  onChange={(e) => setFeeForm({ ...feeForm, dueDate: e.target.value })}
                  required
                  style={styles.input}
                />
                <input
                  type="text"
                  placeholder="Description"
                  value={feeForm.description}
                  onChange={(e) => setFeeForm({ ...feeForm, description: e.target.value })}
                  style={styles.input}
                />
              </div>
              <button type="submit" style={styles.button} disabled={loading || feeForm.userId === 0}>
                Create Fee
              </button>
            </form>
          </div>

          {/* Fees Table */}
          <div style={styles.card}>
            <h2 style={styles.cardTitle}>All Fees</h2>
            <div style={styles.tableContainer}>
              <table style={styles.table}>
                <thead>
                  <tr>
                    <th style={styles.th}>ID</th>
                    <th style={styles.th}>User</th>
                    <th style={styles.th}>Amount</th>
                    <th style={styles.th}>Due Date</th>
                    <th style={styles.th}>Status</th>
                    <th style={styles.th}>Description</th>
                    <th style={styles.th}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {fees.map((fee) => (
                    <tr key={fee.id}>
                      <td style={styles.td}>{fee.id}</td>
                      <td style={styles.td}>{fee.username}</td>
                      <td style={styles.td}>${fee.amount.toFixed(2)}</td>
                      <td style={styles.td}>{new Date(fee.dueDate).toLocaleDateString()}</td>
                      <td style={styles.td}>
                        <span style={fee.paid ? styles.badgePaid : styles.badgeUnpaid}>
                          {fee.paid ? 'Paid' : 'Unpaid'}
                        </span>
                      </td>
                      <td style={styles.td}>{fee.description || '-'}</td>
                      <td style={styles.td}>
                        <div style={styles.actionBtns}>
                          {!fee.paid && (
                            <button onClick={() => handleMarkPaid(fee.id)} style={styles.payBtn}>
                              Mark Paid
                            </button>
                          )}
                          <button onClick={() => handleDownloadInvoice(fee.id)} style={styles.downloadBtn}>
                            PDF
                          </button>
                          <button onClick={() => handleDeleteFee(fee.id)} style={styles.deleteBtn}>
                            Delete
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* Reports Tab */}
      {activeTab === 'reports' && (
        <div style={styles.section}>
          {/* Income Summary */}
          {incomeSummary && (
            <div style={styles.card}>
              <h2 style={styles.cardTitle}>Income Summary</h2>
              <div style={styles.statsGrid}>
                <div style={styles.statCard}>
                  <div style={styles.statValue}>${incomeSummary.totalPaidAmount.toFixed(2)}</div>
                  <div style={styles.statLabel}>Total Paid</div>
                </div>
                <div style={styles.statCard}>
                  <div style={styles.statValue}>${incomeSummary.totalUnpaidAmount.toFixed(2)}</div>
                  <div style={styles.statLabel}>Total Unpaid</div>
                </div>
                <div style={styles.statCard}>
                  <div style={styles.statValue}>${incomeSummary.totalAmount.toFixed(2)}</div>
                  <div style={styles.statLabel}>Total Revenue</div>
                </div>
                <div style={styles.statCard}>
                  <div style={styles.statValue}>{incomeSummary.collectionRate}%</div>
                  <div style={styles.statLabel}>Collection Rate</div>
                </div>
                <div style={styles.statCard}>
                  <div style={styles.statValue}>{incomeSummary.paidFeesCount}</div>
                  <div style={styles.statLabel}>Paid Fees</div>
                </div>
                <div style={styles.statCard}>
                  <div style={styles.statValue}>{incomeSummary.unpaidFeesCount}</div>
                  <div style={styles.statLabel}>Unpaid Fees</div>
                </div>
                <div style={styles.statCard}>
                  <div style={styles.statValue}>{incomeSummary.overdueFeesCount}</div>
                  <div style={styles.statLabel}>Overdue Fees</div>
                </div>
                <div style={styles.statCard}>
                  <div style={styles.statValue}>${incomeSummary.overdueAmount.toFixed(2)}</div>
                  <div style={styles.statLabel}>Overdue Amount</div>
                </div>
              </div>
            </div>
          )}

          {/* Invoices Download */}
          <div style={styles.card}>
            <h2 style={styles.cardTitle}>Download Invoices</h2>
            <div style={styles.tableContainer}>
              <table style={styles.table}>
                <thead>
                  <tr>
                    <th style={styles.th}>Fee ID</th>
                    <th style={styles.th}>User</th>
                    <th style={styles.th}>Amount</th>
                    <th style={styles.th}>Status</th>
                    <th style={styles.th}>Download</th>
                  </tr>
                </thead>
                <tbody>
                  {fees.map((fee) => (
                    <tr key={fee.id}>
                      <td style={styles.td}>{fee.id}</td>
                      <td style={styles.td}>{fee.username}</td>
                      <td style={styles.td}>${fee.amount.toFixed(2)}</td>
                      <td style={styles.td}>
                        <span style={fee.paid ? styles.badgePaid : styles.badgeUnpaid}>
                          {fee.paid ? 'Paid' : 'Unpaid'}
                        </span>
                      </td>
                      <td style={styles.td}>
                        <button onClick={() => handleDownloadInvoice(fee.id)} style={styles.downloadBtn}>
                          Download PDF
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

const styles: { [key: string]: React.CSSProperties } = {
  container: {
    padding: '2rem',
    maxWidth: '1400px',
    margin: '0 auto',
  },
  title: {
    fontSize: '1.75rem',
    fontWeight: 'bold',
    marginBottom: '1.5rem',
    color: '#1f2937',
  },
  error: {
    backgroundColor: '#fef2f2',
    border: '1px solid #fecaca',
    color: '#dc2626',
    padding: '0.75rem',
    borderRadius: '4px',
    marginBottom: '1rem',
  },
  success: {
    backgroundColor: '#f0fdf4',
    border: '1px solid #bbf7d0',
    color: '#16a34a',
    padding: '0.75rem',
    borderRadius: '4px',
    marginBottom: '1rem',
  },
  loading: {
    textAlign: 'center',
    padding: '1rem',
    color: '#6b7280',
  },
  tabs: {
    display: 'flex',
    gap: '0.5rem',
    marginBottom: '1.5rem',
    borderBottom: '2px solid #e5e7eb',
    paddingBottom: '0.5rem',
  },
  tab: {
    padding: '0.75rem 1.5rem',
    border: 'none',
    backgroundColor: 'transparent',
    color: '#6b7280',
    fontSize: '1rem',
    cursor: 'pointer',
    borderRadius: '4px 4px 0 0',
  },
  activeTab: {
    padding: '0.75rem 1.5rem',
    border: 'none',
    backgroundColor: '#2563eb',
    color: '#ffffff',
    fontSize: '1rem',
    cursor: 'pointer',
    borderRadius: '4px 4px 0 0',
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: '1.5rem',
  },
  card: {
    backgroundColor: '#ffffff',
    borderRadius: '8px',
    boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)',
    padding: '1.5rem',
  },
  cardTitle: {
    fontSize: '1.25rem',
    fontWeight: '600',
    marginBottom: '1rem',
    color: '#374151',
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: '1rem',
  },
  formGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
    gap: '0.75rem',
  },
  input: {
    padding: '0.75rem',
    border: '1px solid #d1d5db',
    borderRadius: '4px',
    fontSize: '0.875rem',
  },
  button: {
    backgroundColor: '#2563eb',
    color: '#ffffff',
    padding: '0.75rem 1.5rem',
    border: 'none',
    borderRadius: '4px',
    fontSize: '0.875rem',
    cursor: 'pointer',
    alignSelf: 'flex-start',
  },
  tableContainer: {
    overflowX: 'auto',
  },
  table: {
    width: '100%',
    borderCollapse: 'collapse',
    fontSize: '0.875rem',
  },
  th: {
    textAlign: 'left',
    padding: '0.75rem',
    backgroundColor: '#f9fafb',
    borderBottom: '2px solid #e5e7eb',
    fontWeight: '600',
    color: '#374151',
  },
  td: {
    padding: '0.75rem',
    borderBottom: '1px solid #e5e7eb',
    color: '#4b5563',
  },
  actionBtns: {
    display: 'flex',
    gap: '0.5rem',
    flexWrap: 'wrap',
  },
  payBtn: {
    backgroundColor: '#16a34a',
    color: '#ffffff',
    border: 'none',
    padding: '0.375rem 0.75rem',
    borderRadius: '4px',
    fontSize: '0.75rem',
    cursor: 'pointer',
  },
  downloadBtn: {
    backgroundColor: '#2563eb',
    color: '#ffffff',
    border: 'none',
    padding: '0.375rem 0.75rem',
    borderRadius: '4px',
    fontSize: '0.75rem',
    cursor: 'pointer',
  },
  deleteBtn: {
    backgroundColor: '#dc2626',
    color: '#ffffff',
    border: 'none',
    padding: '0.375rem 0.75rem',
    borderRadius: '4px',
    fontSize: '0.75rem',
    cursor: 'pointer',
  },
  badgeAdmin: {
    backgroundColor: '#7c3aed',
    color: '#ffffff',
    padding: '0.25rem 0.5rem',
    borderRadius: '4px',
    fontSize: '0.75rem',
  },
  badgeClient: {
    backgroundColor: '#0891b2',
    color: '#ffffff',
    padding: '0.25rem 0.5rem',
    borderRadius: '4px',
    fontSize: '0.75rem',
  },
  badgeActive: {
    backgroundColor: '#16a34a',
    color: '#ffffff',
    padding: '0.25rem 0.5rem',
    borderRadius: '4px',
    fontSize: '0.75rem',
  },
  badgeInactive: {
    backgroundColor: '#6b7280',
    color: '#ffffff',
    padding: '0.25rem 0.5rem',
    borderRadius: '4px',
    fontSize: '0.75rem',
  },
  badgePaid: {
    backgroundColor: '#16a34a',
    color: '#ffffff',
    padding: '0.25rem 0.5rem',
    borderRadius: '4px',
    fontSize: '0.75rem',
  },
  badgeUnpaid: {
    backgroundColor: '#dc2626',
    color: '#ffffff',
    padding: '0.25rem 0.5rem',
    borderRadius: '4px',
    fontSize: '0.75rem',
  },
  statsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))',
    gap: '1rem',
  },
  statCard: {
    backgroundColor: '#f9fafb',
    padding: '1rem',
    borderRadius: '8px',
    textAlign: 'center',
  },
  statValue: {
    fontSize: '1.5rem',
    fontWeight: 'bold',
    color: '#1f2937',
  },
  statLabel: {
    fontSize: '0.875rem',
    color: '#6b7280',
    marginTop: '0.25rem',
  },
};

export default AdminDashboard;
