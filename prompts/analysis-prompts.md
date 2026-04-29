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

## Tips for Best Results

| Tip | Why |
|-----|-----|
| Lock the target in `assessment-config.json` before re-running | Otherwise you compare against `Any` and miss target-specific Mandatory items |
| Run the full assessment once, then ask drill-down questions | Keeps the conversation grounded in the dashboard's real findings |
| Ask for tables when comparing targets | Easier to scan than paragraphs |
| Always export the report before deleting `.appmod/` | The exported report can be re-imported by anyone, no re-assessment needed |
| Pair each Mandatory finding with its predefined task name | Lets you run remediations in batches instead of one issue at a time |
