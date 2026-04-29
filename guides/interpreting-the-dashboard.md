# Interpreting the Dashboard

> A field guide to the GHCP App Modernization assessment dashboard. What
> each panel means, how to drill in, how to compare targets, and how to
> read the diff between runs.

---

## 1. Top of the dashboard — Application Information

This panel grounds the rest of the report. Always check it **first** —
if the project list is wrong, the rest is meaningless.

| Field | What to look for |
|-------|------------------|
| **Project count** | Matches your expectation? (Compare with `git ls-files '*.csproj'`.) If short, see [common-issues.md #1](common-issues.md#1-empty-assessment--no-projects-detected). |
| **Frameworks detected** | Should match `<TargetFrameworkVersion>` / `<TargetFramework>` in your csproj. Multi-targeting shows as `net48; netstandard2.0`. |
| **Build tools** | `MSBuild (legacy csproj)` vs `MSBuild (SDK-style)` — if mixed, plan SDK-style conversion first. |
| **Target Azure service** | Confirms the agent honoured your `assessment-config.json`. If wrong, see [common-issues.md #2](common-issues.md#2-the-target-in-assessment-configjson-is-ignored). |

> 💡 **Sanity check:** Frameworks + project count + target should match
> what you expect *before* you spend time reading findings. If any are off,
> stop and fix the configuration.

---

## 2. Issue Summary — Heatmap by Domain × Criticality

The summary table shows counts per **Domain** × **Criticality**:

```
                Mandatory   Potential   Optional   Total
Cloud Readiness        12           7          3      22
```

### Read it like this

- **Mandatory total >> 10** — this is a real wave of work. Plan multiple
  scenarios and at least one engineer-week per scenario.
- **Mandatory total ≤ 3** — likely already-modern code or false positives;
  triage them and ship the fix in one PR.
- **Potential >> Mandatory** — the codebase is "OK as-is on Windows" but
  has long-term debt. Usually means a Linux migration is the cliff.
- **Optional dominates** — code is reasonably modern; the assessment is
  mostly polish.

### Don't fixate on the total

A finding's **count** doesn't reflect its **cost**. One Mandatory item
("Migrate WCF service to CoreWCF") can cost more than a hundred Potential
items. Use the per-issue detail to estimate, not the summary.

---

## 3. Issues panel — per-finding detail

Each issue expands to show:

- **Title** and severity emoji.
- **Domain** — currently always `Cloud Readiness` for App Mod.
- **Affected files** — list with line numbers. **Click a `.cs` file** to
  jump to the source location in the editor.
- **Description** — what's wrong and why it matters.
- **Recommended action** — usually a link to a predefined task.
- **Doc link** — Microsoft Learn reference.

### Triage workflow inside this panel

1. Sort by **Criticality** descending.
2. For each Mandatory item, check the **affected files** list. If they're
   all in one project / namespace, the fix is local. If they're spread
   across the solution, the fix needs coordination.
3. Tag each finding (informally, in a comment) as `accept` /
   `false-positive` / `defer` so the next reviewer doesn't re-do the
   triage.

### When the file list is suspicious

- Many findings against `*.Generated.cs` or `*.Designer.cs` → exclude
  generated files in `assessment-config.json`.
- Findings against files you've deleted on a feature branch → re-run the
  assessment after merging that branch.

---

## 4. Switching targets — the dropdown

When `target: "Any"` is configured, the dashboard shows a target dropdown.
Switch between `AppService.Linux`, `ACA`, `AKS.Windows`, etc. to see how
the same findings re-classify.

### What you're looking for

- **Mandatory items that drop to Potential** when you switch to Windows
  → these are *Linux-specific* blockers (`System.Web`, `EventLog`,
  `System.Messaging`, WCF runtime). Tells you the cost of going Linux.
- **Mandatory items that stay Mandatory across all targets** → universal
  findings (managed identity, Key Vault, .NET version, SQL injection).
  Fix these first regardless of target.
- **A target that suddenly explodes** in Mandatory count → that target is
  the wrong one. Pick a different target.

### When to lock the target

After you've switched between 2–3 candidate targets and have a clear
preference, **edit `assessment-config.json` and lock the target**. Future
runs will be cleaner because the agent won't have to compute every
target's recommendations.

---

## 5. Drilling into one issue — what to look for

Open one Mandatory finding. Read in this order:

1. **Recommended action.** Is it a predefined task? Is it manual? If
   manual, does the description tell you what to do?
2. **Affected files.** How many incidents? Are they clustered or spread?
   Are any in test projects (often safe to ignore)?
3. **Doc link.** Microsoft Learn or `learn.microsoft.com/en-us/dotnet/...`
   gives you the canonical fix and migration guide.

### Patterns that look like one finding but are really many

- "Migrate `System.Data.SqlClient`" — one finding, but every `using`
  statement and every `SqlConnection`/`SqlCommand` is a touchpoint. The
  predefined task handles this; manually you'd be at it for hours.
- "Replace `ConfigurationManager`" — one finding, but every
  `ConfigurationManager.AppSettings[...]` becomes an `IConfiguration` /
  `IOptions<T>` change. Best done as part of the .NET version upgrade.

---

## 6. Diffing between runs

After you remediate a scenario, **re-assess**. The dashboard shows a delta
view if the same project has been assessed before:

- **Resolved** — items that disappeared. Confirms the predefined task
  worked.
- **New** — items the previous run missed (or that the remediation
  introduced). Read carefully — agent-driven remediation occasionally adds
  Mandatory items (e.g. swapping log4net for `ILogger` introduces an
  `Application Insights connection string in source` finding if you didn't
  also do the Key Vault task).
- **Unchanged** — items the scenario didn't touch. Plan the next scenario.

### Definition of done for a wave

> The latest assessment shows **zero Mandatory items** against your chosen
> target.

When that's true, you're ready to ship. Export the report and commit it
under `appmod/reports/`.

---

## 7. Exporting the report

The `Export` button produces a JSON file with the full assessment payload —
findings, file references, severity per target, application info.

Use it for:

- **Sharing without re-running.** Reviewer can `Import` and see the same
  data.
- **CI integration.** Drop the JSON into a GitHub Actions artifact and
  parse it for "is Mandatory > 0?" gating.
- **Audit trail.** Commit it under `appmod/reports/{wave-id}.json` so
  every wave has a record.

### File layout convention (recommended)

```
appmod/
└── reports/
    ├── wave-1-pre.json        ← Before any remediation
    ├── wave-1-post-keyvault.json
    ├── wave-1-post-managed-identity.json
    └── wave-1-final.json      ← Zero Mandatory
```

This makes the wave's progress visible at a glance.

---

## 8. Importing a report

The `Import` button takes one of:

- A previously **exported GHCP App Mod report** (`appmod-export.json`).
- A **.NET AppCAT CLI** result (`appcat-output.json`). See
  [`appcat-cli-integration.md`](appcat-cli-integration.md) for the CLI
  recipe.
- A **Dr.Migrate** app context file (`appcontext.json`).

Use case: a staff engineer ran the CLI on a build machine overnight; you
import the result in your editor in the morning.

---

## 9. What the dashboard *doesn't* tell you

- **Hot vs. cold paths.** A finding in `Program.Main` and a finding in
  `LegacyDiagnosticsHandler.cs` look the same severity. Only you know
  which is hot.
- **Test coverage.** A code path with zero tests is a higher remediation
  risk than a covered one.
- **Downstream consumers.** A change to a class library affects every
  consumer; the dashboard shows only the library.
- **Cost.** Mandatory ≠ expensive. "Replace WCF" is one Mandatory item
  and weeks of work.

Pair the dashboard with team knowledge during the
[review step](setup-for-success.md#4-team-workflow). The agent is the
search; you are the judgment.

---

## Quick reference — dashboard elements → guide

| Element | Where to look next |
|---------|---------------------|
| Project count looks wrong | [common-issues.md #1](common-issues.md#1-empty-assessment--no-projects-detected) |
| Target dropdown shows wrong value | [common-issues.md #2](common-issues.md#2-the-target-in-assessment-configjson-is-ignored) |
| Mandatory finding → which task fixes it? | [predefined-tasks-cheatsheet.md](predefined-tasks-cheatsheet.md) |
| Want to gate CI on Mandatory count | [appcat-cli-integration.md](appcat-cli-integration.md) |
| Export taking too long / hangs | [common-issues.md #5](common-issues.md#5-context-budget-exhausted-on-large-solutions) |
| Want to give the agent build feedback | [agent-feedback-loop.md](agent-feedback-loop.md) |
