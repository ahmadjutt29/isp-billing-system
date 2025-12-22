
import { useState, type FormEvent } from 'react';
import axios from 'axios';
import { jwtDecode } from 'jwt-decode';
import { useNavigate } from 'react-router-dom';
import { FaEye, FaEyeSlash } from 'react-icons/fa';

interface LoginResponse {
  token: string;
  username: string;
  role: string;
  expiresAt: string;
}

interface JwtPayload {
  role?: string;
  nameid: string;
  unique_name: string;
  exp: number;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string;
}

const Login = () => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');

  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [rememberMe, setRememberMe] = useState(false);
  const [usernameTouched, setUsernameTouched] = useState(false);
  const [passwordTouched, setPasswordTouched] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);

    try {
      const response = await axios.post<LoginResponse>(
        '/api/auth/login',
        { username, password }
      );

      const { token } = response.data;

      // Save token to localStorage or sessionStorage
      if (rememberMe) {
        localStorage.setItem('token', token);
      } else {
        sessionStorage.setItem('token', token);
      }

      // Decode token to get role
      const decoded = jwtDecode<JwtPayload>(token);
      const role = decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

      // Redirect based on role
      if (role === 'Admin') {
        navigate('/admin');
      } else {
        navigate('/client');
      }
    } catch (err) {
      if (axios.isAxiosError(err)) {
        if (err.response?.status === 401) {
          setError('Invalid username or password');
        } else if (err.response?.data?.message) {
          setError(err.response.data.message);
        } else {
          setError('Login failed. Please try again.');
        }
      } else {
        setError('An unexpected error occurred');
      }
    } finally {
      setIsLoading(false);
    }
  };

  // Validation helpers
  const isUsernameValid = username.length >= 3 && !username.includes(' ');
  const isPasswordValid = password.length >= 6;
  const canSubmit = isUsernameValid && isPasswordValid && !isLoading;

  return (
    <div style={styles.container}>
      <div style={styles.card}>
        <div style={styles.logoRow}>
          <img src="/vite.svg" alt="ISP Logo" style={styles.logo} />
          <span style={styles.brand}>ISP Billing System</span>
        </div>
        <h2 style={styles.subtitle}>Sign in to your account</h2>

        <div style={styles.testCreds}>
          <b>Test Admin:</b> admin / admin123<br />
          <b>Test Client:</b> client1 / client123
        </div>

        {error && <div style={styles.error} role="alert">{error}</div>}

        <form onSubmit={handleSubmit} style={styles.form} autoComplete="on" aria-label="Login form">
          <div style={styles.inputGroup}>
            <label htmlFor="username" style={styles.label}>
              Username
            </label>
            <input
              id="username"
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              onBlur={() => setUsernameTouched(true)}
              placeholder="Enter your username"
              required
              minLength={3}
              autoFocus
              style={{
                ...styles.input,
                borderColor:
                  usernameTouched && !isUsernameValid ? '#dc2626' : '#d1d5db',
              }}
              aria-invalid={usernameTouched && !isUsernameValid}
              aria-describedby="usernameHelp"
            />
            {usernameTouched && !isUsernameValid && (
              <div style={styles.inputError} id="usernameHelp">
                Username must be at least 3 characters, no spaces.
              </div>
            )}
          </div>

          <div style={styles.inputGroup}>
            <label htmlFor="password" style={styles.label}>
              Password
            </label>
            <div style={styles.passwordRow}>
              <input
                id="password"
                type={showPassword ? 'text' : 'password'}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                onBlur={() => setPasswordTouched(true)}
                placeholder="Enter your password"
                required
                minLength={6}
                style={{
                  ...styles.input,
                  borderColor:
                    passwordTouched && !isPasswordValid ? '#dc2626' : '#d1d5db',
                  paddingRight: '2.5rem',
                }}
                aria-invalid={passwordTouched && !isPasswordValid}
                aria-describedby="passwordHelp"
              />
              <button
                type="button"
                aria-label={showPassword ? 'Hide password' : 'Show password'}
                onClick={() => setShowPassword((v) => !v)}
                style={styles.eyeButton}
                tabIndex={-1}
              >
                {showPassword ? <FaEyeSlash /> : <FaEye />}
              </button>
            </div>
            {passwordTouched && !isPasswordValid && (
              <div style={styles.inputError} id="passwordHelp">
                Password must be at least 6 characters.
              </div>
            )}
          </div>

          <div style={styles.optionsRow}>
            <label style={styles.checkboxLabel}>
              <input
                type="checkbox"
                checked={rememberMe}
                onChange={() => setRememberMe((v) => !v)}
                style={styles.checkbox}
              />
              Remember me
            </label>
            <a href="#" style={styles.forgot} tabIndex={-1} aria-disabled="true">
              Forgot password?
            </a>
          </div>

          <button
            type="submit"
            disabled={!canSubmit}
            style={{
              ...styles.button,
              ...(!canSubmit ? styles.buttonDisabled : {}),
            }}
            aria-busy={isLoading}
          >
            {isLoading ? (
              <span style={styles.spinner} aria-label="Loading" />
            ) : (
              'Sign In'
            )}
          </button>
        </form>
      </div>
    </div>
  );
};


const styles: { [key: string]: React.CSSProperties } = {
  container: {
    minHeight: '100vh',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    background: 'linear-gradient(120deg, #e0e7ff 0%, #f3f4f6 100%)',
    padding: '1rem',
  },
  card: {
    backgroundColor: '#fff',
    padding: '2.5rem 2rem 2rem 2rem',
    borderRadius: '12px',
    boxShadow: '0 8px 32px 0 rgba(31, 38, 135, 0.15)',
    width: '100%',
    maxWidth: '410px',
    position: 'relative',
  },
  logoRow: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: '0.5rem',
    marginBottom: '0.5rem',
  },
  logo: {
    width: '2.2rem',
    height: '2.2rem',
    marginRight: '0.2rem',
  },
  brand: {
    fontWeight: 700,
    fontSize: '1.3rem',
    color: '#2563eb',
    letterSpacing: '0.01em',
  },
  subtitle: {
    fontSize: '1.1rem',
    color: '#6b7280',
    textAlign: 'center',
    marginBottom: '1.2rem',
    fontWeight: 500,
  },
  testCreds: {
    background: '#f1f5f9',
    border: '1px solid #e0e7ef',
    color: '#334155',
    borderRadius: '6px',
    fontSize: '0.95rem',
    padding: '0.5rem 0.75rem',
    marginBottom: '1.1rem',
    textAlign: 'center',
  },
  error: {
    backgroundColor: '#fef2f2',
    border: '1px solid #fecaca',
    color: '#dc2626',
    padding: '0.75rem',
    borderRadius: '4px',
    marginBottom: '1rem',
    fontSize: '0.95rem',
    textAlign: 'center',
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: '1.1rem',
  },
  inputGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: '0.25rem',
  },
  label: {
    fontSize: '0.97rem',
    fontWeight: 500,
    color: '#374151',
    marginBottom: '0.1rem',
  },
  input: {
    padding: '0.75rem',
    border: '1.5px solid #d1d5db',
    borderRadius: '5px',
    fontSize: '1rem',
    outline: 'none',
    transition: 'border-color 0.2s',
    background: '#f8fafc',
  },
  inputError: {
    color: '#dc2626',
    fontSize: '0.85rem',
    marginTop: '0.1rem',
  },
  passwordRow: {
    display: 'flex',
    alignItems: 'center',
    position: 'relative',
  },
  eyeButton: {
    position: 'absolute',
    right: '0.5rem',
    top: '50%',
    transform: 'translateY(-50%)',
    background: 'none',
    border: 'none',
    cursor: 'pointer',
    color: '#64748b',
    fontSize: '1.1rem',
    padding: 0,
    outline: 'none',
  },
  optionsRow: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginTop: '-0.5rem',
    marginBottom: '-0.5rem',
  },
  checkboxLabel: {
    display: 'flex',
    alignItems: 'center',
    fontSize: '0.97rem',
    color: '#374151',
    gap: '0.4rem',
    userSelect: 'none',
  },
  checkbox: {
    accentColor: '#2563eb',
    width: '1rem',
    height: '1rem',
    marginRight: '0.2rem',
  },
  forgot: {
    color: '#2563eb',
    fontSize: '0.97rem',
    textDecoration: 'none',
    opacity: 0.7,
    cursor: 'not-allowed',
  },
  button: {
    backgroundColor: '#2563eb',
    color: '#fff',
    padding: '0.75rem',
    borderRadius: '5px',
    fontSize: '1.08rem',
    fontWeight: 600,
    border: 'none',
    cursor: 'pointer',
    marginTop: '0.5rem',
    transition: 'background-color 0.2s',
    boxShadow: '0 1px 2px rgba(37,99,235,0.04)',
    letterSpacing: '0.01em',
  },
  buttonDisabled: {
    backgroundColor: '#93c5fd',
    cursor: 'not-allowed',
    color: '#f1f5f9',
  },
  spinner: {
    display: 'inline-block',
    width: '1.2em',
    height: '1.2em',
    border: '2.5px solid #fff',
    borderTop: '2.5px solid #2563eb',
    borderRadius: '50%',
    animation: 'spin 0.7s linear infinite',
    verticalAlign: 'middle',
  },
};

export default Login;
