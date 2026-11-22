import { useAuth } from '../auth/authContext'

// Start page behind the guard
// Placeholder until E3 (accounts, balances) - logout is in the Layout banner now
export function HomePage() {
  const { user } = useAuth()

  return (
    <section className="panel">
      <div className="ph">
        <h1>Welcome <span>back</span></h1>
      </div>
      <div className="pb">
        <dl className="facts">
          <div><dt>Email</dt><dd>{user?.email}</dd></div>
          <div><dt>Role</dt><dd>{user?.role}</dd></div>
        </dl>
        <p className="strip"><strong>Coming next</strong>Accounts and balances (E3)</p>
      </div>
    </section>
  )
}
