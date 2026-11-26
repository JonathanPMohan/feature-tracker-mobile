# Spec: setup and C# primer (M0)

**Owner:** Jonathan runs the installs, git, and the commands in "Your steps". Claude writes the scaffolding config, the primer, `CLAUDE.md`, the first tests and the pre-commit hook, then reviews. No product code yet.

After M0, the repo has an empty .NET API and an empty Expo app. The API runs on your Mac, the app runs on your iPhone, and one test on each side passes in a pre-commit hook.

## Before you start: installs

They're in `docs/PLAN.md` → "Install before M0". When you're done, these should all print a version:

```
dotnet --list-sdks      # includes a 10.0.x line (an 11.0.x line too is fine)
docker --version
node --version          # 22 or newer
```

Also check that Expo Go is on your iPhone.

**My versions:**

- `dotnet --list-sdks`: 10.0.404 and 11.0.100
- `dotnet ef --version`: 11.0.0
- `node --version`: v26.13.0
- `docker --version`: 29.9.2
- Expo Go: installed

## Rules (what "done" means)

| #   | Rule                                                                                                                                                                                                                                                                                                                                                                                 |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Z1  | A git repo at the project root with a `.gitignore` covering .NET (`bin/`, `obj/`), Node (`node_modules/`), Expo (`.expo/`), and `.env*`. `main` holds the first commit; M0's work happens on the `m0-setup` branch.                                                                                                                                                                  |
| Z2  | `api/FeatureTracker.slnx` (`.slnx` is .NET 10's newer, readable solution format) contains `FeatureTracker.Api` (a web project) and `FeatureTracker.Api.Tests` (xUnit), with the test project referencing the API project. A `global.json` at the root pins SDK 10.0.x (`rollForward: latestFeature`), so `dotnet --version` inside the repo prints 10.x even with .NET 11 installed. |
| Z3  | Nullable reference types and "warnings as errors" are on in both projects. This is the C# version of TypeScript strict: a possible `null` that isn't handled stops the build.                                                                                                                                                                                                        |
| Z4  | `dotnet run` serves `GET /` → `{ "name": "Feature Tracker API" }` on port 5080.                                                                                                                                                                                                                                                                                                      |
| Z5  | One xUnit unit test passes: `PageParam.Parse`, a C# port of the old tracker's `parsePageParam` (S3: `"0"`, `"-1"`, `"abc"` and an empty value all mean page 1). M5 uses it.                                                                                                                                                                                                          |
| Z6  | `mobile/` is an Expo app (current SDK, TypeScript strict, Expo Router) with the template's example screens removed. It opens on your iPhone in Expo Go and shows "Feature Tracker".                                                                                                                                                                                                  |
| Z7  | One Jest test passes in `mobile/` (React Native Testing Library renders the home screen and finds the title).                                                                                                                                                                                                                                                                        |
| Z8  | A git **pre-commit hook** runs `dotnet format --verify-no-changes`, the API unit tests, and the mobile `tsc --noEmit`, lint and Jest in a few seconds, with no Docker needed. A failure blocks the commit.                                                                                                                                                                           |
| Z9  | `CLAUDE.md` at the root records the working agreement (who writes what, the read-only old repo, specs before code, the five-step endpoint order) and the commands.                                                                                                                                                                                                                   |
| Z10 | `docs/csharp-primer.md` covers what M1–M5 need, each with a TypeScript side-by-side: types and `var`, `class` vs `record`, nullable `?` and `!`, `async Task<T>`, LINQ (`.Where`/`.Select` ≈ `.filter`/`.map`), attributes (`[Fact]`), and dependency injection (how `Program.cs` hands services to endpoints).                                                                      |

## Your steps

1. **Git refresher:** run `git init` in the project folder, make an empty first commit on `main`, then `git switch -c m0-setup`.
2. **Scaffold the API** (Claude gives you the exact commands with each one explained): `dotnet new sln`, `dotnet new web`, `dotnet new xunit`, `dotnet sln add`, `dotnet add reference`. Run `dotnet run`, then open `http://localhost:5080/`.
3. **Scaffold the app:** `npx create-expo-app@latest mobile`, then `npm run reset-project` to strip the examples. Run `npx expo start` and scan the QR code with your iPhone's Camera app.
4. **Install the hook:** `git config core.hooksPath .githooks` (once per clone; the hook is a plain shell script in the repo, so there's nothing extra to install).
5. Read the primer, both tests and their comments, and `CLAUDE.md`. Ask anything.
6. Commit, then run `/code-review` before asking me to review.

## Claude writes

| File                                                          | What                                                     |
| ------------------------------------------------------------- | -------------------------------------------------------- |
| `global.json`, `dotnet-tools.json` (🧑 ran the commands; SDK 10 puts it at the root) | Pin SDK 10 (Z2) and `dotnet-ef` 10 as a local tool       |
| `api/Directory.Build.props`                                   | Nullable and warnings as errors for every project (Z3)   |
| `api/FeatureTracker.Api/Program.cs`                           | The `GET /` endpoint, explained line by line (Z4)        |
| `api/FeatureTracker.Api/Paging/PageParam.cs`                  | `PageParam.Parse` (Z5)                                   |
| `api/FeatureTracker.Api.Tests/Unit/PageParamTests.cs`         | A `[Theory]` with `[InlineData]` rows, commented (Z5)    |
| `mobile/src/app/index.tsx`, `mobile/__tests__/home-screen.test.tsx` | Home screen + test (Z6, Z7)                              |
| `.githooks/pre-commit`, `.gitignore`, `.editorconfig`         | Z1, Z8 (`.editorconfig` is what `dotnet format` follows) |
| `CLAUDE.md`, `docs/csharp-primer.md`                          | Z9, Z10                                                  |

## Coming from Next.js

| Then                                   | Now                                                                                                        |
| -------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| `package.json` + `tsconfig.json`       | `.csproj` (dependencies and compiler settings in one XML file) + `Directory.Build.props` (shared settings) |
| `.nvmrc` / `engines` in `package.json` | `global.json` (which SDK version this repo uses)                                                           |
| `npm install <pkg>`                    | `dotnet add package <Pkg>` (from NuGet, .NET's npm)                                                        |
| `npm run dev`                          | `dotnet run` (or `dotnet watch` for hot reload)                                                            |
| `"strict": true`                       | `<Nullable>enable</Nullable>` + `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`                      |
| Prettier + ESLint                      | `dotnet format` (follows `.editorconfig`) + built-in compiler analyzers                                    |
| `it.each([...])` in Vitest             | `[Theory]` + `[InlineData(...)]` in xUnit                                                                  |
| `npx create-next-app`, `npm run dev`   | `npx create-expo-app`, `npx expo start`, then scan the QR code                                             |
| Husky / your old pre-commit hook       | A plain `.githooks/pre-commit` script + `core.hooksPath`                                                   |

## 🧑 Decisions

Write a sentence for each under "Jonathan's decisions".

### A. Minimal APIs or Controllers?

The plan says Minimal APIs. They're the default in new .NET templates, and an endpoint reads like one function, close to a server action. Many existing team codebases use **Controllers** instead: classes with `[HttpGet]` methods. Both are fully supported, and the concepts are the same. **If you can, ask your new team which they use**, and we'll match it. If you can't ask, keep Minimal APIs.

### B. Where does Claude Code's permission list live?

The old project kept an allowlist in `.claude/settings.json`, which is committed and shared. Decide which commands to allow without asking. Read-only ones like `dotnet test`, `dotnet build` and `git status` are safe. Keep migrations (`dotnet ef database update`), `git push` and anything that deletes as "ask first". This is the M0 Claude Code skill.

## Out of scope

The database (M2), GitHub and CI (M1), any product feature, and styling beyond a title.

## Jonathan's decisions

**A.** Minimal APIs for now, since they're the default in new templates and read like my Next.js server actions. If my new team uses Controllers, we'll switch to match them; the concepts are the same.

**B.** The allowlist lives in `.claude/settings.json`, committed so it's shared and reviewable. Allowed without asking: reading files, `dotnet build`, `dotnet test`, `npm test`, and read-only git commands like `git status`, `git diff`, and `git log`. Ask first: installing packages, migrations (`dotnet ef database update`), `git push`, anything on GitHub, and anything that deletes.
