import { useState, type SubmitEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router'
import { ApiError } from '../api'
import { useAuth } from '../auth/authContext'

// Same numbers as PasswordPolicy.cs - the server stays the authority
const MinLength = 15
const MaxLength = 128

// Code points after NFKC, like EnumerateRunes().Count() on the server
// password.length would count UTF-16 units: one emoji = 2
function passwordLength(password: string): number {
  return [...password.normalize('NFKC')].length
}

type FieldErrors = { email?: string; password?: string }

// POST /api/auth/register (E1 endpoint, E2 page)
export function RegisterPage() {
  const { user, loading, register } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  if (loading) return <p className='loading'>Loading...</p>
  if (user) return <Navigate to="/" replace />

  const length = passwordLength(password)

  async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault()
    setError(null)
    setFieldErrors({})

    // Quick feedback only
    // A script can skip this and post anything - the server checks again
    if (length < MinLength || length > MaxLength) {
      setFieldErrors({ password: `Use ${MinLength} to ${MaxLength} characters.` })
      return
    }

    setBusy(true)
    try {
      // The raw password goes out - normalizing is the server's job
      await register(email, password)
      // 201 = no session yet -> login page with the email filled in
      navigate('/login', { replace: true, state: { registeredEmail: email } })
    } catch (err) {
      setBusy(false)
      if (err instanceof ApiError && err.status === 400 && err.problem?.errors) {
        // 400: field -> messages, from RegisterValidator
        setFieldErrors({
          email: err.problem.errors.email?.[0],
          password: err.problem.errors.password?.[0],
        })
      } else if (err instanceof ApiError && err.status === 409) {
        // 409: email taken - registration enumeration is an accepted risk (B2 rate limits it)
        setFieldErrors({ email: err.message })
      } else {
        setError('Registration failed - try again later')
      }
    }
  }

  return (
    <section className="panel auth">
      <div className="ph">
        <h1>Create an <span>account</span></h1>
      </div>
      <div className="pb">
        <form className="form" onSubmit={handleSubmit} noValidate>
          <label className="field">
            <span className="label">Email</span>
            <input type="email" autoComplete="username" required
              value={email} onChange={(e) => setEmail(e.target.value)}
              aria-describedby={fieldErrors.email ? 'email-error' : undefined}
              aria-invalid={fieldErrors.email ? true : undefined} />
            {fieldErrors.email && <span id="email-error" className="field-error" role="alert">{fieldErrors.email}</span>}
          </label>

          <label className="field">
            <span className="label">Password</span>
            {/* new-password: password managers offer to generate one */}
            <input type="password" autoComplete="new-password" required
              value={password} onChange={(e) => setPassword(e.target.value)}
              aria-describedby={fieldErrors.password ? 'password-hint password-error' : 'password-hint'}
              aria-invalid={fieldErrors.password ? true : undefined} />
            {/* Live counter in code points, the same way the server counts */}
            <span id="password-hint" className="hint">
              {length} / {MinLength}-{MaxLength} characters - a passphrase of a few words works well
            </span>
            {fieldErrors.password && <span id="password-error" className="field-error" role="alert">{fieldErrors.password}</span>}
          </label>

          {error && <p className="strip error" role="alert"><strong>Error</strong>{error}</p>}
          <div className="actions">
            <button type="submit" className="btn" disabled={busy}>{busy ? 'Creating...' : 'Create account'}</button>
          </div>
        </form>
        <p>Already registered? <Link to="/login" className="more">Log in</Link></p>
      </div>
    </section>
  )
}
