# Spec: database foundation (M2)

**Owner:** Claude writes the configuration, `AppDbContext`, the health endpoint, the test setup, the phone's status line, and all tests. Jonathan runs the installs, Docker and the migration commands (each explained), and makes the decisions below. There are no tables yet: the data model is Jonathan's, in M4. M2's Claude Code skill is **Claude Code hooks, and how they differ from git hooks**.

After M2, the API has a database it can reach, a migration workflow, and integration tests that run against a real Postgres, on your Mac and in CI. Your iPhone talks to the API over Wi-Fi for the first time.

## Rules

Each rule is tested unless it says otherwise.

### Database and configuration

| #   | Rule                                                                                                                                                                                                                                                                                                                                  |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| B1  | `docker-compose.yml` at the repo root runs Postgres (decision A) as the **dev** database `feature_tracker`, on host port **5433**. Your old tracker's database already uses 5432, and both must be able to run at once. Data survives restarts (a named volume). A health check reports when Postgres is ready. Reviewed, not tested. |
| B2  | The API reads its connection string from configuration as `ConnectionStrings:Default`, never from a hard-coded string in C#. Where the dev value lives is decision B.                                                                                                                                                                 |
| B3  | `AppDbContext` (EF Core 10 with the Npgsql provider) is registered once in `Program.cs` and handed to endpoints by dependency injection (primer, section 7).                                                                                                                                                                          |
| B4  | Table and column names are `snake_case` in Postgres, as in your old project (decision C).                                                                                                                                                                                                                                             |
| B5  | Migrations live in `api/FeatureTracker.Api/Data/Migrations/`. The first one, `Initial`, has no tables (M4 adds them), but applying it creates EF Core's `__EFMigrationsHistory` table. That's the proof the workflow works end to end.                                                                                                |
| B6  | In Development, the API logs every SQL statement EF Core sends, so you can see the SQL each query becomes (the primer promised this). Outside Development it doesn't. Reviewed, not tested.                                                                                                                                           |

### The health endpoint

| #   | Rule                                                                                                                                                                     |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| B7  | `GET /health` returns **200** with the body `Healthy` when the API can reach the database. It uses ASP.NET Core's built-in health checks with an EF Core database check. |
| B8  | When the database is unreachable, `GET /health` returns **503** with `Unhealthy`, within a few seconds, and the API keeps running.                                       |
| B9  | **Server:** the response never includes details: no connection string, host name, or exception message. A public endpoint must not describe your infrastructure.         |

### Integration tests

| #   | Rule                                                                                                                                                                                        |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| B10 | Integration tests live in `FeatureTracker.Api.Tests/Integration/` (namespace `…Tests.Integration`). The pre-commit hook still runs only `Unit.` tests; CI runs everything (M1, G3).         |
| B11 | One Postgres container (Testcontainers, same image as B1) starts once per test run and is shared by every integration test. Migrations are applied to it before the first test.             |
| B12 | Tests call the API over HTTP in memory with `WebApplicationFactory`: the real `Program.cs`, the real middleware and endpoints, with only the connection string swapped for the container's. |
| B13 | The integration tests pass in CI on GitHub's Linux runners, which have Docker. Proven by PR #4's checks.                                                                                    |

### The phone

| #   | Rule                                                                                                                                                                                                                                        |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| B14 | In Development, the API listens on every network interface (`0.0.0.0:5080`), not only `localhost`, so the phone can reach it.                                                                                                               |
| B15 | The app reads the API's address from `EXPO_PUBLIC_API_URL` in `mobile/.env.local` (gitignored, since your Mac's IP is yours). If it's missing, the app says so instead of guessing.                                                         |
| B16 | The home screen shows **"API: connected"** when `/health` answers 200, **"API: unreachable"** when the request fails or returns anything else, and "Checking…" while waiting. Tested with a mocked `fetch`; checked by hand on your iPhone. |

## Who writes what

| File or task                                                                | What                                                    | Who         |
| --------------------------------------------------------------------------- | ------------------------------------------------------- | ----------- |
| `docker-compose.yml`                                                        | B1                                                      | Claude      |
| `api/FeatureTracker.Api/Data/AppDbContext.cs`                               | An empty `DbContext` (no tables yet), commented         | Claude      |
| `Program.cs`                                                                | Database, naming convention, SQL logging, health checks | Claude      |
| `appsettings.Development.json`, `launchSettings.json`                       | B2 (per decision B), B6, B14                            | Claude      |
| `Integration/PostgresFixture.cs`, `Integration/ApiFactory.cs`               | The shared container and the test API (B11, B12)        | Claude      |
| `Integration/HealthTests.cs`, `Integration/MigrationTests.cs`               | B5, B7–B9                                               | Claude      |
| `mobile/src/api/config.ts`, `mobile/src/components/ApiStatus.tsx`, its test | B15, B16                                                | Claude      |
| `.claude/settings.json` hook + script                                       | Decision D                                              | Claude      |
| Install packages, start Docker, create and apply the migration              | The commands below                                      | 🧑 Jonathan |
| Decisions A–D                                                               | Below                                                   | 🧑 Jonathan |

### 🧑 Your pieces

No C# from you this milestone (your code starts in M3). You run the commands where the workflow is the lesson:

1. **Packages**, all pinned to 10.x. NuGet already has 11.0 versions, which require .NET 11 and would fail on our `net10.0` project:
   ```
   dotnet add api/FeatureTracker.Api package Npgsql.EntityFrameworkCore.PostgreSQL --version "10.*"
   dotnet add api/FeatureTracker.Api package Microsoft.EntityFrameworkCore.Design --version "10.*"
   dotnet add api/FeatureTracker.Api package EFCore.NamingConventions --version "10.*"
   dotnet add api/FeatureTracker.Api package Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore --version "10.*"
   dotnet add api/FeatureTracker.Api.Tests package Microsoft.AspNetCore.Mvc.Testing --version "10.*"
   dotnet add api/FeatureTracker.Api.Tests package Testcontainers.PostgreSql
   ```
   I'll explain each one when you run it. (`EFCore.NamingConventions` only applies with decision C's recommended option.)
2. **Start Postgres:** `docker compose up -d`, then `docker compose ps` until it reports healthy.
3. **The first migration:** `dotnet ef migrations add Initial --project api/FeatureTracker.Api --output-dir Data/Migrations`. Then read the three generated files with me.
4. **Apply it:** `dotnet ef database update --project api/FeatureTracker.Api`. Then look inside with `docker compose exec db psql -U postgres -d feature_tracker -c '\dt'` and find `__EFMigrationsHistory`.
5. **The phone:** put your Mac's IP in `mobile/.env.local`, run the API and `npx expo start`, and see "API: connected" on the iPhone. Then stop the database (`docker compose stop db`) and reload the app (press **r** in the Expo terminal): "API: unreachable". Two prompts may appear the first time, and both need **Allow**: macOS asking whether `dotnet` may accept incoming connections, and iOS asking whether Expo Go may find devices on your local network.

   **A risk to check early:** iOS normally blocks plain `http://` requests. Expo Go is expected to allow them for development, but that's the first thing to suspect if the phone says "unreachable" while `curl http://<your Mac's IP>:5080/health` works from the Mac.

**Order of work, as before:** I write everything first. The integration tests fail until your packages, migration and database exist, and pass once they do.

## Tests (Claude)

| File                                   | Type        | Covers                                                                                                                                                                                                                                       |
| -------------------------------------- | ----------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Integration/HealthTests.cs`           | integration | B7: 200 `Healthy` with the container. B8: 503 `Unhealthy` with a connection string pointing at a closed port, and the API still answers `GET /` afterwards. B9: neither body contains the host, port, database name or an exception message. |
| `Integration/MigrationTests.cs`        | integration | B5: after the fixture's migrations, `__EFMigrationsHistory` lists `Initial`. Also: no migration is pending, so the model and the migrations agree. In M4, this catches a model change that someone forgot to make a migration for.           |
| `Integration/RootEndpointTests.cs`     | integration | `GET /` through `WebApplicationFactory` (M0's Z4), the simplest example of the pattern, commented line by line                                                                                                                               |
| `mobile/__tests__/api-status.test.tsx` | unit        | B15, B16: connected, unreachable on a network error, unreachable on a 503, "Checking…" first, and the missing-URL message                                                                                                                    |

**On a machine without Docker running,** the integration tests fail with a clear "Docker isn't running" message rather than a timeout. The hook never runs them, so committing never needs Docker.

## Claude Code hooks (the M2 skill)

Your git hook runs when **you** commit. A **Claude Code hook** runs when **I** use a tool: before or after I edit a file, run a command, and so on. It's configured in `.claude/settings.json`, and it runs your script, not my judgment. That's the point: it applies even when I forget. Decision D picks the first one.

|                  | Git hook (M0)                             | Claude Code hook (M2)                                                          |
| ---------------- | ----------------------------------------- | ------------------------------------------------------------------------------ |
| Triggered by     | `git commit`                              | Claude using a tool (`PreToolUse`, `PostToolUse`, …)                           |
| Configured in    | `.githooks/pre-commit` + `core.hooksPath` | `"hooks"` in `.claude/settings.json`                                           |
| Can block?       | Yes: a non-zero exit cancels the commit   | `PreToolUse` can refuse the action before it happens; `PostToolUse` runs after |
| Protects against | Anything committed by anyone              | Mistakes in what Claude does, as it does them                                  |

## Coming from Next.js

| Your tracker                                           | This project                                                                                                                                                                                    |
| ------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `DATABASE_URL` in `.env`                               | `ConnectionStrings:Default` from configuration: `appsettings.json` → `appsettings.Development.json` → user secrets → environment variables, later sources winning (primer, section 7's builder) |
| `drizzle.config.ts`                                    | `AppDbContext` + `options.UseNpgsql(...)` in `Program.cs`                                                                                                                                       |
| `drizzle-kit generate --name x`                        | `dotnet ef migrations add X`: C# migration files instead of SQL. Read them; `dotnet ef migrations script` shows the SQL.                                                                        |
| `drizzle-kit migrate`                                  | `dotnet ef database update`                                                                                                                                                                     |
| `drizzle/meta/_journal.json`                           | `__EFMigrationsHistory` (in the database) + `AppDbContextModelSnapshot.cs` (in the repo)                                                                                                        |
| A long-running Docker test database                    | Testcontainers: a fresh, throwaway Postgres for each test run, started from the tests                                                                                                           |
| Vitest `globalSetup`                                   | An xUnit assembly fixture, shared by every test class                                                                                                                                           |
| Calling a server action in a test with a faked session | `WebApplicationFactory`: real HTTP requests to the real app, in memory                                                                                                                          |
| `next dev` on `localhost:3000`                         | The API on `0.0.0.0:5080`, because the client is a phone, not a browser on the same machine                                                                                                     |

## 🧑 Decisions

Write a sentence for each under "Jonathan's decisions".

### A. Which Postgres?

| Option                              | For                                                 | Against                                                                              |
| ----------------------------------- | --------------------------------------------------- | ------------------------------------------------------------------------------------ |
| **18** (now 18.7)                   | A year of fixes; every tool and library supports it | Not the newest                                                                       |
| **19** (now 19.0)                   | The newest                                          | A `.0` release: the first bug-fix release hasn't shipped yet, and some tools may lag |
| **17** (the plan's original choice) | Matches what was current when the plan was written  | Two versions behind for a brand-new project                                          |

I'd pick **18**. Testcontainers uses the same image tag, so dev, tests and CI always match.

### B. Where the dev connection string lives

The dev password only protects a Postgres container on your own laptop, bound to `127.0.0.1`.

| Option                                        | For                                                                                                      | Against                                                               |
| --------------------------------------------- | -------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------- |
| **`appsettings.Development.json`, committed** | Zero setup: clone, `docker compose up`, run. Common for local-only dev databases                         | A password in git, even a throwaway one, is a habit some teams forbid |
| **`dotnet user-secrets`**                     | Nothing secret in the repo; the .NET way for dev secrets (stored in your user profile, outside the repo) | One extra setup command per clone, and the README must explain it     |

I'd pick **`appsettings.Development.json`**, with a comment saying the password is local-only, and switch to user secrets as soon as anything real appears (an API key in a later milestone). Either is defensible; your new team may have a rule.

### C. Table and column naming

| Option                                                  | `FeatureRequest.AuthorId` becomes | For                                                                    | Against                                                                                |
| ------------------------------------------------------- | --------------------------------- | ---------------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| **snake_case** (the `EFCore.NamingConventions` package) | `feature_request.author_id`       | Postgres convention; raw SQL needs no quotes; same as your old project | One more package. M3's Identity tables become `asp_net_users` instead of `AspNetUsers` |
| **EF Core's default**                                   | `"FeatureRequests"."AuthorId"`    | No package; matches most .NET tutorials                                | Every raw SQL query needs double quotes, because Postgres lowercases unquoted names    |

I'd pick **snake_case**: M4 to M8 will have raw SQL (the rate-limit `INSERT … ON CONFLICT`, `FOR SHARE` in #14), and quoting every name gets old fast.

### D. The first Claude Code hook

| Option                         | Kind                                        | What it does                                                                                                                                                                 |
| ------------------------------ | ------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Format on edit**             | `PostToolUse` on Edit/Write of a `.cs` file | Runs `dotnet format` on that file right after I change it, so my C# always matches `.editorconfig` before your hook ever sees it                                             |
| **Protect applied migrations** | `PreToolUse` on Edit/Write                  | Refuses any edit to an existing file in `Data/Migrations/`. A migration that has run must never change; a fix is a new migration. Creating new migration files stays allowed |

Both are small. I'd pick **protect applied migrations**: it's the one that _prevents_ a real mistake (editing history that's already in someone's database), and it shows a hook saying no. Format-on-edit only saves a step.

## Out of scope

Tables and the data model (M4); auth (M3); production hosting and when migrations run there (local only, by decision); connection retries and pooling settings beyond the defaults; HTTPS on the phone (plain HTTP on your home Wi-Fi, dev only).

## Jonathan's decisions

**A.** Postgres 18, current and stable. 19.0 is too new.

**B.** The dev connection string goes in appsettings.Development.json, committed with a "local-only password" comment. It only works on my Mac.

**C.** snake_case, so raw SQL in M4–M8 stays readable without quotes.

**D.** A hook that blocks edits to applied migrations. Changing a migration that has already run is a real risk I learned about in my first project.
