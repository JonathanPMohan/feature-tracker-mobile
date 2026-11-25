# Feature Tracker Mobile — Plan

_Last updated: 2026-11-25_

## Context

Jonathan (a former PM, now back in development) built a feature request tracker in Next.js, Drizzle and Postgres (`~/Desktop/projects/feature-request-tracker`, **read-only**: we read its docs and never change them). His new job uses React, .NET, React Native and Expo, and works with Claude. This project rebuilds the same product as a **.NET API plus an Expo mobile app**, so he learns that stack before he starts.

The product rules come from the old `docs/PLAN.md` and `docs/specs/m3–m8`, including the decisions he made there: active-only default board, "try again in N minutes", voting only on active requests, the demo account can't vote or comment, comments allowed on any status, a submission key for comments, any status to any status, one admin check per action, and the #14 vote/status lock.

The working style carries over from the old project:
- One milestone at a time, each on its own branch and PR, with a spec in `docs/specs/` written before any code.
- 🧑 Jonathan writes the **data model** and the **permission checks**, including each endpoint's authorization steps. Claude writes everything around them, stubbing his parts with `throw new NotImplementedException()` so the tests fail until he fills them in.
- Claude writes **all tests** as commented examples to study, and reviews Jonathan's code.
- Every spec has a **"Coming from Next.js"** box (the full map is below), and the specs explain C# and .NET as if he's rusty.

Decisions made in this conversation:
- **A new, separate database**, with its own Postgres container and EF Core migrations.
- **ASP.NET Core Identity with bearer tokens** for sign-in.
- **Rate limits ported as Postgres counters**, covering sign-up by IP plus submit, vote and comment per user.
- **GitHub and CI, with everything running locally.** There's no hosting; the iPhone reaches the API over home Wi-Fi.

---

## Architecture

```
iPhone (Expo Go)                         later: React web client (M10)
   │  HTTPS-less dev over Wi-Fi: http://<Mac LAN IP>:5080
   │  Authorization: Bearer <token>   (token kept in expo-secure-store)
   ▼
ASP.NET Core API (.NET 10, Minimal APIs) ── runs on the Mac
   ├─ Identity ─────────── users, password hashing, /auth/login, /auth/refresh
   ├─ Endpoints (by feature: Requests, Votes, Comments, Admin, Auth)
   │     each one: 1. who is this?          → token → ClaimsPrincipal → current user
   │               2. are they allowed?     → Permissions (🧑 pure static functions)
   │               3. is the input valid?   → validation
   │               4. under the rate limit? → RateLimiter (Postgres counter)
   │               5. do it                 → EF Core query / transaction
   │     answers are HTTP status codes + ProblemDetails JSON (401/403/404/422/429)
   ├─ Permissions.cs ─────── 🧑 all permission rules
   ├─ Data/ (AppDbContext, entities, configurations) ── 🧑 the data model
   └─ OpenAPI document ──── generates the TypeScript types the app uses
   ▼
PostgreSQL 17 in Docker (dev DB) · Testcontainers (a fresh DB for each test run)
```

The order of the five steps is the same as in the old PLAN.md, for the same reason: the rate limit is the only step with a side effect, so it runs last.

| Layer        | Choice                                                  | Why                                                                                                                                         |
| ------------ | ------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| API          | **.NET 10** (LTS until Nov 2028), ASP.NET Core Minimal APIs, pinned with `global.json` | The long-term-support release, and the one most teams run (.NET 11, released Nov 2026, is a shorter-support release). Minimal APIs are the default in new templates and read like one function per endpoint. If the new team uses Controllers, we switch in M0 (ask them). |
| Data         | **EF Core 10 + Npgsql**, code-first migrations          | The .NET standard ORM                                                                                                                        |
| Auth         | **ASP.NET Core Identity**, bearer + refresh tokens      | Microsoft's built-in equivalent of Better Auth                                                                                               |
| API tests    | **xUnit**, `WebApplicationFactory`, **Testcontainers**  | In-process HTTP calls against a real throwaway Postgres                                                                                      |
| Mobile       | **Expo (latest SDK at M0)**, Expo Router, TypeScript strict | Expo Go on the App Store only runs the newest SDK, so we pin to whatever is current when M0 starts                                       |
| Data fetching | **TanStack Query**                                     | Caching, loading states and optimistic updates (the vote button)                                                                             |
| API types    | **openapi-typescript** from the API's OpenAPI document  | The app's request and response types come from the C# code, so the two can't drift apart                                                    |
| Mobile tests | **Jest + jest-expo + React Native Testing Library**     | Component and hook tests. The rules live on the server, so most tests are xUnit.                                                             |
| Repo         | One monorepo: `api/`, `mobile/`, `docs/`                | One PR per milestone covers both sides                                                                                                       |
| CI           | GitHub Actions: `dotnet test` + mobile type checks, lint and Jest | Branch protection on `main`                                                                                                  |

**Layout:** a single API project. No "Clean Architecture" split into 4 projects, because that adds layers to learn before they're useful (we can discuss it in M10 if the team uses it).

```
api/FeatureTracker.Api/          Program.cs, Data/, Auth/, Permissions.cs, RateLimiting/, Features/<Feature>/
api/FeatureTracker.Api.Tests/    Unit/ (no DB)  ·  Integration/ (Testcontainers)
mobile/                          src/app/ (Expo Router screens; tests in __tests__/, never in src/app/), src/api/ (generated types + client), src/components/
docker-compose.yml               dev Postgres
```

---

## Coming from Next.js: concept map

The specs expand each row when it comes up.

| Your tracker (Next.js)                            | This project (.NET / Expo)                                                                  |
| ------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| Server action (a POST with a `Next-Action` header) | API endpoint: `app.MapPost("/requests", ...)`, with a real URL and HTTP verb               |
| `{ ok: false, message }` returned with 200        | A real status code (401, 403, 404, 422, 429) with a ProblemDetails body                     |
| Drizzle `schema.ts`                               | Entity classes, plus `IEntityTypeConfiguration<T>` (Fluent API) for keys, indexes and CHECKs |
| `drizzle-kit generate` / `migrate`                | `dotnet ef migrations add` / `dotnet ef database update`                                    |
| Hand-written `--custom` SQL migration             | `migrationBuilder.Sql(...)` inside a migration                                              |
| Drizzle `db.transaction(...)`                     | `await using var tx = await db.Database.BeginTransactionAsync()`                            |
| Better Auth                                       | ASP.NET Core Identity                                                                       |
| Session cookie                                    | Bearer token in the `Authorization` header, stored in `expo-secure-store`                   |
| `getCurrentUser()`                                | `ClaimsPrincipal` (`HttpContext.User`), then load the user                                  |
| Zod schema                                        | Request DTO `record` + validation (decided in the M5 spec)                                  |
| `lib/permissions.ts` pure functions               | `Permissions.cs` static methods: same idea, same unit tests                                 |
| Vitest                                            | xUnit (`[Fact]`, `[Theory]`)                                                                |
| Docker test DB + integration tests                | Testcontainers + `WebApplicationFactory`                                                    |
| Next.js App Router (`app/page.tsx`)               | Expo Router (`app/index.tsx`): file-based routing with the same idea, on a phone            |
| Server components fetching data                   | The app fetches over HTTP with TanStack Query                                               |
| `useOptimistic`                                   | TanStack Query optimistic updates                                                           |
| Tailwind, `<div>`                                 | `StyleSheet`, `<View>`, `<Text>`, `<Pressable>`                                             |
| `npm run make-admin`                              | `dotnet run -- make-admin <email>`                                                          |
| `async/await`, TypeScript types                   | The same `async/await` in C#, plus `Task<T>`, nullable reference types and `record`         |

---

## Milestones

Each milestone is roughly one week at 8–10 hours, with its own branch, spec and PR. 🧑 = Jonathan writes it.

| #       | Milestone              | Scope                                                                                                                                                                                                                                                                                                                                                                                                                                                                  | Claude Code skill             |
| ------- | ---------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------- |
| **M0**  | Setup and C# primer    | Monorepo and git init; `dotnet new` solution with the API and test projects; a C# primer page (types, `record`, nullability, `async Task`, LINQ, dependency injection); first xUnit test; Expo app scaffolded and **running on your iPhone in Expo Go**; pre-commit hook (`dotnet format --verify-no-changes`, unit tests, `tsc`, eslint); `CLAUDE.md`; this plan as `docs/PLAN.md`                                                                                  | `CLAUDE.md`, permissions      |
| **M1**  | GitHub and CI          | Public repo; GitHub Actions for the API and the mobile app; protected `main`                                                                                                                                                                                                                                                                                                                                                                                           | Commits, PRs, `/code-review`  |
| **M2**  | Database foundation    | `docker-compose.yml` Postgres; EF Core and `AppDbContext`; the migration workflow; a `/health` endpoint that touches the DB; Testcontainers + `WebApplicationFactory` integration test setup in CI; **the phone fetches `/health` over Wi-Fi** (LAN binding, local network permission)                                                                                                                                                                                 | Claude Code hooks             |
| **M3**  | Auth                   | Identity with bearer and refresh tokens, explained step by step; register (with display name), login, refresh, `GET /me`; 🧑 `Role` and `IsDemo` on the user, with DB CHECKs for valid roles and **no demo admin**; 🧑 `CanBecomeAdmin` + a `make-admin` command; demo seed; tests that passwords are hashed and tokens expire. Mobile: sign-in and sign-up screens, the token in SecureStore, a "use demo account" button, an auth context                          | Plan mode                     |
| **M4**  | 🧑 Data model          | 🧑 `FeatureRequest`, `Vote`, `Comment` (with the submission key), and the rate-limit counter table (keyed so it covers both user and IP); constraints, delete behavior and indexes in EF configurations and migrations. Claude writes the constraint tests (the old D1–D9 rules) and reviews                                                                                                                                                                         | Reviewing my own code         |
| **M5**  | Submit and browse      | `GET /requests` (sort, filter, paging), `GET /requests/{id}`, `POST /requests` (request + first vote in one transaction); validation; the **rate-limit helper** (one atomic `INSERT … ON CONFLICT`), also applied to sign-up by IP; OpenAPI → TypeScript types; 🧑 `CanSubmitRequest` + endpoint auth steps. Mobile: board, status filter, detail, submit form. **Product decision for you:** page numbers or infinite scroll on a phone | Writing tests with Claude     |
| **M6**  | Voting                 | `PUT /requests/{id}/vote` and `DELETE …/vote`. "Set, not flip" maps naturally onto idempotent HTTP verbs. 🧑 `CanVote` + auth steps; optimistic button. **Walkthrough:** `curl` a vote with no token, then with the demo's token while `CanVote` is commented out. **→ First shareable version** (demoed from your phone)                                                                                                                                              | Debugging with Claude         |
| **M7**  | Comments               | `GET` and `POST /requests/{id}/comments` with a submission key; 🧑 `CanComment` + auth steps. Mobile: thread and comment box                                                                                                                                                                                                                                                                                                                                         | —                             |
| **M8**  | Admin                  | `PATCH /requests/{id}/status`, `DELETE /requests/{id}`, `DELETE /comments/{id}`; the #14 fix (`SELECT … FOR SHARE` inside an EF transaction); 🧑 `CanChangeStatus`, `CanDeleteRequest`, `CanDeleteComment` + auth steps; admin controls with confirmation. Mobile: admin controls                                                                                                                                                                                    | `/security-review`            |
| **M9**  | Polish and README      | Empty, loading, error and offline states; pull to refresh; README with screenshots and architecture; an optional Maestro end-to-end flow (needs Xcode's simulator, so we decide then)                                                                                                                                                                                                                                                                                 | Polishing a portfolio repo    |
| **M10** | _Optional:_ React web client | Vite + React + TanStack Query against the same API (CORS, the same generated types); talk through when a team would split the API into projects                                                                                                                                                                                                                                                                                                                 | —                             |

Each spec follows the old format: numbered rules (**Server** = it holds when the UI is skipped), **Who writes what**, **Your pieces**, **Tests**, **Decisions (yours)** with a "Jonathan's decisions" section, **Out of scope**, and **Coming from Next.js**.

**Carried-over decisions:** the old decisions stand unless a spec flags a reason to revisit one. Two specs will flag one:
- **M4:** IDs. Integers are still fine.
- **M5:** paging on a phone.

**Carried into later specs** (from reviews):
- **M5, from M0's `/code-review` (#4):** `PageParam.Parse` accepts pages up to `int.MaxValue`, so the offset `(page - 1) * pageSize` can overflow and return a 500 instead of an empty page. The M5 spec adds a rule and a test row (`?page=2147483647` shows the empty state), and computes the offset safely or caps the page.

---

## Testing

| Layer                    | Tool                                       | What it covers                                                  | DB?            | Pre-commit | CI  |
| ------------------------ | ------------------------------------------ | --------------------------------------------------------------- | -------------- | ---------- | --- |
| API unit                 | xUnit                                      | `Permissions`, validation, small helpers                        | No             | ✅         | ✅  |
| API integration          | xUnit + `WebApplicationFactory` + Testcontainers | Endpoints over HTTP, constraints, races (rate limit, votes, #14), auth | Real Postgres | ❌ | ✅ |
| Mobile                   | Jest + React Native Testing Library        | Components and hooks (vote button, forms, auth context)         | No (mocked API) | ✅        | ✅  |
| Manual on device         | Expo Go                                    | Each milestone's paths, on your iPhone                          | Dev DB         | —          | —   |

Claude writes the tests with comments explaining what each one proves and why. Old rule IDs (S8, V4, C7, …) are referenced in test names so you can trace them back to the old specs.

---

## Install before M0 (you run these)

1. **.NET 10 SDK**: `brew install --cask dotnet-sdk@10`. (The plain `dotnet-sdk` cask always installs the newest SDK, which is .NET 11 now. It can stay installed: SDKs live side by side, and the repo's `global.json` picks 10.) Check with `dotnet --list-sdks`, which should show a 10.0.x line.
2. **EF Core CLI**: nothing to install now. In M0 it becomes a *local* tool pinned in the repo (`dotnet tool install dotnet-ef --version 10.*`), so its version matches EF Core 10 on every machine and in CI.
3. **Editor support**: the VS Code extension **C# Dev Kit** (it installs the C# extension). Rider (free for non-commercial use) also works.
4. **Docker Desktop**: you likely have it from the old project. Testcontainers needs it running.
5. **Node 22 LTS or newer**: you likely have it.
6. **Expo Go** from the iPhone App Store. Keep the iPhone and Mac on the same Wi-Fi. When the app first asks for **Local Network** permission, allow it.
7. **Optional:** the `gh` CLI, if it isn't installed already (M1).

No global Expo CLI is needed (we use `npx expo`). No Xcode is needed unless we choose Maestro in M9.

---

## Verification (per milestone)

- `dotnet test` passes: unit tests plus integration tests against Testcontainers Postgres.
- `npm test` and `npx tsc --noEmit` pass in `mobile/`.
- CI is green on the PR, and the branch is protected.
- **Manual check on the iPhone in Expo Go** covers the milestone's rules, including at least one **server** rule shown with `curl` (for example, a 401 with no token, or a 403 for the demo's vote).
- Jonathan runs `/code-review` before review (and `/security-review` in M8).

