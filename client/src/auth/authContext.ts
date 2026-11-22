import { createContext, useContext } from 'react'

// Match API's UserProfile record
// System.Text.Json sends camelCase
export type User = {
  email: string
  role: 'user' | 'admin'
}

export type AuthState = {
  user: User | null // null = not logged in
  loading: boolean // true until the first GET /api/me has answered
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

// null default:
// using the hook outside the provider is a bug, thus it is "loud"
export const AuthContext = createContext<AuthState | null>(null)

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>')
  return ctx
}