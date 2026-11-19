# Obscura Bank

An educational online banking system with a Monero-style privacy wallet, built to study web application security one feature at a time.

- **Course:** Web Technologies
- **Category:** Online Banking System
- **Stack:** React + Vite, React Router, Recharts (client); C# with ASP.NET Core minimal APIs on .NET 10 (API); PostgreSQL with Npgsql + Dapper; REST/JSON

> **Stack revision (Nov 2026).** The first version of this proposal used Node.js + Express for the API. The API is now planned in C# / ASP.NET Core instead, pending the instructor's approval. Features, tiers and the threat model are unchanged. Only the libraries behind them differ.

> **No real money.** The app uses a simulated currency (`XMR-t`) stored in its own database. No real keys, wallets or funds are involved. Connecting to the Monero *stagenet* (a public test network with worthless coins) is an optional Tier 3 extra.

## Overview

Users open an account, hold a balance in a test currency, send money to other users, and receive payments through one-time deposit addresses. Behind the interface it behaves like a small bank: every movement of money goes into a double-entry ledger, every sensitive action is authenticated and logged, and the API is hardened against common web attacks.

### Why Monero

Monero is a privacy-focused cryptocurrency, and several of its ideas map well onto a web app:

- a fresh **subaddress** for every incoming payment,
- amounts stored as integer **piconero** (1 XMR = 10^12 piconero),
- a **10-block confirmation window** before received funds can be spent.

### Why this project

A banking app makes the cost of each security mistake easy to picture. A weak password store, a missing CSRF check, or a race between two transfers all have an obvious price.

I plan to use generative AI while building it. To make sure I learn from it, every feature is paired with the concepts it teaches, and a feature only counts as done when I can explain it without the tool.

## Feature tiers

| Tier | Name | Status | Scope |
|---|---|---|---|
| 1 | Essentials | Committed | A working bank: accounts, transfers, ledger and baseline security |
| 2 | Buildup | Planned | Hardened security plus the Monero-style payment features |
| 3 | Complex extras | Stretch | Real test-network integration and advanced techniques |

Tier 1 is the minimum I commit to. Tier 2 starts once Tier 1 works end to end. Tier 3 is pursued only if time allows.

### Tier 1 · Essentials (Committed)

| ID | Feature | Description | Study |
|---|---|---|---|
| E1 | Registration and login | Users sign up with email and password. Passwords are hashed with Argon2id, never stored in plain text. | password hashing, salts, work factors |
| E2 | Secure sessions | Logged-in state is kept in a server-side session behind an `HttpOnly`, `Secure`, `SameSite` cookie, with expiry and logout. | sessions vs. JWT, cookie flags, session fixation |
| E3 | Accounts and balances | Each user has an account with a balance in XMR-t. Amounts are stored as integer piconero (C# `long`, PostgreSQL `bigint`) so no rounding errors can occur. | integer money, floating-point pitfalls, 64-bit integer limits |
| E4 | Transfers with a double-entry ledger | Sending money writes a debit and a matching credit inside one database transaction. A balance can never go negative, and two simultaneous transfers cannot spend the same funds. | ACID transactions, row locking, race conditions, double-entry bookkeeping |
| E5 | Transaction history | A paginated list of incoming and outgoing transfers with filters by date, direction and amount. | pagination, query design, indexes |
| E6 | Input validation and safe queries | Every request body is validated on the server before it is used, and all SQL uses parameterized queries (Dapper parameters, never string concatenation). | SQL injection, server-side validation, trust boundaries |
| E7 | Security headers | The server sends a Content Security Policy and related headers through its own middleware, and React output is never injected as raw HTML. | XSS, CSP, clickjacking |
| E8 | Roles and protected routes | Two roles: user and admin. Admins can view users and freeze accounts, but cannot move a user's funds. Access is checked on every API route, not only in the UI. | authorization vs. authentication, IDOR, least privilege |

### Tier 2 · Buildup (Planned)

| ID | Feature | Description | Study |
|---|---|---|---|
| B1 | Two-factor authentication | Optional TOTP codes from an authenticator app (Otp.NET), enrolled by scanning a QR code (QRCoder), with one-time recovery codes. | TOTP (RFC 6238), shared secrets, recovery flows |
| B2 | Rate limiting and lockout | Login and transfer endpoints are rate limited (ASP.NET Core rate-limiting middleware), and repeated failed logins temporarily lock the account. | brute force, credential stuffing, user enumeration |
| B3 | CSRF protection | State-changing requests require a CSRF token (ASP.NET Core antiforgery) in addition to the SameSite cookie. | CSRF, same-origin policy, CORS |
| B4 | Step-up confirmation and idempotency | Large transfers and security changes ask for the password or a 2FA code again. Each transfer carries an idempotency key, so a double click or a retried request cannot send money twice. | re-authentication, idempotency keys, replay attacks |
| B5 | Audit log and active sessions | Logins, transfers and setting changes are logged with time and device. Users can see their active sessions and sign out the others. | audit trails, session revocation, logging without leaking secrets |
| B6 | Subaddresses | Each payment request generates a new one-time deposit address, so separate payments to the same user cannot be linked by address. Addresses are simulated but follow Monero's format and length. | address reuse, unlinkability, stealth addresses (concept) |
| B7 | Payment requests with confirmations | A user creates a request for an amount and shares its address. Incoming payments show as `unconfirmed (n/10)` and become spendable after 10 simulated blocks, matching Monero's lock time. | block confirmations, pending vs. settled funds, background jobs (`BackgroundService`) |
| B8 | Dashboard | Balance-over-time chart, recent activity and pending payments on one page. | data aggregation, charting, React state management |

### Tier 3 · Complex extras (Stretch)

Each one is self-contained, so any subset can be added.

| ID | Feature | Description | Study |
|---|---|---|---|
| X1 | Monero stagenet integration | Replace the simulated wallet with a real `monero-wallet-rpc` connected to stagenet, generating real subaddresses and reading real confirmations. | JSON-RPC, node synchronization, hot wallet risks |
| X2 | Passkeys | Passwordless login with WebAuthn as an alternative to password plus TOTP. | public-key authentication, phishing resistance |
| X3 | Live updates | Incoming payments and confirmation counts update in real time through Server-Sent Events, without refreshing the page. | SSE vs. WebSocket, authenticated streams |
| X4 | Encryption at rest | Sensitive fields such as TOTP secrets are encrypted in the database with AES-GCM (`System.Security.Cryptography.AesGcm`), using a key kept outside the database. | symmetric encryption, key management, envelope encryption |
| X5 | Suspicious activity alerts | Simple rules flag logins from a new device or transfers far above a user's usual amount, and hold them for confirmation. | fraud heuristics, device fingerprinting limits |
| X6 | Privacy explainer | An interactive page that shows how ring signatures, stealth addresses and RingCT hide sender, receiver and amount in Monero. | ring signatures, Pedersen commitments, privacy trade-offs |
| X7 | Automated security testing | Integration tests for the attack cases in the threat model, a dependency audit (`dotnet list package --vulnerable`, `npm audit`), and an OWASP ZAP scan of the running app. | security testing, supply-chain risk, OWASP Top 10 |

## Example: a transfer in the ledger

Alice sends 1.25 XMR-t to Bob. The ledger stores two entries that share one transfer ID and always sum to zero.

| Transfer | Account | Entry | Piconero | XMR-t |
|---|---|---|---:|---:|
| tx_7f3a91 | alice | Debit | −1 250 000 000 000 | −1.250000000000 |
| tx_7f3a91 | bob | Credit | +1 250 000 000 000 | +1.250000000000 |
| | | **Sum** | 0 | 0.000000000000 |

Balances are derived from these entries, so the history and the balance can never disagree.

## Architecture

```
React client  ──HTTPS / JSON──▶  ASP.NET Core API  ──SQL──▶  PostgreSQL
                                        │
                                        └──JSON-RPC──▶  monero-wallet-rpc (stagenet, Tier 3)
```

- **React client:** Vite, React Router; forms, dashboard, charts; no secrets in the browser.
- **ASP.NET Core API (.NET 10, C#):** minimal APIs; auth, sessions, CSRF; validation, rate limits; ledger service. Raw SQL through Npgsql + Dapper, so transactions and row locks stay visible. Authentication is written by hand on top of cookie authentication, not ASP.NET Core Identity, so each security feature is built and understood rather than inherited.
- **PostgreSQL:** users, sessions, ledger entries, audit log.
- **monero-wallet-rpc (Tier 3):** called only by the API, never by the browser; replaces the simulated address and confirmation service.

## Threat model

| Threat | Mitigation | Feature |
|---|---|---|
| Stolen database exposes passwords | Argon2id hashing with per-user salts | E1 |
| SQL injection | Parameterized queries, schema validation | E6 |
| Cross-site scripting | React escaping, strict CSP, HttpOnly cookies | E2, E7 |
| Reading another user's transactions | Ownership checks on every route | E8 |
| Double spending through concurrent requests | Database transactions with row locks | E4 |
| Password guessing | Rate limiting, lockout, 2FA | B1, B2 |
| Cross-site request forgery | SameSite cookies plus CSRF tokens | B3 |
| Duplicate or replayed transfers | Idempotency keys, step-up confirmation | B4 |
| Hijacked session goes unnoticed | Audit log, session list with remote logout | B5 |
| Linking a user's payments by address | One-time subaddresses | B6 |

## How I will work

1. Build Tier 1 first, as a thin version that works end to end.
2. Read about each feature's concept before generating or writing code.
3. Use generative AI to speed up implementation, then review the code line by line and rewrite anything I cannot explain.
4. Keep a short note per feature: what it protects against, how it works, what breaks without it.
5. Write at least one test per security feature that performs the attack and checks that it fails (xUnit + `WebApplicationFactory`, which runs the real API in memory).
6. Move to the next tier only when the current one is stable.

## Deliverables

**Code**
- Git repository with the React client and ASP.NET Core API
- Database schema and seed data with demo users
- Setup instructions in the README

**Documentation**
- List of completed features by tier
- Threat model with mitigations
- Per-feature concept notes
- Live demo of the main flows

---

*Obscura Bank is an educational project for the Web Technologies course. It does not handle real money and is not affiliated with the Monero Project.*
