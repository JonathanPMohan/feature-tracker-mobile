# Spec: GitHub, CI and a protected `main` (M1)

**Owner:** Claude writes the CI workflow, the README stub and the PR description, and runs the `gh` and `git push` commands, each after asking. Jonathan makes the decisions below and sets up branch protection himself in GitHub's settings, because it's the security control this milestone is about. M1's Claude Code skill is **drafting commits and PRs, and `/code-review` on a PR**.

After M1, the code lives on GitHub, every pull request runs the same checks as the pre-commit hook, and nothing reaches `main` unless those checks pass. M0's branch becomes PR #1.

M1 is smaller than a usual milestone: probably a few hours. There's no database yet (M2) and no deployment (local only, by decision).

## Rules

| #   | Rule                                                                                                                                                                                                                                                                                                                                     |
| --- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| G1  | A public GitHub repo (decision A) with `main` and `m0-setup` pushed. `main` holds only the empty initial commit until PR #1 merges.                                                                                                                                                                                                      |
| G2  | `.github/workflows/ci.yml` runs on every pull request to `main` and on every push to `main`.                                                                                                                                                                                                                                             |
| G3  | An **API** job sets up .NET from `global.json` (SDK 10, the same as your Mac), runs `dotnet tool restore`, `dotnet format --verify-no-changes`, `dotnet build`, and **all** of `dotnet test`. Today that's only unit tests; from M2, the integration tests run here too. GitHub's Linux runners have Docker, which Testcontainers needs. |
| G4  | A **Mobile** job sets up Node from `mobile/.nvmrc` (decision D), runs `npm ci`, then `typecheck`, `lint` and `npm test -- --ci`.                                                                                                                                                                                                         |
| G5  | Both jobs always run, even when only one side changed. Unlike the hook, CI doesn't pick checks by file: it's the gate, so it checks everything.                                                                                                                                                                                          |
| G6  | The job names stay exactly `API` and `Mobile`. Branch protection refers to them by name, so renaming one silently removes its protection.                                                                                                                                                                                                |
| G7  | The workflow can only read the repo (`permissions: contents: read`). If a newer push arrives on the same PR, the old run is cancelled.                                                                                                                                                                                                   |
| G8  | **`main` is protected** (Jonathan, in GitHub's settings): changes arrive only by pull request, `API` and `Mobile` must pass, the branch must be up to date with `main` before merging, no force pushes, no deletion, and **no bypass, including for you as the admin**.                                                                  |
| G9  | **Proven, not assumed:** a throwaway PR with a failing test (M0 step 5's `42 → 43`, committed with `--no-verify` to skip the hook) shows a red ❌ and a blocked merge button. Then it's closed without merging.                                                                                                                          |
| G10 | A short `README.md`: what the project is, the stack, how to run it, and a link to `docs/PLAN.md`. The full README with screenshots comes in M9.                                                                                                                                                                                          |

### Why the hook _and_ CI?

The hook is fast feedback on your machine, but it's optional: `--no-verify` skips it, and a fresh clone doesn't have it until someone runs `git config core.hooksPath`. CI is the enforced gate. It runs on GitHub's machines, and branch protection makes it impossible to skip. G9 proves it.

## Order of work

The order matters because of one GitHub detail: **a check can only be made "required" after it has run at least once.** So the workflow goes in before the protection.

1. **Claude:** a new branch `m1-github-ci` off `m0-setup`, with this spec, `ci.yml`, `mobile/.nvmrc` and the README. Then a commit, after showing you `git status` and the message.
2. **Claude, after asking:** `gh repo create` (decision A), push `main`, then push `m0-setup` and `m1-github-ci`.
3. **Claude:** open **PR #1** (`m0-setup` → `main`) with a drafted description. CI runs on it for the first time, so the `API` and `Mobile` checks now exist.
4. **🧑 You:** create a **ruleset** for `main` in GitHub (G8). I'll give the exact clicks. Rulesets are GitHub's current way to protect branches; the older "branch protection rules" page still works, but rulesets are what teams use now.
5. **You:** run `/code-review` on PR #1, read the findings with me, then **merge PR #1** with your merge style (decision B).
6. **Claude:** PR #2 for `m1-github-ci`, rebased onto the new `main`. The same flow: CI, review, you merge.
7. **Both:** G9, the failing PR, to see the gate hold.

## Claude writes

| File                       | What                                                                      |
| -------------------------- | ------------------------------------------------------------------------- |
| `.github/workflows/ci.yml` | The two jobs, explained line by line in comments                          |
| `mobile/.nvmrc`            | The Node version for CI (and for `nvm use` on your Mac)                   |
| `README.md`                | G10                                                                       |
| PR descriptions            | What changed, why, how it was tested, plus Claude Code's attribution line |

No new tests: M1 runs the existing ones in a new place.

## Coming from Next.js

| Your tracker (M1 there)                         | This project                                                                                                           |
| ----------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| GitHub Actions CI                               | The same, with one job per language                                                                                    |
| Vercel deploys every PR as a preview URL        | No deployment (local only, by decision). The equivalent for Expo would be EAS Update previews, which are out of scope. |
| A Playwright smoke test against each deployment | Nothing to test remotely: the API runs only on your Mac.                                                               |
| `actions/setup-node` with `npm ci`              | The same for `mobile/`, plus `actions/setup-dotnet` for the API                                                        |
| Classic branch protection                       | A ruleset: the same protections, newer settings page                                                                   |

## 🧑 Decisions

Write a sentence for each under "Jonathan's decisions".

### A. The repo's name

Public, per the plan. **`feature-tracker-mobile`** matches the folder. Something like `feature-tracker-dotnet` would say more on your GitHub profile next to the Next.js one, since this repo is as much a .NET API as a mobile app. You can rename it later, and GitHub redirects the old URL.

### B. How PRs merge into `main`

| Option           | `main` gets                                                   | For                                                                       | Against                                                                                                    |
| ---------------- | ------------------------------------------------------------- | ------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| **Squash merge** | One commit per PR                                             | `main` reads like a list of milestones. Each commit passed CI as a whole. | The branch's individual commits (like M0's hook fix) disappear from `main`, though the PR page keeps them. |
| **Merge commit** | Every branch commit, plus a merge commit                      | Full history; nothing is rewritten                                        | `main` includes in-between commits that never passed CI on their own                                       |
| **Rebase merge** | Every branch commit, replayed in a line, with no merge commit | A linear history with every commit                                        | The same in-between commits as a merge commit, and their hashes change                                     |

The ruleset can allow just the one you pick, so it stays consistent. I'd pick **squash**: one milestone becomes one tested commit on `main`.

### C. How the workflow refers to GitHub's actions

| Option                | Looks like                                 | For                                      | Against                                                                                                                                     |
| --------------------- | ------------------------------------------ | ---------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| **Major version tag** | `actions/checkout@v5`                      | Readable; you get fixes automatically    | A tag can be moved. If an action's repo were compromised, the tag could point at malicious code (this has happened to third-party actions). |
| **Full commit SHA**   | `actions/checkout@<40 hex chars> # v5.0.0` | Immutable: exactly the code you reviewed | Unreadable without the comment, and updates are manual, or need Dependabot (out of scope for now)                                           |

Every action here comes from GitHub itself (`actions/…`), which lowers the risk. I'd pick **major tags** now and switch to SHAs if a third-party action is ever added. Either is defensible in an interview, as long as you can say why.

### D. Which Node version CI uses

Your Mac runs **Node 26**, which became the long-term-support line in October 2026. Pinning `26` in `mobile/.nvmrc` makes CI match your Mac, which avoids "passes locally, fails in CI" surprises. The alternative, the older LTS (24), would only make sense if your team pins it. I'd pin 26.

## Out of scope

Deployment and EAS builds; Dependabot; code coverage reports; integration tests (M2); required approving reviews (you're the only person, and GitHub won't let you approve your own PR).

## Jonathan's decisions

**A.** feature-tracker-mobile, matching the folder. The GitHub description names the stack (.NET API, React Native with Expo), so it's clear next to my Next.js tracker.

**B.** Squash merge, so each milestone is one tested commit on main. Co-author lines are kept.

**C.** Major version tags for GitHub's own actions, as the spec recommends.

**D.** Node 26, matching my Mac, so CI and local runs behave the same.
