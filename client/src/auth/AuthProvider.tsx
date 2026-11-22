import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, ApiError } from '../api'
import { AuthContext, type AuthState, type User } from './authContext'

// Asks the server who the cookie belongs to
// 401 is the normal "not logged in" answer
async function fetchMe(): Promise<User | null> {
  try {
    return await api.get<User>('/api/me')
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) return null
    throw err
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  // On app start and on every reload, the cookie is the only source of truth
  useEffect(() => {
    fetchMe()
      .then(setUser)
      .catch((err: unknown) => {
        console.error('Could not load the current user', err)
        setUser(null)
      })
      .finally(() => setLoading(false))
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    // 204 + Set-Cookie: 
    // the browser stores the cookie, JS never sees it
    await api.post('/api/auth/login', { email, password })
    setUser(await fetchMe())
  }, [])

  const register = useCallback(async (email: string, password: string) => {
    // 201 and no session: 
    // registering does not log you in (the page sends you to /login)
    await api.post('/api/auth/register', { email, password })
  }, [])

  const logout = useCallback(async () => {
    await api.post('/api/auth/logout')
    // Only after the server confirmed: 
    // if the request failed, the session still exists
    setUser(null)
  }, [])

  const value = useMemo<AuthState>(
    () => ({ user, loading, login, register, logout }),
    [user, loading, login, register, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}