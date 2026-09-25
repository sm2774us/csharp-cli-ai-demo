# Full Walkthrough Step-by-Step on Windows 11 (DOS Prompt)

Split into **one-time setup**, **what you run manually each time**, and
**what the automation does for you**.

## Part 1 — One-time setup (all manual, DOS prompt)

**1. Install prerequisites** (skip any already installed):
```
winget install --id Git.Git -e
winget install --id GitHub.cli -e
winget install --id Microsoft.DotNet.SDK.10 -e
```
Close and reopen the DOS prompt after installing so PATH updates take effect.

**2. Unzip the project and open it:**
```
cd C:\SOFTWARE_ENGG_PROJECTS
tar -xf C:\path\to\csharp-cli-ai-demo.zip
cd csharp-cli-ai-demo
```
(Win11's built-in `tar` handles zip fine; File Explorer's "Extract All" also works.)

**3. Turn it into a git repo and push to GitHub:**
```
git init
git branch -M main
git add -A
git commit -m "initial commit: sub-optimal wordstat CLI"
gh auth login
gh repo create csharp-cli-ai-demo --public --source=. --remote=origin --push
```
`gh auth login` prompts you through browser-based GitHub login — one time only.

> `git branch -M main` matters: a plain `git init` still defaults to a
> branch named `master` unless your global git config sets
> `init.defaultBranch`, which many Windows Git installs don't set. The
> workflows in this repo detect whatever the branch is actually called
> and don't assume `main`, but naming it `main` explicitly here keeps
> things predictable and matches what most tooling expects.

**4. Get a free Gemini API key:**
Go to https://aistudio.google.com/apikey in a browser, click "Create API key," copy it.

> If a Gemini key was ever pasted in plaintext anywhere shareable (chat,
> ticket, log), rotate it at that same URL before using it as a secret —
> treat any exposed key as compromised.

**5. Add it as a repo secret** (this is what lets the GitHub Action call Gemini):
```
gh secret set GEMINI_API_KEY
```
It'll prompt you to paste the key — paste it and hit Enter.

**6. Allow Actions to open pull requests** (off by default on new repos;
without this, `ai-improve.yml` will complete successfully but fail at
the very last step with `GitHub Actions is not permitted to create or
approve pull requests`):
```
gh api -X PUT repos/:owner/:repo/actions/permissions/workflow -f default_workflow_permissions=write -F can_approve_pull_request_reviews=true
```
Run that from inside the repo folder — `:owner` and `:repo` are filled
in automatically from the current directory's git remote. Equivalent
website path, if you'd rather click through it: repo → **Settings** →
**Actions** → **General** → **Workflow permissions** → select **"Read
and write permissions"** → check **"Allow GitHub Actions to create and
approve pull requests"** → **Save**.

**7. Enforce human review on your default branch** (one-time; makes it
a GitHub-level rule, not just workflow convention, that nothing —
including a future `changelog-release.yml` run — can merge without an
actual approval).

Do this by hand — it's faster than setting up a token for it: repo
page on github.com → **Settings** → **Branches** → **Add branch
protection rule** → branch name pattern: your default branch (`main`
or `master`, whichever `git branch -M main` above gave you) → check
**"Require a pull request before merging"** (approvals: `1`) → check
**"Require status checks to pass"**, add `test` → check **"Do not
allow bypassing the above settings"** → **Save**.

(There's also a `setup-branch-protection.yml` workflow that does this
via `gh workflow run setup-branch-protection.yml` — but branch
protection is an admin-only GitHub API that the workflow's built-in
token can never call, so that path needs you to first create a
fine-grained Personal Access Token with Administration:write and set
it as the `ADMIN_PAT` repo secret. The website click above gives the
identical result in under a minute with no extra credential to manage,
so it's the better default unless you're doing this across many
repos.)

Setup is done. You won't repeat Part 1 again for this repo.

> **Verify the .NET SDK before your first push** (this sandbox couldn't
> install/test `dotnet` when the project was generated, so this is the
> most important first check): `dotnet --version` should report
> `10.0.x`. Then run Part 2's local sanity-check block once before
> trusting anything else.

## Part 2 — Manual steps you run each time

**Sanity-check the repo locally (do this first, and especially the
very first time — the project was generated without a local `dotnet`
available to verify it):**
```
dotnet restore csharp-cli-ai-demo.sln
dotnet build csharp-cli-ai-demo.sln
dotnet format --verify-no-changes
dotnet test tests\WordStat.Tests\WordStat.Tests.csproj -p:CollectCoverage=true -p:CoverletOutputFormat=cobertura -p:Threshold=100 -p:ThresholdType=\"line,branch\" -p:ThresholdStat=total
```
All four should pass clean before you touch the AI workflow. If
`dotnet format` reports changes, run `dotnet format` (without
`--verify-no-changes`) once to auto-fix, review the diff, and commit
it before continuing.

**To try the Gemini rewrite locally first, before trusting CI:**
```
set GEMINI_API_KEY=your_key_here
python scripts\ai_improve.py --instruction "improve algorithm and performance" --file src\WordStat\Program.cs --tests tests\WordStat.Tests\ProgramTests.cs
```
This runs the same self-heal loop CI uses: it measures a baseline
benchmark, calls Gemini, auto-fixes/format-checks/tests/benchmarks the
result, and retries on its own if anything fails — so by the time it
exits 0, it's already verified. `git diff` to see what Gemini changed;
`git checkout -- .` to discard it if you don't like the result.

**To trigger the real automated pipeline on GitHub:**
```
gh workflow run ai-improve.yml -f instruction="improve algorithm and performance"
```

**To watch it run:**
```
gh run watch
```

**If a run still fails after 4 self-heal attempts** (rare — the script
already retries format, test, and performance failures internally,
feeding Gemini the exact error each time), no branch is left behind
to clean up: `ai_improve.py` restores the original files and exits
non-zero before anything is committed or pushed. Check the failed
run's log (`gh run view <run-id> --log`) to see what it couldn't
resolve, adjust `scripts/ai_improve.py`'s prompt on `main` if it's a
recurring pattern, commit, push, and just re-run:
```
gh workflow run ai-improve.yml -f instruction="improve algorithm and performance"
```

**When a run succeeds, it opens a PR. Review the diff, then approve
before merging** — branch protection now rejects the merge otherwise
(either on github.com or):
```
gh pr list
gh pr view <number> --web
gh pr review <number> --approve
gh pr merge <number> --squash
```

**For your own hand-written changes** (no AI involved), create a
feature branch, commit and push as usual, then open the PR for it:
```
git checkout -b feature/my-change
:: ...edit, commit, push as usual...
git push origin feature/my-change
gh workflow run open-pr.yml -f branch=feature/my-change -f title="My change"
```
Then review/approve/merge exactly as above.

**To cut a changelog + release, after you've merged above:**
```
gh workflow run changelog-release.yml -f pr_number=<number> -f bump=patch
```
(`bump` is `patch`, `minor`, or `major` — your call each time.) This
runs in two phases because branch protection blocks direct pushes to
the protected branch:

- **Phase 1** (this run): writes the changelog entry on its own branch
  `chore/changelog-vX.Y.Z`, pushes it, and prints the exact command to
  open a PR for it:
  ```
  gh workflow run open-pr.yml -f branch=chore/changelog-vX.Y.Z -f title="chore(release): vX.Y.Z"
  gh pr review <number> --approve
  gh pr merge <number> --squash
  ```
- **Phase 2**: re-run the *same* `changelog-release.yml` command with
  the same `pr_number` and `bump` — it detects the changelog entry is
  now merged, tags that commit, and publishes the GitHub Release.

## Part 3 — What runs automatically, with no input from you

| Trigger | What happens hands-off |
|---|---|
| Every `git push` or PR | `ci.yml` runs: restores/builds with `dotnet build`, checks style with `dotnet format --verify-no-changes` (Google C# style via `.editorconfig`), runs the full xUnit suite, enforces the 100% line+branch coverage gate. You never invoke this directly. |
| `ai-improve.yml` (after you run the `gh workflow run` command) | Checks out a new branch → runs baseline tests → runs `scripts/ai_improve.py`, which **owns the whole self-heal loop**: measures a real baseline benchmark, calls Gemini (`gemini-3.5-flash-lite`), then auto-fixes trivial issues (`dotnet format`, long comment lines), runs the hard format-check + 100%-coverage test gates, and re-benchmarks — if format, tests, *or* performance fail, it feeds Gemini the exact error/benchmark numbers and retries, up to 4 attempts → only once genuinely clean and measurably faster does it commit, push, and open a PR. If it can't self-heal in 4 attempts, it restores the original files and the job fails — nothing is ever pushed. It never merges anything, and branch protection means it couldn't even if it tried. |
| `open-pr.yml` (after you run its `gh workflow run` command) | Checks out your named branch, re-verifies `dotnet format` + the 100%-coverage test gate on it, confirms there are actual commits ahead of the default branch, and opens a PR. Never approves or merges. |
| `changelog-release.yml` (after you run its `gh workflow run` command) | Merges the PR number you gave it if it's still open and approved (no admin override — fails cleanly if not approved); then either writes the changelog entry on its own branch and stops (phase 1), or, once that's merged, tags and publishes the release (phase 2). No AI involved — deterministic on purpose. |
| `setup-branch-protection.yml` (optional; the manual Settings → Branches click in step 7 above does the same thing without needing a PAT) | Configures the default branch to require a human approval and passing CI before any merge, with no bypass even for the repo owner. Requires the `ADMIN_PAT` secret — the built-in token can't call this admin-only API. |

The only things a human must actively do are: (1) kick off
`ai-improve.yml` (or write code by hand and run `open-pr.yml`), (2)
actually review and approve the PR it opens — branch protection
enforces that this step can't be skipped, bot or no bot — and (3) kick
off `changelog-release.yml` (twice, across the two phases) when ready
to cut a release. Everything else — restoring, building, style
checking, auto-fixing trivial AI mistakes, testing, coverage
enforcement, benchmarking, the perf-regression gate, changelog
generation, and tagging — is fully automated once triggered.

## Quick reference — full command sequence, start to finish

```
:: one-time setup
winget install --id Git.Git -e
winget install --id GitHub.cli -e
winget install --id Microsoft.DotNet.SDK.10 -e
cd C:\SOFTWARE_ENGG_PROJECTS
tar -xf C:\path\to\csharp-cli-ai-demo.zip
cd csharp-cli-ai-demo
dotnet restore csharp-cli-ai-demo.sln
dotnet build csharp-cli-ai-demo.sln
dotnet test tests\WordStat.Tests\WordStat.Tests.csproj -p:CollectCoverage=true -p:Threshold=100 -p:ThresholdType=\"line,branch\" -p:ThresholdStat=total
git init
git branch -M main
git add -A
git commit -m "initial commit: sub-optimal wordstat CLI"
gh auth login
gh repo create csharp-cli-ai-demo --private --source=. --remote=origin --push
gh secret set GEMINI_API_KEY
gh api -X PUT repos/:owner/:repo/actions/permissions/workflow -f default_workflow_permissions=write -F can_approve_pull_request_reviews=true
:: then set branch protection by hand: repo -> Settings -> Branches -> Add rule (see step 7 above)

:: each improvement cycle
gh workflow run ai-improve.yml -f instruction="improve algorithm and performance"
gh run watch
gh pr list
gh pr view <number> --web
gh pr review <number> --approve
gh pr merge <number> --squash
gh workflow run changelog-release.yml -f pr_number=<number> -f bump=patch
:: (phase 1 prints an open-pr.yml command; run it, approve, merge, then re-run the line above)
```
