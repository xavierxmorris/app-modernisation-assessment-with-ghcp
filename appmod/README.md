# GHCP App Modernization Assessment — Legacy .NET Sample

> **Use Case:** Run a [GitHub Copilot App Modernization](https://learn.microsoft.com/en-us/dotnet/azure/migration/appmod/overview)
> assessment on a representative legacy .NET order-processing system,
> compare Azure compute targets, and turn the resulting report into a
> concrete migration plan.

This folder contains the configuration examples and walkthrough for the
**App Modernization assessment** workflow. The legacy code under
[`../legacy-code/`](../legacy-code/) is the assessment input.

> 📘 Reference: [Working with assessment](https://learn.microsoft.com/en-us/dotnet/azure/migration/appmod/working-with-assessment)
> in the Microsoft Learn modernization guide.

---

## The Flow

```
┌─────────────────────────────────────────────────────────────────┐
│             LEGACY .NET CODEBASE (../legacy-code/)              │
│                                                                 │
│  OrderProcessor.cs  •  CustomerRepository.cs                    │
│  PaymentService.cs  •  Web.config                               │
└────────────────────────┬────────────────────────────────────────┘
                         │  pick a target Azure service
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│             .appmod/.appcat/assessment-config.json              │
│                                                                 │
│  Any  •  AppService.Linux  •  ACA  •  AKS.Windows  •  ...       │
└────────────────────────┬────────────────────────────────────────┘
                         │  run the App Mod assessment
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                  GHCP APP MODERNIZATION AGENT                   │
│                                                                 │
│  Step 1: Configure the target Azure service                     │
│  Step 2: Run the assessment                                     │
│  Step 3: Read the dashboard (App Info • Issues • Summary)       │
│  Step 4: Compare targets (Any → switch to compare)              │
│  Step 5: Export / share / import the report                     │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│             📁 ASSESSMENT REPORT + MIGRATION DECISIONS          │
│                                                                 │
│  • Mandatory issues fixed before migration                      │
│  • Potential issues triaged with the team                       │
│  • Target Azure service chosen with evidence                    │
│  • Report exported and committed for the next person            │
└─────────────────────────────────────────────────────────────────┘
```

---

## Step 1: Configure the Target Azure Service

The first time the App Mod agent runs against your project it generates
`.appmod/.appcat/assessment-config.json`. You then edit it to lock in the
target you want assessed.

This folder contains **four** ready-to-use config examples — pick one,
copy it into your project as `.appmod/.appcat/assessment-config.json`,
and re-run the assessment.

| File | Target | Use when |
|------|--------|---------|
| [`assessment-config.Any.json`](assessment-config.Any.json) | `Any` | You haven't picked a target yet — surface issues for *all* supported targets and compare. |
| [`assessment-config.AppService.Linux.json`](assessment-config.AppService.Linux.json) | `AppService.Linux` | You want the smallest hosting surface area and are happy to containerize / cross-compile. |
| [`assessment-config.ACA.json`](assessment-config.ACA.json) | `ACA` | You want serverless containers with scale-to-zero (Azure Container Apps). |
| [`assessment-config.AKS.Windows.json`](assessment-config.AKS.Windows.json) | `AKS.Windows` | You must keep Windows-only dependencies (System.Web, EventLog, COM) and want full Kubernetes control. |

> **Recommended first run:** start with `Any`. Switch the dropdown in the
> dashboard to compare targets before you commit to one.

### All supported targets (per Microsoft Learn)

| Target | Description |
|--------|-------------|
| `Any` | Issues for all supported targets — pick later in the dashboard. |
| `AKS.Windows` | Azure Kubernetes Service (Windows nodes). |
| `AKS.Linux` | Azure Kubernetes Service (Linux nodes). |
| `AppService.Windows` | Azure App Service (Windows). |
| `AppService.Linux` | Azure App Service (Linux). |
| `AppServiceContainer.Windows` | Azure App Service Container (Windows). |
| `AppServiceContainer.Linux` | Azure App Service Container (Linux). |
| `AppServiceManagedInstance.Windows` | Azure App Service Managed Instance (Windows). |
| `ACA` | Azure Container Apps. |

---

## Step 2: Run the Assessment

In Visual Studio, VS Code, or Copilot CLI, invoke the modernization agent
against this sample's `legacy-code/` folder:

```bash
copilot

> @modernize-dotnet
> Run an App Modernization assessment against ../legacy-code/.
> Use the target in .appmod/.appcat/assessment-config.json.
```

The agent generates an interactive dashboard with three sections:

- **Application Information** — projects, frameworks, build tools, target.
- **Issue Summary** — issues per domain with a criticality breakdown.
- **Issues** — the per-issue list, expandable to file + line references.

> **What you get:** a dashboard like the one captured in
> [`../example-outputs/appmod-assessment-report.md`](../example-outputs/appmod-assessment-report.md)
> for this sample.

---

## Step 3: Interpret the Report

Issues are categorized by **Domain** and **Criticality**:

| Criticality | Meaning |
|-------------|---------|
| 🔴 **Mandatory** | Must be fixed before migration. |
| 🟠 **Potential** | May impact migration — review with the team. |
| 🟡 **Optional** | Low impact — fix when convenient. |

| Domain | Meaning |
|--------|---------|
| **Cloud Readiness** | Evaluates dependencies and patterns that block or complicate Azure adoption. |

For this sample, the assessment surfaces (see the [example report](../example-outputs/appmod-assessment-report.md)
for the full list):

- 🔴 `System.Data.SqlClient` is replaced by `Microsoft.Data.SqlClient` for Azure auth scenarios.
- 🔴 `MerchantSecret` lives in `Web.config` — must move to Azure Key Vault.
- 🔴 `Integrated Security=True` connection string can't reach Azure SQL from Linux compute.
- 🔴 `EventLog` logging won't work on any Linux target — switch to OpenTelemetry / `ILogger`.
- 🔴 .NET Framework 4.5 target — out of support; must upgrade to .NET 8+.
- 🟠 `HttpWebRequest` in `PaymentService.cs` — use `HttpClient` + Polly.
- 🟠 Synchronous `SmtpClient` to an internal SMTP host on port 25 — use Azure Communication Services email.

---

## Step 4: Compare Targets

If you assessed with `Any`, the dashboard lets you switch between targets to
compare counts and recommendations. Use Copilot to summarize:

```bash
> Compare the assessment results across AppService.Linux, ACA, and AKS.Windows.
> Show issue counts per criticality and call out which legacy patterns
> become *blockers* on Linux targets but stay *Potential* on Windows targets.
```

> **What you get:** a comparison table like the one in the
> [example report](../example-outputs/appmod-assessment-report.md#target-comparison).

---

## Step 5: Export, Share, Import

The report is portable — share it instead of re-running the assessment.

- **Export** — click `Export` in the dashboard to save a portable report.
  Commit it under `appmod/reports/` so reviewers see the same data you saw.
- **Import** — in chat type `import assessment report` (or use the `Import`
  button) to load:
  - a **.NET AppCAT CLI** result,
  - a previously **exported GHCP App Mod report**, or
  - a **Dr.Migrate** app context file.

```bash
> @modernize-dotnet
> import assessment report
> Path: appmod/reports/order-system-AppService.Linux.json
```

---

## When to Use This

| You want to... | Use |
|----------------|-----|
| Check Azure readiness against a *specific* compute target | **This repo** |
| Compare App Service Linux vs. ACA vs. AKS for the same code | **This repo + `Any` config** |
| Share a portable assessment with reviewers | **This repo — Step 5 (Export)** |

---

## Project Layout

```
appmod/
├── README.md                                  ← This file
├── assessment-config.Any.json                 ← Target: Any (compare in dashboard)
├── assessment-config.AppService.Linux.json    ← Target: AppService.Linux
├── assessment-config.ACA.json                 ← Target: ACA
└── assessment-config.AKS.Windows.json         ← Target: AKS.Windows
```

Pre-generated example output lives under
[`../example-outputs/appmod-assessment-report.md`](../example-outputs/appmod-assessment-report.md).
Prompts for App Mod assessment live in
[`../prompts/analysis-prompts.md`](../prompts/analysis-prompts.md#6-modernize--ghcp-app-modernization-assessment-prompts).
