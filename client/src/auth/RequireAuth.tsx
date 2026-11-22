import { Navigate, Outlet, useLocation } from 'react-router'
import { useAuth } from './authContext'

// Route guard - only for the UI!
// The API checks the session on every request anyway (RequireAuthorization)
export function RequireAuth() {
  const { user, loading } = useAuth()
  const location = useLocation()

  // Still waiting for GET /api/me
  // Without this a reload would bounce to /login for a moment
  if (loading) return <p className='status'>Loading...</p>

  // Remember where the user wanted to go, so login can send them back
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />

  // Logged in: render the child route
  return <Outlet />
}