# Common Issues — GHCP App Modernization

> Ten failure modes that show up repeatedly in real GHCP App Mod runs, with
> symptom → cause → fix → prevention. Reach for this guide *before* opening a
> support ticket — the same handful of patterns account for most "the agent
> did something weird" reports.

---

## 1. Empty assessment / "no projects detected"

**Symptom.** You run the assessment and the dashboard is blank, or shows
`Project count: 0`.

**Cause.** The agent walks the file tree looking for `*.csproj` / `*.sln` /
`*.fsproj` / `*.vbproj`. If your projects use unusual locations, are ignored
by `.gitignore`, or live behind a `Directory.Build.props` that conditionally
excludes them, they're invisible.

**Fix.**
- `git ls-files '*.csproj'` to confirm the projects are tracked.
- Open the solution in Visual Studio at least once so `*.suo` / `*.user`
  files are generated and the solution graph resolves.
- Move `Directory.Build.props` so it doesn't gate project discovery.

**Prevention.** Run a manual `dotnet build` (or `msbuild /restore`) at the
repo root before the first assessment. If `dotnet build` can't load the
graph, the agent can't either.

---

## 2. The target in `assessment-config.json` is ignored

**Symptom.** You set `target: "AppService.Linux"` in
`.appmod/.appcat/assessment-config.json` but the dashboard still shows
`Any` (or the wrong target).

**Cause.** The agent caches the previous run's config. If it generated the
file *before* you edited it, the cached run wins.

**Fix.**
- Re-run the assessment with the updated config.
- If the dashboard still doesn't update, delete the entire `.appmod/`
  directory and run again from scratch.

**Prevention.** Whenever you change the target, immediately run the
assessment so the cached state matches.

---

## 3. Predefined task fails because of a missing precondition

**Symptom.** You run **"Migrate to Managed Identity based Database on Azure"**
and the agent reports "cannot proceed — secrets remain in `Web.config`".

**Cause.** Many predefined tasks expect upstream tasks to have run first.
The most common upstream is **"Migrate to secured credentials by using
Managed Identity and Azure Key Vault"** — it removes plaintext secrets so
the SQL task can switch to identity-based connection.

**Fix.** Run the upstream task first. The dependency order is:

```
1. Key Vault (secrets out of config)
   ↓
2. Managed Identity SQL (connection switches to AAD auth)
   ↓
3. OpenTelemetry (replaces logging)
   ↓
4. .NET version upgrade
```

**Prevention.** Use the [Universal predefined-task ordering](../README.md#universal-predefined-task-ordering)
in the top-level README. It encodes these dependencies.

---

## 4. Merge conflict in `.github/upgrades/{scenarioId}/`

**Symptom.** Your wave branch has commits from two scenarios. When the
second scenario writes its plan, you get conflicts in `assessment.md` /
`plan.md` under `.github/upgrades/{scenarioId}/`.

**Cause.** The two scenarios share a parent folder; both write summary
markdown that diverges.

**Fix.**
- Always run **one scenario per branch**.
- Squash-merge each scenario PR before starting the next one.
- If you've already mixed two scenarios on one branch: delete the
  `.github/upgrades/` folder, re-run the *current* scenario only, commit
  the regenerated artifacts.

**Prevention.** Never bundle scenarios. The scenario plan markdown is
regenerated, not append-only — overlapping runs will always conflict.

---

## 5. Context budget exhausted on large solutions

**Symptom.** The agent reports "context limit reached" or silently produces
a partial assessment that misses obvious files.

**Cause.** Large monorepos (30+ projects, 500k+ LOC) exceed the context
window the agent allocates per run.

**Fix.**
- Scope the assessment to one project or one solution at a time. The agent
  honours `--include` / `--exclude` filters when started from CLI.
- Use **AppCAT CLI** for the initial scan, then import the result into the
  agent dashboard. AppCAT has no context limit.
- Split assessments by **layer**: data access projects, then services, then
  UI.

**Prevention.** For solutions over 20 projects, plan a **per-project
assessment matrix** before clicking Run. The dashboard supports import, so
you can stitch results together.

---

## 6. False-positive Mandatory items

**Symptom.** The agent flags `System.Web.HttpContext` as Mandatory but
search shows it's only used in dead code, or it flags `System.Net.Mail`
when you've already replaced the SMTP path.

**Cause.** The agent uses static rules — it doesn't know which paths are
hot, which methods are dead, or which dependencies are about to be deleted.

**Fix.**
- During team review (see [setup-for-success.md, Step 4](setup-for-success.md#4-team-workflow)),
  **annotate** false positives in the exported assessment report. Don't
  delete them — future engineers re-running the assessment need to know why
  they were ignored.
- Suggested annotation format inline in the exported markdown:

  ```
  ### 🔴 Mandatory — System.Web.HttpContext usage
  > **Triage decision (2026-04, jane@contoso.com):** false positive — only used
  > in `LegacyDiagnosticsHandler.cs` which is removed in PR #4421. Re-run
  > assessment after that PR merges to confirm.
  ```

**Prevention.** Always do a team review before remediation, even if it
"looks obvious". The 5 minutes spent triaging false positives saves hours
of agent cycles.

---

## 7. Build breaks after a predefined task runs

**Symptom.** `dotnet build` fails after the agent applies a predefined task.
Usually the failure is a missing using-directive, an ambiguous overload, or
a removed config key.

**Cause.** Predefined tasks are heuristic — they handle the common 80% but
miss edge cases (custom wrappers around `ConfigurationManager`, partial
classes, generated code).

**Fix.** Use the **agent feedback loop** ([guide](agent-feedback-loop.md)):
1. Show the agent the build error.
2. The agent proposes a fix.
3. You build again.
4. Repeat until green.

Keep this loop **short** — 3 turns max. If the agent can't fix it in 3 turns,
revert the scenario branch and apply the change manually.

**Prevention.** Always have a **green build** before running a predefined
task. The agent assumes the baseline compiles; if it doesn't, the agent's
diff is on top of an already-broken state and the build error is ambiguous.

---

## 8. Secrets leaked during Key Vault migration

**Symptom.** You run the Key Vault task. The plaintext secret is removed
from `Web.config` ✅. But it's still visible in the **previous commit's
diff** in your branch — and once you push, in your remote.

**Cause.** The Key Vault task removes the value from the current file. It
doesn't rewrite history.

**Fix.**
- Treat any plaintext secret that *was* in source control as **already
  compromised**. **Rotate it.**
- For pre-push catch: pre-commit hooks (e.g. `git-secrets`,
  `detect-secrets`) — they fail the commit before the secret leaves your
  machine.
- For post-push remediation: rotate first, then GitHub's
  [Secret Scanning](https://docs.github.com/en/code-security/secret-scanning/about-secret-scanning)
  to revoke the credential at the provider.

**Prevention.** **Snapshot then rotate.** See
[setup-for-success.md, Pre-Flight Checklist](setup-for-success.md#1-pre-flight-checklist)
— snapshot every secret to a private vault *before* the Key Vault task
runs, then rotate every snapshotted secret as the first step after
migration.

---

## 9. AppCAT CLI import schema mismatch

**Symptom.** You run `appcat` from the CLI, then "Import assessment report"
in the agent — you get "schema version mismatch" or "report cannot be
parsed".

**Cause.** AppCAT CLI versions are independent of the agent. A report
generated by AppCAT CLI 7.x won't import into a dashboard expecting 6.x
schema (or vice versa).

**Fix.**
- Update AppCAT CLI: `dotnet tool update --global appcat`.
- Update the GHCP App Mod extension in your editor.
- Re-export from the CLI and re-import.

**Prevention.** Pin AppCAT CLI version in the team's `dotnet-tools.json`
manifest. See [`appcat-cli-integration.md`](appcat-cli-integration.md) for
the full integration recipe.

---

## 10. Long-running assessment timeout

**Symptom.** The assessment runs for 10+ minutes then times out. Partial
findings appear; some projects are missing.

**Cause.** Agent-driven assessments combine static rules + LLM analysis.
The LLM step is per-file and can stall on very large files (5k+ LOC),
generated code, or files with embedded resource binaries.

**Fix.**
- Identify the offending file from the agent's progress log
  (`.appmod/.appcat/log.txt`).
- Add it to `.appmod/.appcat/exclude.json` (project-relative globs).
- Re-run.

**Prevention.**
- Exclude `**/Generated/**`, `**/obj/**`, `**/bin/**`, and any
  T4 / Razor / EDMX-generated files by default.
- For files you can't exclude (e.g. one giant `.cs` file), refactor it
  *before* the assessment — assessment quality on a 200-LOC class is much
  higher than on a 5k-LOC class.

---

## Quick reference — which guide handles what

| Issue category | Guide |
|----------------|-------|
| "How do I set up before running?" | [`setup-for-success.md`](setup-for-success.md) |
| "Which predefined task fixes this finding?" | [`predefined-tasks-cheatsheet.md`](predefined-tasks-cheatsheet.md) |
| "How do I read this dashboard?" | [`interpreting-the-dashboard.md`](interpreting-the-dashboard.md) *(coming soon)* |
| "How does AppCAT CLI fit in?" | [`appcat-cli-integration.md`](appcat-cli-integration.md) *(coming soon)* |
| "How do I keep the agent on track during remediation?" | [`agent-feedback-loop.md`](agent-feedback-loop.md) *(coming soon)* |

---

## When to escalate

If you've worked through this guide and the agent is still misbehaving,
collect:

- The exact command / chat prompt you ran.
- `.appmod/.appcat/log.txt` (agent run log).
- Your `assessment-config.json`.
- Your `csproj` / `Web.config` (redacted of secrets).
- A `git rev-parse HEAD` of the commit you ran against.

Then file an issue at the [@modernize-dotnet GitHub repository](https://github.com/dotnet/modernize-dotnet)
or via the Visual Studio "Report a problem" UI. Don't paste secrets.
