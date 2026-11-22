import { useState } from 'react'
import { Link, NavLink, Outlet } from 'react-router'
import { useAuth } from '../auth/authContext'

// Page frame for every route: toptag -> banner -> nav -> body -> footer
// Logout lives here, so every logged-in page has it
export function Layout() {
  const { user, loading, logout } = useAuth()
  const [logoutError, setLogoutError] = useState<string | null>(null)

  async function handleLogout() {
    setLogoutError(null)
    try {
      // Success: user = null -> RequireAuth sends us to /login
      await logout()
    } catch {
      setLogoutError('Logout failed - you are still logged in')
    }
  }

  return (
    <div className="shell">
      <div className="toptag">
        <span className="tag">OBSCURA</span>
        <span>Educational demo - simulated XMR-t, no real money</span>
      </div>

      <header className="banner">
        <Link to="/" className="logo">
          <span className="ring" aria-hidden="true" />
          <span><b>OBSCURA</b><small>BANK</small></span>
        </Link>
        {user && (
          <div className="session">
            <span>Logged in as <strong>{user.email}</strong></span>
            <button type="button" className="btn dark" onClick={handleLogout}>Log out</button>
          </div>
        )}
      </header>

      <nav className="nav" aria-label="Main">
        {/* Empty until GET /api/me answered - no flash of the wrong links */}
        {!loading && (user
          ? <NavLink to="/" end>Home</NavLink>
          : <>
              <NavLink to="/login">Log in</NavLink>
              <NavLink to="/register">Register</NavLink>
            </>)}
      </nav>

      <main className="body">
        {logoutError && <p className="strip error" role="alert"><strong>Error</strong>{logoutError}</p>}
        {/* The matched child route renders here */}
        <Outlet />
      </main>

      <footer className="footer">
        <p>Obscura Bank - Web Technologies student project</p>
        <p>No real money, keys or wallets</p>
      </footer>
    </div>
  )
}
