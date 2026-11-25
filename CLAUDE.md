# Feature Tracker Mobile

A .NET API and an Expo mobile app for the feature request board. Jonathan is rebuilding his Next.js tracker in this stack to learn it before a new job. The plan, the product rules and the milestones are in `docs/PLAN.md`. Each milestone's spec is in `docs/specs/`.

## Working agreement

- **One milestone at a time**, on its own branch (`m<N>-<name>`), with a spec in `docs/specs/` written and read **before** any code.
- **Jonathan writes the data model and the permission checks**, including each endpoint's "who is this" and "are they allowed" steps. Claude writes everything around them and stubs his parts with `throw new NotImplementedException()`, so the tests fail until he fills them in. **Never write his parts for him**; review them instead.
- **Claude writes all tests**, with comments explaining what each test proves and why. Jonathan studies them.
- **Explain for someone rusty**, especially C# and .NET. Compare each new idea to his Next.js / Drizzle / Better Auth project (`docs/PLAN.md` has the concept map).
- **Jonathan runs installs and git commands himself.** Give him the command and what it does. Ask first before installing packages, running migrations, pushing, anything on GitHub, or deleting files.
- **`~/Desktop/projects/feature-request-tracker` is read-only.** Read its docs for reference; never create, edit or delete anything there. `.claude/settings.json` denies the Edit, Write and NotebookEdit tools there. Shell commands can't be blocked by path reliably, so never aim a writing command (`sed -i`, `mv`, `cp`, `>`, `npm install`) at that folder; the approval prompt is only the last line of defense.
- Product rules and Jonathan's earlier decisions carry over from that project's specs unless a spec here says otherwise.

## The endpoint order (every write endpoint)

1. **Who is this?** The token gives the current user.
2. **Are they allowed?** `Permissions` (Jonathan's).
3. **Is the input valid?**
4. **Under the rate limit?** This is the only step with a side effect (it uses quota), so nothing that can still refuse may run after it.
5. **Do it:** EF Core query or transaction.

Every rule is enforced **on the server**. Hiding a button in the app is a convenience, not a security control.

## Layout

```
api/FeatureTracker.slnx          the .NET solution
api/FeatureTracker.Api/          the API (Minimal APIs)
api/FeatureTracker.Api.Tests/    Unit/ (no database) · Integration/ (Testcontainers, from M2)
mobile/                          the Expo app. Screens in src/app/; tests in __tests__/, never in src/app/
docs/                            PLAN.md, specs/, csharp-primer.md
.githooks/pre-commit             format, type checks, lint, unit tests
global.json                      pins the .NET 10 SDK (.NET 11 is also installed)
```

## Commands

Run these from the repo root unless noted.

| What                | Command                                                                 |
| ------------------- | ----------------------------------------------------------------------- |
| Run the API         | `dotnet run --project api/FeatureTracker.Api` → http://localhost:5080   |
| API tests           | `dotnet test api/FeatureTracker.slnx`                                   |
| API unit tests only | `dotnet test api/FeatureTracker.slnx --filter "FullyQualifiedName~FeatureTracker.Api.Tests.Unit."` |
| Fix C# formatting   | `dotnet format api/FeatureTracker.slnx`                                 |
| Run the app         | `cd mobile && npx expo start`, then scan the QR code with the iPhone    |
| App checks          | `cd mobile && npm run typecheck && npm run lint && npm test`            |
| Add an app package  | `cd mobile && npx expo install <pkg>` (never `npm install <pkg>`: Expo picks SDK-compatible versions) |

## Conventions

- **C#:** nullable reference types on and warnings as errors (`api/Directory.Build.props`). Style comes from `.editorconfig`: file-scoped namespaces, `_camelCase` private fields, braces always. Namespaces match folders.
- **Tests:** unit tests go in the `FeatureTracker.Api.Tests.Unit` namespace (the pre-commit hook selects them by name). Test names read as sentences: `Anything_else_means_the_first_page`. Reference the old project's rule IDs (S3, V4, …) where a test proves one.
- **Expo:** SDK 58. APIs change between SDKs, so check the versioned docs rather than memory; `mobile/AGENTS.md` explains how.
- **Specs** follow the old project's format: numbered rules (**Server** = it holds even when the UI is skipped), Who writes what, Your pieces, Tests, Decisions with "Jonathan's decisions", Out of scope, and Coming from Next.js.
