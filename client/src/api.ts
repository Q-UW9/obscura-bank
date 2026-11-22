// Shape of ASP.NET Core's ProblemDetails (RFC 9457) as our API sends it
// `errors` only exists on 400 validation problems: { email: [...], password: [...] }
export type ProblemDetails = {
    title?: string
    status?: number
    errors?: Record<string, string[]>
}

// Thrown for every non-2xx response, so callers can check status and 
// field errors
export class ApiError extends Error {
    readonly status: number
    readonly problem: ProblemDetails | null

    constructor(status: number, problem: ProblemDetails | null) {
        super(problem?.title ?? `HTTP ${status}`)
        this.name = `ApiError`
        this.status = status
        this.problem = problem
    }
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T | null> {
    const res = await fetch(path, {
        method,
        headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
        body: body === undefined ? undefined : JSON.stringify(body),
    })

    if (!res.ok) {
        // Problems come as application/problem+json
        // Anything else, such as a proxy error, has no problem body
        const isJson = res.headers.get('Content-Type')?.includes('json') ?? false
        const problem = isJson ? ((await res.json()) as ProblemDetails) : null
        throw new ApiError(res.status, problem)
    }

    // 201 register
    // 204 login, logout
    // -- have no body
    const text = await res.text()
    return text ? (JSON.parse(text) as T) : null
}

export const api = {
    get: <T>(path: string) => request<T>('GET', path),
    post: <T = null>(path: string, body?: unknown) => request<T>('POST', path, body),
}