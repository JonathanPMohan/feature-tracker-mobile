# Feature Tracker Mobile

A feature request board — submit ideas, vote, comment, and see what's planned, in progress or shipped — as a **.NET API** and a **React Native app built with Expo**.

It's a rebuild of my Next.js feature request tracker in a new stack, with the same product rules, written milestone by milestone with Claude Code. **Work in progress:** the plan and milestones are in [`docs/PLAN.md`](docs/PLAN.md).

## Stack

| Part      | Technology                                                    |
| --------- | ------------------------------------------------------------- |
| API       | ASP.NET Core (.NET 10, Minimal APIs), C#                      |
| Database  | PostgreSQL with Entity Framework Core (from M2)               |
| Mobile    | React Native, Expo (SDK 58), Expo Router, TypeScript          |
| Tests     | xUnit (API), Jest + React Native Testing Library (mobile)     |
| CI        | GitHub Actions; `main` is protected and requires both checks  |

## Run it locally

Prerequisites: the .NET 10 SDK, Node 26, and Expo Go on a phone.

```sh
# API → http://localhost:5080
dotnet tool restore
dotnet run --project api/FeatureTracker.Api

# Mobile app: scan the QR code with your phone's camera
cd mobile
npm ci
npx expo start
```

Tests: `dotnet test api/FeatureTracker.slnx` and `cd mobile && npm test`.

To enable the pre-commit hook in a fresh clone: `git config core.hooksPath .githooks`.
