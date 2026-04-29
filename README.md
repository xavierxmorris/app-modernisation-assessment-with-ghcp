# App Modernisation Assessment with GHCP

> **GitHub Copilot App Modernization (GHCP App Mod) — assess legacy .NET code
> for Azure readiness, target-by-target, before you migrate.**

This repo demonstrates the [App Modernization assessment](https://learn.microsoft.com/en-us/dotnet/azure/migration/appmod/working-with-assessment)
workflow on a realistic legacy ASP.NET order-processing system. It includes:

- Four ready-to-use `assessment-config.json` files (one per common Azure target)
- A worked example assessment report comparing **App Service Linux**, **ACA**, and **AKS Windows**
- A prompt catalog for the `@modernize-dotnet` agent
- The legacy .NET source files used as assessment input

> 🎯 **Focus:** This repo is **only** about App Modernization assessment.
> If you also want plain-English explanations, Mermaid flow diagrams, and an
> incremental refactor plan for the same code, see the parent demo:
> [CFS-AI-Hackathon-26 — Legacy .NET Analysis](https://github.com/xavierxmorris/CFS-AI-Hackathon-26/tree/master/samples/legacy-dotnet-analysis).

---

## Why This Exists

Enterprise teams inherit legacy .NET codebases and need to answer one question
before a migration board signs off:

> *"What does it actually take to run this on \<Azure compute target\>?"*

Running the GHCP App Mod assessment turns that question into an evidence-backed
list — Mandatory / Potential / Optional issues, with file + line references and
a recommended fix for each.

---

## The Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                  LEGACY .NET CODEBASE (legacy-code/)            │
│                                                                 │
│  OrderProcessor.cs   • CustomerRepository.cs                    │
│  PaymentService.cs   • Web.config (.NET Framework 4.5)          │
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

## Quick Start

### Prerequisites

- [GitHub Copilot App Modernization](https://learn.microsoft.com/en-us/dotnet/azure/migration/appmod/install)
  installed in **Visual Studio**, **VS Code**, or **GitHub Copilot CLI**.
- A GitHub Copilot subscription with access to the modernization agent.
- **No .NET SDK required** — assessment reads source, doesn't compile.

### Run the assessment

1. Clone this repo and `cd` into it.
2. Pick a target — copy one of the four configs into your project:

   ```bash
   # Example: assess against Azure App Service (Linux)
   mkdir -p .appmod/.appcat
   cp appmod/assessment-config.AppService.Linux.json .appmod/.appcat/assessment-config.json
   ```

3. Trigger the agent:

   ```bash
   copilot
   > @modernize-dotnet
   > Run an App Modernization assessment against legacy-code/.
   > Use the target in .appmod/.appcat/assessment-config.json.
   ```

4. Open the dashboard the agent generates. Compare findings against the
   pre-rendered example at
   [`example-outputs/appmod-assessment-report.md`](example-outputs/appmod-assessment-report.md).

> 📖 Full step-by-step walkthrough lives in [`appmod/README.md`](appmod/README.md).

---

## What's Included

| Path | Purpose |
|------|---------|
| [`appmod/README.md`](appmod/README.md) | Step-by-step App Mod assessment walkthrough (configure → run → interpret → compare → export/import) |
| [`appmod/assessment-config.Any.json`](appmod/assessment-config.Any.json) | Target: `Any` — surface issues for every supported target so you can compare |
| [`appmod/assessment-config.AppService.Linux.json`](appmod/assessment-config.AppService.Linux.json) | Target: `AppService.Linux` |
| [`appmod/assessment-config.ACA.json`](appmod/assessment-config.ACA.json) | Target: `ACA` (Azure Container Apps) |
| [`appmod/assessment-config.AKS.Windows.json`](appmod/assessment-config.AKS.Windows.json) | Target: `AKS.Windows` (Azure Kubernetes Service) |
| [`example-outputs/appmod-assessment-report.md`](example-outputs/appmod-assessment-report.md) | Worked example: Application Information + Issue Summary + per-issue detail + target comparison table |
| [`prompts/analysis-prompts.md`](prompts/analysis-prompts.md) | Prompt catalog for the `@modernize-dotnet` agent (configure, run, drill, compare, map to predefined tasks, export/import) |
| [`legacy-code/`](legacy-code/) | The .NET Framework 4.5 sample used as assessment input — 3 C# files + Web.config |

---

## Sample Findings (worked example)

For the legacy code in [`legacy-code/`](legacy-code/), the App Mod assessment
produces target-aware results — same code, very different stories:

| Target | 🔴 Mandatory | 🟠 Potential | 🟡 Optional |
|--------|:------------:|:------------:|:-----------:|
| `AppService.Linux` | 6 | 2 | 4 |
| `ACA` | 6 | 2 | 4 |
| `AKS.Windows` | 2 | 4 | 6 |

Top blockers on the Linux targets:

- 🔴 `System.Data.SqlClient` (no managed-identity support)
- 🔴 `MerchantSecret` in `Web.config` (must move to Key Vault)
- 🔴 `Integrated Security=True` SQL connection (won't reach Azure SQL from Linux)
- 🔴 `System.Diagnostics.EventLog` (Windows-only API)
- 🔴 .NET Framework 4.5 target (out of support)
- 🔴 `SmtpClient` to internal SMTP host on port 25 (port 25 is blocked on Azure)

Full breakdown:
[`example-outputs/appmod-assessment-report.md`](example-outputs/appmod-assessment-report.md).

---

## When to Use This Repo

| You want to... | Use this repo |
|----------------|:-------------:|
| Check Azure readiness against a specific compute target | ✅ |
| Compare App Service Linux vs. ACA vs. AKS for the same code | ✅ |
| Share a portable assessment report with reviewers | ✅ |
| Map every Mandatory finding to a GHCP App Mod predefined task | ✅ |
| Get plain-English explanations of legacy code | See [parent demo](https://github.com/xavierxmorris/CFS-AI-Hackathon-26/tree/master/samples/legacy-dotnet-analysis) |
| Generate Mermaid flow diagrams of legacy classes | See [parent demo](https://github.com/xavierxmorris/CFS-AI-Hackathon-26/tree/master/samples/legacy-dotnet-analysis) |
| Get an incremental refactor plan for the existing code | See [parent demo](https://github.com/xavierxmorris/CFS-AI-Hackathon-26/tree/master/samples/legacy-dotnet-analysis) |

---

## Key Principles

1. **Assessment first, remediation second** — never start "modernizing" before
   you know what the target Azure service actually requires.
2. **Run with `Any`, then commit to a target** — comparing targets in the
   dashboard prevents lock-in to the wrong compute service.
3. **Map findings to predefined tasks** — each Mandatory finding usually maps
   to one of the [predefined migration tasks](https://learn.microsoft.com/en-us/dotnet/azure/migration/appmod/overview#predefined-tasks-for-migration).
4. **Export the report** — share the evidence, don't make every reviewer
   re-run the assessment.
5. **Human still validates** — the agent surfaces patterns; the team confirms
   whether each finding actually matters for the production system.
6. **No .NET SDK required** — assessment is performed against source files.

---

## References

- [GitHub Copilot App Modernization — Overview](https://learn.microsoft.com/en-us/dotnet/azure/migration/appmod/overview)
- [Working with assessment](https://learn.microsoft.com/en-us/dotnet/azure/migration/appmod/working-with-assessment) (Microsoft Learn)
- [Predefined tasks for migration](https://learn.microsoft.com/en-us/dotnet/azure/migration/appmod/overview#predefined-tasks-for-migration)
- [Install GitHub Copilot Modernization](https://learn.microsoft.com/en-us/dotnet/azure/migration/appmod/install)
- [Parent demo — `CFS-AI-Hackathon-26`](https://github.com/xavierxmorris/CFS-AI-Hackathon-26)

---

## License

[MIT](LICENSE)
