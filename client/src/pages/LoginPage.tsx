import { useState, type SubmitEvent } from 'react'
import { Link, Navigate, useLocation } from 'react-router'
import { ApiError } from '../api'
import { useAuth } from '../auth/authContext'

// Where to go after login - same-app paths only
// "/x" -> ok
// "//evil.com" or "https://..." -> "/" (open redirect)
function safeFrom(state: unknown): string {
  const from = (state as { from?: unknown } | null)?.from
  if (typeof from !== 'string') return '/'
  return from.startsWith('/') && !from.startsWith('//') ? from : '/'
}

// Email from the register page, if we came from there
function registeredEmail(state: unknown): string {
  const value = (state as { registeredEmail?: unknown } | null)?.registeredEmail
  return typeof value === 'string' ? value : ''
}

// POST /api/auth/login (E2)
export function LoginPage() {
  const { user, loading, login } = useAuth()
  const location = useLocation()
  const [email, setEmail] = useState(() => registeredEmail(location.state))
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  if (loading) return <p className='loading'>Loading...</p>

  // Already logged in, or the login below just succeeded
  if (user) return <Navigate to={safeFrom(location.state)} replace />

  async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      // Success sets user -> the render above redirects
      await login(email, password)
    } catch (err) {
      // 401 = the server's one generic message, shown as is
      // The client must never tell "unknown email" apart from "wrong password"
      setError(err instanceof ApiError && err.status === 401
        ? err.message
        : 'Login failed - try again later')
      setBusy(false)
    }
  }

  return (
    <section className="panel auth">
      <div className="ph">
        <h1>Log <span>in</span></h1>
      </div>
      <div className="pb">
        {registeredEmail(location.state) && (
          <p className="strip"><strong>Done</strong>Account created - log in now.</p>
        )}
        <form className="form" onSubmit={handleSubmit}>
          <label className="field">
            <span className="label">Email</span>
            <input type="email" autoComplete="username" required
              value={email} onChange={(e) => setEmail(e.target.value)} />
          </label>
          <label className="field">
            <span className="label">Password</span>
            <input type="password" autoComplete="current-password" required
              value={password} onChange={(e) => setPassword(e.target.value)} />
          </label>
          {error && <p className="strip error" role="alert"><strong>Error</strong>{error}</p>}
          <div className="actions">
            <button type="submit" className="btn" disabled={busy}>{busy ? 'Logging in...' : 'Log in'}</button>
          </div>
        </form>
        <p>No account yet? <Link to="/register" className="more">Register</Link></p>
      </div>
    </section>
  )
}
