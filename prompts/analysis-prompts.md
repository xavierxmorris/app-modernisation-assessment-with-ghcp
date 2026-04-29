# GHCP App Modernization — Prompt Catalog

Proven prompts for the [GitHub Copilot App Modernization](https://learn.microsoft.com/en-us/dotnet/azure/migration/appmod/working-with-assessment)
agent (`@modernize-dotnet`). Each prompt assesses **Azure readiness** of the
sample legacy code in [`../legacy-code/`](../legacy-code/) against a specific
compute target.

> See the full walkthrough in [`../appmod/README.md`](../appmod/README.md) and
> the example output in
> [`../example-outputs/appmod-assessment-report.md`](../example-outputs/appmod-assessment-report.md).

---

## Configure the assessment target

```
@modernize-dotnet
Open .appmod/.appcat/assessment-config.json. Set the target to "AppService.Linux".
If the file does not exist yet, run a first assessment to generate it,
then update the target and re-run.
```

## Run the assessment against this sample

```
@modernize-dotnet
Run an App Modernization assessment against ../legacy-code/.
Use the target in .appmod/.appcat/assessment-config.json.
Summarize the Issue Summary table by domain and criticality before opening
the dashboard.
```

## Drill into one issue

```
@modernize-dotnet
For the "System.Data.SqlClient" finding, list every file and line where it
appears in ../legacy-code/. Explain why it blocks the AppService.Linux target
and which predefined task fixes it.
```

## Compare targets (run with `target: "Any"`)

```
@modernize-dotnet
Compare assessment results for AppService.Linux, ACA, and AKS.Windows.
Build a markdown table with one row per finding and one column per target,
showing criticality (Mandatory / Potential / Optional) for each.
Call out findings that are Mandatory on Linux but only Potential on Windows.
```

## Map findings to predefined tasks

```
@modernize-dotnet
For each Mandatory finding in the latest assessment, name the GHCP App Mod
predefined task that resolves it (e.g. "Migrate to Azure Key Vault").
Group by predefined task so we can run them in batches.
```

## Export and import a report

```
@modernize-dotnet
Export the latest assessment to appmod/reports/order-system-AppService.Linux.json.
Then show the chat command another teammate would use to import it
without re-running the assessment.
```

## Reconcile assessment with an existing modernization plan

```
@modernize-dotnet
For each Mandatory finding in the latest App Mod assessment, propose a phase
in an incremental modernization plan. Rules:
- Security findings (SQL injection, secrets in config) come first.
- Make the code testable (extract interfaces, add tests) before any behavioural change.
- Defer .NET version upgrade until after testability is in place.
```

---

## Pick the right sample (recognize your archetype)

When you point the agent at this repo and want it to identify which sample
your real codebase resembles:

```
@modernize-dotnet
Look at <my real project path> and tell me which of the samples in this
repo most closely matches the archetype:
- legacy-code/ (web order system, ASP.NET 4.5)
- samples/wcf-billing-service/
- samples/windows-service-fileprocessor/
- samples/webforms-employee-portal/
- samples/webapi2-owin-service/
- samples/console-msmq-worker/
- samples/class-library-old-deps/
- samples/azure-functions-inprocess/
- samples/wpf-desktop-app/
For each candidate, list 3 patterns from my code that match the sample and
3 that don't. Recommend which sample's example-outputs/appmod-assessment-report.md
to read first.
```

```
@modernize-dotnet
My project has multiple archetypes (e.g. a WebAPI 2 service plus a Windows
Service worker). Which samples in this repo should I read in which order?
Order by predefined-task overlap so I can run shared scenarios once and
reuse the result.
```

---

## Diagnose a failed assessment

Aligned with [`guides/common-issues.md`](../guides/common-issues.md). Use
when the assessment behaves unexpectedly.

### "The dashboard is empty / no projects detected"

```
@modernize-dotnet
The assessment dashboard shows zero projects but the repo has 5 csproj files.
- git ls-files '*.csproj' output: <paste>
- dotnet build output: <paste>
- .appmod/.appcat/log.txt last 20 lines: <paste>
What is preventing project discovery?
```

### "Target in assessment-config.json is ignored"

```
@modernize-dotnet
I set target: "AppService.Linux" in .appmod/.appcat/assessment-config.json
but the dashboard still shows "Any". I have re-run the assessment.
Show me the cached state and the steps to force a fresh run. Confirm the
target the agent is actually using.
```

### "Predefined task fails because precondition not met"

```
@modernize-dotnet
I tried "Migrate to Managed Identity based Database on Azure" and it errored
with <error>. Walk me through the prerequisite scenarios — which task needs
to run first, and how do I tell whether it has already run successfully?
```

### "Build broke after the agent applied a task"

```
@modernize-dotnet
After running <task name>, dotnet build reports:
  <exact compiler error with file + line>
Show me the offending lines in <file>. Propose the smallest fix that gets
the build green again. Don't re-run the task — I want a manual patch.
```

### "Context budget exhausted on large solution"

```
@modernize-dotnet
The solution has 47 projects. The full-solution assessment hit the context
limit and returned a partial report. Suggest a per-layer assessment plan:
which projects should I assess together, and in what order? Output a CSV
with columns: batch, projects, projected runtime.
```

### "False-positive Mandatory item"

```
@modernize-dotnet
The assessment flags <finding> in <file>:<line> as Mandatory. The code path
is dead — it lives behind a feature flag set to false in production. How do
I annotate this finding as a triaged false positive so future assessments
group it the same way? Should I exclude the file in assessment-config.json
or handle it inline?
```

---

## Run a predefined task safely

Wrap every remediation with snapshot + branch + test + review.

### Pre-task snapshot

```
@modernize-dotnet
Before I run "<predefined task name>", capture a snapshot:
- List every secret in Web.config / App.config / appsettings.json / local.settings.json
  (key name only, not value).
- Identify every file the task is likely to touch.
- Identify every consumer of those files inside the repo.
- Output as a checklist I can paste into the PR description.
```

### Bracketed run

```
@modernize-dotnet
Run "<predefined task name>" with these constraints:
1. Create branch modernize/<task-id> from current HEAD.
2. Do not touch files outside the listed scope: <files>.
3. Stop after applying the diff — do NOT auto-rebuild or auto-commit.
4. Output a summary of: files changed, new package references, new env vars
   the runtime will need, removed config keys.
```

### Post-task verification

```
@modernize-dotnet
The task <name> finished. Verify:
1. dotnet build is green (run it).
2. dotnet test --filter "FullyQualifiedName~Smoke" is green.
3. No plaintext secret survived in source-controlled files.
4. The .github/upgrades/<task-id>/ folder contains assessment.md, plan.md,
   and validation.md.
Report any check that failed with the exact error.
```

### After the task — re-assess

```
@modernize-dotnet
Re-run the App Mod assessment with the same target. Compare against the
previous report:
- Resolved Mandatory items (expected).
- New Mandatory items (unexpected — explain why each appeared).
- Unchanged items (these are next wave's work).
```

---

## Tips for Best Results

| Tip | Why |
|-----|-----|
| Lock the target in `assessment-config.json` before re-running | Otherwise you compare against `Any` and miss target-specific Mandatory items |
| Run the full assessment once, then ask drill-down questions | Keeps the conversation grounded in the dashboard's real findings |
| Ask for tables when comparing targets | Easier to scan than paragraphs |
| Always export the report before deleting `.appmod/` | The exported report can be re-imported by anyone, no re-assessment needed |
| Pair each Mandatory finding with its predefined task name | Lets you run remediations in batches instead of one issue at a time |
