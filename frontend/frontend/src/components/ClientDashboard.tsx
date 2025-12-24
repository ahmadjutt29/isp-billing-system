import { useState, useEffect } from 'react';
import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';

const API_BASE = '/api';

// API helper with JWT
const api = axios.create({
  baseURL: API_BASE,
});

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = localStorage.getItem('token') || sessionStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Types
interface Fee {
  id: number;
  userId: number;
  username: string | null;
  amount: number;
  dueDate: string;
  paid: boolean;
  paymentDate: string | null;
  description: string;
  createdAt: string;
}

const ClientDashboard = () => {
  const [fees, setFees] = useState<Fee[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  // PayNow modal state
  const [showPayNow, setShowPayNow] = useState(false);
  const [payNowFeeId, setPayNowFeeId] = useState<number | null>(null);
  const [transactionId, setTransactionId] = useState('');
  const [payeeName, setPayeeName] = useState('');
  const [payAmount, setPayAmount] = useState('');
  const [payNowError, setPayNowError] = useState('');

  // Calculate summary
  const totalPaid = fees.filter((f) => f.paid).reduce((sum, f) => sum + f.amount, 0);
  const totalUnpaid = fees.filter((f) => !f.paid).reduce((sum, f) => sum + f.amount, 0);
  const overdueCount = fees.filter((f) => !f.paid && new Date(f.dueDate) < new Date()).length;

  // Fetch client's fees
  const fetchMyFees = async () => {
    try {
      setLoading(true);
      const response = await api.get<Fee[]>('/fees/my-fees');
      setFees(response.data);
    } catch (err) {
      handleError(err, 'Failed to fetch your fees');
    } finally {
      setLoading(false);
    }
  };

  // Open PayNow modal
  const handlePayNowClick = (feeId: number) => {
    setPayNowFeeId(feeId);
    setShowPayNow(true);
    setTransactionId('');
    setPayeeName('');
    setPayAmount('');
    setPayNowError('');
  };

  // Client-side verify and submit
  const handlePayNowSubmit = async () => {
    if (!transactionId.trim() || !payeeName.trim() || !payAmount.trim()) {
      setPayNowError('All fields are required.');
      return;
    }
    if (isNaN(Number(payAmount)) || Number(payAmount) <= 0) {
      setPayNowError('Amount must be a positive number.');
      return;
    }
    try {
      setLoading(true);
      setPayNowError('');
      // Send for approval (simulate by posting to /fees/{id}/pay-request)
      await api.post(`/fees/${payNowFeeId}/pay-request`, {
        transactionId,
        payeeName,
        amount: Number(payAmount),
      });
      setSuccess('Payment request submitted for approval!');
      setShowPayNow(false);
      fetchMyFees();
    } catch (err) {
      setPayNowError('Failed to submit payment request.');
    } finally {
      setLoading(false);
    }
  };

  // Download invoice PDF
  const handleDownloadInvoice = async (feeId: number) => {
    try {
      const response = await api.get(`/fees/${feeId}/invoice`, {
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

  // Fetch data on mount
  useEffect(() => {
    fetchMyFees();
  }, []);

  // Check if fee is overdue
  const isOverdue = (fee: Fee) => !fee.paid && new Date(fee.dueDate) < new Date();

  return (
    <div style={styles.container}>
      <h1 style={styles.title}>My Dashboard</h1>

      {/* Messages */}
      {error && <div style={styles.error}>{error}</div>}
      {success && <div style={styles.success}>{success}</div>}

      {/* Summary Cards */}
      <div style={styles.summaryGrid}>
        <div style={styles.summaryCard}>
          <div style={styles.summaryValue}>${totalPaid.toFixed(2)}</div>
          <div style={styles.summaryLabel}>Total Paid</div>
        </div>
        <div style={{ ...styles.summaryCard, borderColor: '#dc2626' }}>
          <div style={{ ...styles.summaryValue, color: '#dc2626' }}>${totalUnpaid.toFixed(2)}</div>
          <div style={styles.summaryLabel}>Total Unpaid</div>
        </div>
        <div style={styles.summaryCard}>
          <div style={styles.summaryValue}>{fees.length}</div>
          <div style={styles.summaryLabel}>Total Fees</div>
        </div>
        <div style={{ ...styles.summaryCard, borderColor: overdueCount > 0 ? '#f59e0b' : '#e5e7eb' }}>
          <div style={{ ...styles.summaryValue, color: overdueCount > 0 ? '#f59e0b' : '#1f2937' }}>
            {overdueCount}
          </div>
          <div style={styles.summaryLabel}>Overdue</div>
        </div>
      </div>

      {loading && <div style={styles.loading}>Loading...</div>}

      {/* Fees Table */}
      <div style={styles.card}>
        <h2 style={styles.cardTitle}>My Fees</h2>
        {fees.length === 0 && !loading ? (
          <p style={styles.emptyMessage}>You have no fees yet.</p>
        ) : (
          <div style={styles.tableContainer}>
            <table style={styles.table}>
              <thead>
                <tr>
                  <th style={styles.th}>Invoice #</th>
                  <th style={styles.th}>Description</th>
                  <th style={styles.th}>Amount</th>
                  <th style={styles.th}>Due Date</th>
                  <th style={styles.th}>Status</th>
                  <th style={styles.th}>Payment Date</th>
                  <th style={styles.th}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {fees.map((fee) => (
                  <tr key={fee.id} style={isOverdue(fee) ? styles.overdueRow : undefined}>
                    <td style={styles.td}>#{fee.id.toString().padStart(6, '0')}</td>
                    <td style={styles.td}>{fee.description || 'Monthly Service Fee'}</td>
                    <td style={styles.td}>
                      <strong>${fee.amount.toFixed(2)}</strong>
                    </td>
                    <td style={styles.td}>
                      {new Date(fee.dueDate).toLocaleDateString()}
                      {isOverdue(fee) && <span style={styles.overdueTag}> OVERDUE</span>}
                    </td>
                    <td style={styles.td}>
                      <span style={fee.paid ? styles.badgePaid : styles.badgeUnpaid}>
                        {fee.paid ? 'Paid' : 'Unpaid'}
                      </span>
                    </td>
                    <td style={styles.td}>
                      {fee.paymentDate ? new Date(fee.paymentDate).toLocaleDateString() : '-'}
                    </td>
                    <td style={styles.td}>
                      <div style={styles.actionBtns}>
                        {!fee.paid && (
                          <button
                            onClick={() => handlePayNowClick(fee.id)}
                            style={styles.payBtn}
                            disabled={loading}
                          >
                            Pay Now
                          </button>
                        )}
                        <button
                          onClick={() => handleDownloadInvoice(fee.id)}
                          style={styles.downloadBtn}
                        >
                          Invoice
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Payment Info */}
      <div style={styles.infoCard}>
        <h3 style={styles.infoTitle}>Payment Information</h3>
        <p style={styles.infoText}>
          Click "Pay Now" to mark a fee as paid. You can download invoices for your records at any time.
        </p>
        <p style={styles.infoText}>
          If you have any questions about your bills, please contact support.
        </p>
      </div>
    {/* PayNow Modal */}
    {showPayNow && (
      <div style={styles.modalOverlay}>
        <div style={styles.modal}>
          <h3 style={styles.modalTitle}>Submit Payment Details</h3>
          <div style={styles.modalField}><label>Transaction ID:</label><input value={transactionId} onChange={e => setTransactionId(e.target.value)} style={styles.modalInput} /></div>
          <div style={styles.modalField}><label>Payee Name:</label><input value={payeeName} onChange={e => setPayeeName(e.target.value)} style={styles.modalInput} /></div>
          <div style={styles.modalField}><label>Amount:</label><input value={payAmount} onChange={e => setPayAmount(e.target.value)} style={styles.modalInput} type="number" min="0" /></div>
          {payNowError && <div style={styles.error}>{payNowError}</div>}
          <div style={{display:'flex',gap:'1rem',marginTop:'1rem'}}>
            <button style={styles.payBtn} onClick={handlePayNowSubmit} disabled={loading}>Submit</button>
            <button style={styles.downloadBtn} onClick={()=>setShowPayNow(false)} disabled={loading}>Cancel</button>
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
    maxWidth: '1200px',
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
  summaryGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))',
    gap: '1rem',
    marginBottom: '1.5rem',
  },
  summaryCard: {
    backgroundColor: '#ffffff',
    padding: '1.25rem',
    borderRadius: '8px',
    boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)',
    textAlign: 'center',
    borderLeft: '4px solid #16a34a',
  },
  summaryValue: {
    fontSize: '1.5rem',
    fontWeight: 'bold',
    color: '#1f2937',
  },
  summaryLabel: {
    fontSize: '0.875rem',
    color: '#6b7280',
    marginTop: '0.25rem',
  },
  card: {
    backgroundColor: '#ffffff',
    borderRadius: '8px',
    boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)',
    padding: '1.5rem',
    marginBottom: '1.5rem',
  },
  cardTitle: {
    fontSize: '1.25rem',
    fontWeight: '600',
    marginBottom: '1rem',
    color: '#374151',
  },
  emptyMessage: {
    textAlign: 'center',
    color: '#6b7280',
    padding: '2rem',
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
  overdueRow: {
    backgroundColor: '#fef2f2',
  },
  overdueTag: {
    color: '#dc2626',
    fontSize: '0.75rem',
    fontWeight: 'bold',
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
    padding: '0.5rem 1rem',
    borderRadius: '4px',
    fontSize: '0.75rem',
    cursor: 'pointer',
    fontWeight: '500',
  },
  downloadBtn: {
    backgroundColor: '#2563eb',
    color: '#ffffff',
    border: 'none',
    padding: '0.5rem 1rem',
    borderRadius: '4px',
    fontSize: '0.75rem',
    cursor: 'pointer',
  },
  badgePaid: {
    backgroundColor: '#16a34a',
    color: '#ffffff',
    padding: '0.25rem 0.75rem',
    borderRadius: '4px',
    fontSize: '0.75rem',
    fontWeight: '500',
  },
  badgeUnpaid: {
    backgroundColor: '#dc2626',
    color: '#ffffff',
    padding: '0.25rem 0.75rem',
    borderRadius: '4px',
    fontSize: '0.75rem',
    fontWeight: '500',
  },
  infoCard: {
    backgroundColor: '#f0f9ff',
    border: '1px solid #bae6fd',
    borderRadius: '8px',
    padding: '1.25rem',
  },
  infoTitle: {
    fontSize: '1rem',
    fontWeight: '600',
    color: '#0369a1',
    marginBottom: '0.5rem',
  },
  infoText: {
    fontSize: '0.875rem',
    color: '#0c4a6e',
    margin: '0.25rem 0',
  },
  modalOverlay: {
    position: 'fixed',
    top: 0,
    left: 0,
    width: '100vw',
    height: '100vh',
    background: 'rgba(0,0,0,0.3)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    zIndex: 1000,
  },
  modal: {
    background: '#fff',
    borderRadius: '8px',
    padding: '2rem',
    minWidth: '320px',
    boxShadow: '0 2px 16px rgba(0,0,0,0.2)',
    maxWidth: '90vw',
  },
  modalTitle: {
    fontSize: '1.1rem',
    fontWeight: 'bold',
    marginBottom: '1rem',
    color: '#0369a1',
  },
  modalField: {
    marginBottom: '1rem',
    display: 'flex',
    flexDirection: 'column',
    gap: '0.25rem',
  },
  modalInput: {
    padding: '0.5rem',
    borderRadius: '4px',
    border: '1px solid #bae6fd',
    fontSize: '1rem',
  },
};

export default ClientDashboard;
