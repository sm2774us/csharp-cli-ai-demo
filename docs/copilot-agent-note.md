# Using GitHub Copilot's coding agent as a second comparison point

`ai-improve.yml` automates only Gemini (`gemini-3.5-flash-lite`, called
directly via REST -- the only model here with a genuine free API tier).
GitHub Copilot's autonomous coding agent (free on GitHub Free/Pro with a
monthly premium-request quota) does not run as a plain `uses:` step
inside a workflow YAML the same way -- it's invoked by **assigning a
GitHub Issue to `@copilot`**, and it opens its own PR.

To use it as a second data point for the same "improve algorithm and
performance" task, for a manual apples-to-apples comparison against the
Gemini-authored PR:

1. Open an issue in this repo:
   - Title: `Improve algorithm and performance`
   - Body: paste the same constraints block used in `ai-improve.yml`'s
     underlying prompt (`scripts/ai_improve.py`'s `INITIAL_PROMPT`) --
     scope to `src/WordStat/Program.cs` + its test file, Google C#
     style, keep 100% coverage, no new NuGet packages, use
     `Dictionary<string,int>` / `PriorityQueue` / `Regex.Matches`.
2. Assign the issue to `Copilot`.
3. Copilot opens a PR on its own branch; `ci.yml` runs automatically
   against it (format check + 100%-coverage gate) same as any other PR.
4. Run `dotnet run --project scripts/benchmark -- --words 20000 --vocab 15000`
   locally against Copilot's branch and compare its numbers to the
   Gemini-authored PR from `ai-improve.yml`.

This keeps Copilot in the same review-gated model (PR + CI + manual
merge) as the Gemini workflow, just triggered differently.

## OpenAI Codex

Codex CLI has no free API tier as of this writing, so it isn't wired
into any automation here -- see the main README for why only Gemini is
used for the automated `ai-improve.yml` path.
