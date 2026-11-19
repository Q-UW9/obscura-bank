import { useEffect, useState } from 'react'

type Health = { status: string; database: string }

function App() {
  const [health, setHealth] = useState<Health | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetch('/api/health/db')
      .then((res) => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        return res.json() as Promise<Health>
      })
      .then(setHealth)
      .catch((err: Error) => setError(err.message))
  }, [])

  return (
    <main>
      <h1>Obscura Bank</h1>
      {error && <p>API error: {error}</p>}
      {!error && !health && <p>Checking API...</p>}
      {health && <p>Database: {health.status} ({health.database})</p>}
    </main>
  )
}

export default App