# Setup for Success — GHCP App Modernization

> A pre-flight checklist, target-decision tree, branch strategy, team workflow,
> and definition-of-done for the GHCP App Modernization (`@modernize-dotnet`)
> agent. Use this **before** clicking "Run".

The agent is good. It is not magic. The teams who get clean assessments and
clean predefined-task outputs are the ones who set up the project correctly
*first*. This guide collects the setup steps that make the difference.

---

## 1. Pre-Flight Checklist

Run through these before the first assessment. Skipping any of them is the
single most common cause of "the agent did something weird".

### Source-control hygiene

- [ ] Working tree is **clean** (`git status` shows no changes).
- [ ] You're on a dedicated branch (e.g. `modernize/wave-1`) — **not** `main`.
- [ ] The repo has a CI pipeline that builds the legacy code on Windows.
      You'll need it as a regression net during remediation.
- [ ] No `.appmod/` directory exists yet (delete it if a prior attempt left one;
      the agent prompts to continue or start fresh, and "fresh" is usually
      what you want for a clean baseline).

### Project structure

- [ ] **One project, one Target Framework Moniker (TFM).** Multi-targeting
      (`net48;netstandard2.0`) confuses the assessment — split into separate
      projects or pick the highest-value TFM first.
- [ ] All projects build on a clean machine (`dotnet build` or
      `msbuild /restore`). The agent doesn't strictly require a successful
      build, but if it can't load the project graph, findings will be
      under-counted.
- [ ] **SDK-style `csproj` is preferred but not required.** If you're on
      legacy non-SDK csproj, expect the agent to suggest the
      **"SDK-style conversion"** scenario as Step 0 — accept it before
      anything else.

### Configuration & secrets

- [ ] **Snapshot every secret** in `Web.config` / `App.config` / `appsettings.json`
      to a private vault *before* running the agent. Predefined tasks like
      "Migrate to Key Vault" remove plaintext values from config — and if you
      didn't write them down somewhere first, you've just lost them.
- [ ] Confirm you have an Azure subscription + permission to create Key Vault,
      Managed Identity, and the resources matching your chosen target
      (App Service / ACA / AKS).

### Environment

- [ ] GHCP App Mod installed in your editor of choice
      ([install guide](https://learn.microsoft.com/en-us/dotnet/azure/migration/appmod/install)).
- [ ] You can invoke `@modernize-dotnet` in chat (Visual Studio, VS Code, or
      Copilot CLI).
- [ ] If using GitHub.com coding agent: branch protection on `main` is set so
      the agent has to PR, not push.

---

## 2. Choose the Right Azure Target — Decision Tree

```
                         ┌────────────────────┐
                         │  Start             │
                         └─────────┬──────────┘
                                   │
                                   ▼
              Is the workload long-running and stateless?
              (worker, queue processor, scheduled job)
                         ┌─────────┴─────────┐
                       YES                   NO
                         │                    │
                         ▼                    ▼
              ┌────────────────────┐    Is it a web app
              │  Worker Service    │    or HTTP API?
              │  pattern           │           │
              └─────────┬──────────┘           │
                        ▼                      ▼
              Containerized + scale-to-zero?  Does it need WebForms,
              ┌─────────┬─────────┐           WCF, COM, EventLog,
            YES                  NO           or other Windows-only API?
              │                    │           ┌──────┴──────┐
              ▼                    ▼          YES            NO
        🟢 ACA            🟢 AKS.Linux         │              │
                                              ▼              ▼
                                  🟢 AppService.Windows  Does it need
                                     or AKS.Windows      sidecars / custom
                                     (lift)              networking / GPU?
                                                          ┌────┴────┐
                                                        YES         NO
                                                         │           │
                                                         ▼           ▼
                                                   🟢 AKS.Linux  🟢 AppService.Linux
```

| Target | Pick when | Avoid when |
|--------|-----------|------------|
| `AppService.Linux` | Modern HTTP web app/API; want managed PaaS, easy custom domains + auth. | Need Windows-only deps (WCF, WebForms, EventLog, COM); long-running background jobs (use ACA). |
| `AppService.Windows` | Lift WebForms / WCF you're not ready to rewrite. | Long-term home for new code — Microsoft direction is Linux. |
| `ACA` | Containerized worker, queue processor, microservice; want scale-to-zero. | Long-term sticky session web apps — App Service is simpler. |
| `AKS.Linux` | Need full Kubernetes (operators, sidecars, multi-cluster). | Smaller teams without K8s ops experience — start with ACA. |
| `AKS.Windows` | Must keep Windows-only dependencies and want full Kubernetes. | Cost-sensitive; Windows nodes are expensive. |
| `AppService.Container.*` | You already containerize and want App Service's PaaS UX. | Need scale-to-zero (use ACA). |

> 💡 **First-run tip:** If you're unsure, set `target: "Any"` in
> `assessment-config.json`, run the assessment once, then switch targets in
> the dashboard to compare — *then* commit to a target.

---

## 3. Branch Strategy

The agent writes assessment artifacts and remediation plans to
`.github/upgrades/{scenarioId}/` in your repo. Treat that folder like
generated code: it should land via PR, not push.

Recommended layout:

```
main
└── modernize/wave-1                ← long-lived "wave" branch
    ├── modernize/sdk-style         ← one branch per scenario
    ├── modernize/key-vault
    ├── modernize/managed-identity-sql
    ├── modernize/opentelemetry
    └── modernize/dotnet8-upgrade
```

Rules:

1. **One scenario per branch.** Each predefined task should land as its own
   PR with its own review. Don't bundle "Key Vault + Managed Identity +
   OpenTelemetry" into one giant PR — you can't bisect a regression.
2. **Never run the agent on `main`.** The agent prompts to create a branch;
   accept it.
3. **Re-baseline between scenarios.** After each PR merges to `wave-1`,
   delete the scenario-specific `.github/upgrades/{scenarioId}/` folder so
   the next scenario starts clean.
4. **Squash-merge.** The agent's working commits aren't useful history —
   squash to one commit per scenario.

---

## 4. Team Workflow

The single biggest predictor of a successful App Mod run is *who is in the
loop and when*.

```
┌───────────────────────────────────────────────────────────────┐
│  1. Engineer runs assessment with target: "Any"               │
│     Output: .appmod/.appcat/assessment-report.json            │
└─────────────────────┬─────────────────────────────────────────┘
                      ▼
┌───────────────────────────────────────────────────────────────┐
│  2. Engineer + tech lead review the report                    │
│     - Are the Mandatory items real?                           │
│     - Which target are we picking and why?                    │
│     - Which findings are out-of-scope for this wave?          │
└─────────────────────┬─────────────────────────────────────────┘
                      ▼
┌───────────────────────────────────────────────────────────────┐
│  3. Engineer locks the target in assessment-config.json       │
│     Re-runs the assessment for confirmation                   │
└─────────────────────┬─────────────────────────────────────────┘
                      ▼
┌───────────────────────────────────────────────────────────────┐
│  4. Engineer + product owner agree on remediation order       │
│     (use the universal ordering — see top-level README)       │
└─────────────────────┬─────────────────────────────────────────┘
                      ▼
┌───────────────────────────────────────────────────────────────┐
│  5. For each scenario:                                        │
│     a. Branch  →  b. Agent applies  →  c. Local build + test  │
│     d. PR  →  e. Code review  →  f. Squash-merge to wave      │
└─────────────────────┬─────────────────────────────────────────┘
                      ▼
┌───────────────────────────────────────────────────────────────┐
│  6. After last scenario: integration test + deploy to staging │
│     Then PR wave → main                                       │
└───────────────────────────────────────────────────────────────┘
```

Anti-patterns to avoid:

- ❌ Engineer runs the agent end-to-end alone and surprises the team with
  a 2,000-line PR.
- ❌ Picking a target *after* the first remediation — wastes work.
- ❌ Running the next scenario before the previous one's PR is merged —
  conflicts compound.
- ❌ Skipping the review of the Mandatory list — false positives waste cycles.

---

## 5. Definition of Done — Per Scenario

A scenario is done when **all** of these are true:

- [ ] The agent's PR has been reviewed by at least one human.
- [ ] The repo builds cleanly (`dotnet build` or equivalent) on the new
      target framework.
- [ ] All existing tests still pass. New tests have been added for any
      net-new behaviour.
- [ ] The scenario's `.github/upgrades/{scenarioId}/` markdown artifacts have
      been read end-to-end by the reviewer. They contain rationale that
      future you will want.
- [ ] Secrets removed by the scenario are **rotated**, not just moved. (If
      `MerchantSecret` was committed to a public repo, putting it in Key
      Vault doesn't make it private again.)
- [ ] A staging deployment has been smoke-tested.
- [ ] The PR description references the App Mod scenario name and the
      assessment finding IDs it resolved.

---

## 6. Definition of Done — Per Wave

A wave (group of scenarios) is done when:

- [ ] The new assessment for the same code shows zero **Mandatory** items
      against your chosen target.
- [ ] The exported assessment report is committed under
      `appmod/reports/{wave-id}.json` so reviewers don't need to re-run.
- [ ] A short retrospective is written: which findings were false positives,
      which scenarios needed manual fix-ups, which predefined tasks would
      need to be re-run if applied to a sister project.

---

## 7. Common Pitfalls (preview)

- **Running ".NET version upgrade" first.** Build breaks because secrets +
  EventLog + SqlClient haven't been migrated yet. → See universal ordering
  in the top-level README.
- **Letting the agent re-run on a dirty tree.** Resulting diff is a mix of
  human + agent changes — impossible to review. → Always commit or stash.
- **Not snapshotting secrets.** Plaintext values disappear during Key Vault
  migration. → Snapshot first.
- **Picking `target: "Any"` and never narrowing.** Reports stay noisy and
  predefined tasks ask too many questions. → Lock the target after the
  first comparison.

> 📖 Full troubleshooting catalog: [`common-issues.md`](common-issues.md)
> *(coming in Batch 2)*

---

## TL;DR

```
Snapshot secrets  →  Clean branch  →  Run with target: "Any"  →
Review findings with team  →  Lock target  →  Remediate in
universal order (Key Vault → Managed Identity → OpenTelemetry →
.NET upgrade → archetype-specific)  →  One scenario per PR  →
Re-assess to prove zero Mandatory  →  Ship.
```
