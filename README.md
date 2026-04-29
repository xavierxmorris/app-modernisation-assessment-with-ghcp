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

## Most Common Findings (Across Every Enterprise Assessment)

Regardless of archetype — WCF service, Windows Service, WebForms, WebAPI 2,
Functions in-process, or "just" an ASP.NET app — these **six findings** appear
in almost every real enterprise assessment. If you're new to GHCP App Mod,
expect to see all six in your first run, and prioritize them in this order:

| # | Finding | Why universal | Predefined task that fixes it |
|---|---------|---------------|------------------------------|
| 1 | **`System.Data.SqlClient`** in code, `providerName="System.Data.SqlClient"` in config | Every .NET Framework app touching SQL Server has this. Mandatory for managed-identity auth on Azure SQL. | **"Migrate to Managed Identity based Database on Azure"** |
| 2 | **Plaintext secrets in `Web.config` / `App.config`** (`<add key="ApiSecret" value="..." />`, connection-string passwords) | Standard practice pre-2019. Mandatory for every Azure target. | **"Migrate to secured credentials by using Managed Identity and Azure Key Vault"** |
| 3 | **`Integrated Security=True` SQL connection strings** | Domain-joined SQL is the enterprise default. Won't reach Azure SQL from Linux compute. | **"Migrate to Managed Identity based Database on Azure"** (same task as #1) |
| 4 | **`System.Diagnostics.EventLog.WriteEntry(...)`** | Universal Windows logging pattern. Windows-only API; unavailable on `AppService.Linux`, `ACA`, `AKS.Linux`. | **"Migrate to OpenTelemetry on Azure"** |
| 5 | **.NET Framework 4.5 / 4.6 / 4.7 target** in `<TargetFrameworkVersion>` | EOL or near-EOL. Required upgrade for any modern Azure compute. | Scenario — **".NET version upgrade"** (target .NET 8 or later) |
| 6 | **`ConfigurationManager.AppSettings[...]` / `ConfigurationManager.ConnectionStrings[...]`** | Replaces by `IConfiguration` + `IOptions<T>` — needed for Key Vault and App Configuration integration. | Manual refactor; usually bundled into the **".NET version upgrade"** scenario |

### Universal predefined-task ordering

Run remediations in this order — each unblocks the next:

```
┌──────────────────────────────────────────┐
│ 0.  Fix any SQL injection (live risk)    │
└────────────────┬─────────────────────────┘
                 ▼
┌──────────────────────────────────────────┐
│ 1.  Migrate secrets → Key Vault          │   ← #2 above
└────────────────┬─────────────────────────┘
                 ▼
┌──────────────────────────────────────────┐
│ 2.  SqlClient + connection → Managed ID  │   ← #1 + #3 above
└────────────────┬─────────────────────────┘
                 ▼
┌──────────────────────────────────────────┐
│ 3.  EventLog → OpenTelemetry             │   ← #4 above
└────────────────┬─────────────────────────┘
                 ▼
┌──────────────────────────────────────────┐
│ 4.  .NET version upgrade                 │   ← #5 + #6 above
└────────────────┬─────────────────────────┘
                 ▼
┌──────────────────────────────────────────┐
│ 5.  Archetype-specific work              │   ← see per-sample reports
│     (WCF rewrite, MSMQ→Service Bus,      │
│      WebForms→Blazor, etc.)              │
└──────────────────────────────────────────┘
```

> 💡 **Why this order matters:** Running ".NET version upgrade" *first* is the
> most common mistake — the build breaks because secrets, EventLog, and
> SqlClient all need fixing before the new TFM compiles cleanly. Do them in
> the order above and each step lands cleanly on `main`.

### Where each common finding shows up in this repo

| Finding | Demonstrated in |
|---------|-----------------|
| `System.Data.SqlClient` | Samples [1](example-outputs/appmod-assessment-report.md), [2](samples/wcf-billing-service/example-outputs/appmod-assessment-report.md), [4](samples/webforms-employee-portal/example-outputs/appmod-assessment-report.md) |
| Plaintext secrets in config | Samples [1](example-outputs/appmod-assessment-report.md) (`MerchantSecret`), [2](samples/wcf-billing-service/example-outputs/appmod-assessment-report.md) (`BillingApiKey`) |
| `Integrated Security=True` | Samples [1](example-outputs/appmod-assessment-report.md), [2](samples/wcf-billing-service/example-outputs/appmod-assessment-report.md), [4](samples/webforms-employee-portal/example-outputs/appmod-assessment-report.md) |
| `System.Diagnostics.EventLog` | Samples [1](example-outputs/appmod-assessment-report.md), [2](samples/wcf-billing-service/example-outputs/appmod-assessment-report.md), [3](samples/windows-service-fileprocessor/example-outputs/appmod-assessment-report.md), [4](samples/webforms-employee-portal/example-outputs/appmod-assessment-report.md) |
| .NET Framework 4.5 target | All samples |
| `ConfigurationManager` | All samples |

### Less universal but still very common

These show up in 30–60% of real enterprise assessments and are covered by
specific samples in this repo — see the [comparison matrix](#cross-sample-comparison) below:

- **`System.ServiceModel` (WCF)** — see [Sample 2](samples/wcf-billing-service/)
- **`System.Messaging` (MSMQ)** — see [Sample 3](samples/windows-service-fileprocessor/)
- **`ServiceBase` Windows Service hosting** — see [Sample 3](samples/windows-service-fileprocessor/)
- **ASP.NET WebForms** — see [Sample 4](samples/webforms-employee-portal/)
- **`HttpWebRequest`** — see [Sample 1](example-outputs/appmod-assessment-report.md)
- **`SmtpClient` to internal SMTP** — see [Sample 1](example-outputs/appmod-assessment-report.md), [Sample 2](samples/wcf-billing-service/)
- **Forms authentication** — see [Sample 4](samples/webforms-employee-portal/)
- **In-proc session state** — see [Sample 4](samples/webforms-employee-portal/)

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

### 9 legacy archetypes — each with assessment input + worked report

| # | Sample | Archetype | Quick verdict |
|---|--------|-----------|--------------|
| 1 | [`legacy-code/`](legacy-code/) ([report](example-outputs/appmod-assessment-report.md)) | ASP.NET 4.5 web order system | 🟠 Lift to Linux costs ~6 Mandatory items |
| 2 | [`samples/wcf-billing-service/`](samples/wcf-billing-service/) | WCF SOAP service | 🔴 Linux = rewrite (CoreWCF or REST) |
| 3 | [`samples/windows-service-fileprocessor/`](samples/windows-service-fileprocessor/) | Windows Service + MSMQ | 🟢 Clean lift to ACA Worker Service |
| 4 | [`samples/webforms-employee-portal/`](samples/webforms-employee-portal/) | ASP.NET WebForms | 🔴 Cannot run on .NET 8+ — rewrite or stay on Windows |
| 5 | [`samples/webapi2-owin-service/`](samples/webapi2-owin-service/) | WebAPI 2 + OWIN | 🟠 Mechanical rewrite to ASP.NET Core |
| 6 | [`samples/console-msmq-worker/`](samples/console-msmq-worker/) | Console + MSMQ | 🟢 Easiest path — drop into ACA |
| 7 | [`samples/class-library-old-deps/`](samples/class-library-old-deps/) | Class library with `packages.config` + AutoMapper 6 + log4net + Newtonsoft 9 | 🟠 Breaking-change release for consumers |
| 8 | [`samples/azure-functions-inprocess/`](samples/azure-functions-inprocess/) | Azure Functions in-process v3 | 🟠 Dedicated **"Azure Functions upgrade"** scenario |
| 9 | [`samples/wpf-desktop-app/`](samples/wpf-desktop-app/) | WPF desktop client | 🟢 WPF survives the .NET 8 jump |

> ⚠️ **About the Dependabot alert on this repo:** Sample 7 deliberately
> ships `Newtonsoft.Json 9.0.1` and `System.Net.Http 4.3.0` — both have
> known CVEs. They're **assessment input only**, not deployed code. The
> alert is the desired demonstration of what App Mod surfaces.

### Repo-wide configuration

| Path | Purpose |
|------|---------|
| [`appmod/README.md`](appmod/README.md) | Step-by-step App Mod assessment walkthrough (configure → run → interpret → compare → export/import) |
| [`appmod/assessment-config.Any.json`](appmod/assessment-config.Any.json) | Target: `Any` |
| [`appmod/assessment-config.AppService.Linux.json`](appmod/assessment-config.AppService.Linux.json) | Target: `AppService.Linux` |
| [`appmod/assessment-config.ACA.json`](appmod/assessment-config.ACA.json) | Target: `ACA` |
| [`appmod/assessment-config.AKS.Windows.json`](appmod/assessment-config.AKS.Windows.json) | Target: `AKS.Windows` |

### 6 cross-cutting guides

| Guide | When to read it |
|-------|-----------------|
| [`guides/setup-for-success.md`](guides/setup-for-success.md) | **Before** the first assessment. Pre-flight checklist, target decision tree, branch strategy, team workflow. |
| [`guides/common-issues.md`](guides/common-issues.md) | When the agent does something weird. 10 failure modes with symptom → cause → fix → prevention. |
| [`guides/predefined-tasks-cheatsheet.md`](guides/predefined-tasks-cheatsheet.md) | Looking up which predefined task fixes a finding. Before/after code per task. |
| [`guides/interpreting-the-dashboard.md`](guides/interpreting-the-dashboard.md) | First time looking at the assessment dashboard. |
| [`guides/appcat-cli-integration.md`](guides/appcat-cli-integration.md) | Wiring the assessment into CI, or processing a monorepo of 30+ projects. |
| [`guides/agent-feedback-loop.md`](guides/agent-feedback-loop.md) | When a predefined task breaks the build and you need to converge in 2–3 turns. |

### Prompt catalog

| Path | Purpose |
|------|---------|
| [`prompts/analysis-prompts.md`](prompts/analysis-prompts.md) | 7 sections: configure, run, drill, compare, export/import, reconcile, **pick the right sample**, **diagnose a failed assessment**, **run a predefined task safely**. |

---

## Cross-Sample Comparison Matrix

For each archetype, what each Azure target costs you. 🟢 lift-and-shift,
🟠 modernize-and-run, 🔴 rewrite required, ❌ not supported.

| # | Archetype | `AppService.Windows` | `AppService.Linux` | `ACA` | `AKS.Windows` | `AKS.Linux` | Recommended path |
|---|-----------|:-------------------:|:------------------:|:-----:|:-------------:|:-----------:|------------------|
| 1 | Web order system | 🟢 lift | 🟠 5 Mandatory | 🟠 5 Mandatory | 🟢 lift | 🟠 5 Mandatory | `AppService.Linux` after universal fixes |
| 2 | WCF service | 🟢 lift (legacy) | 🔴 rewrite (CoreWCF or REST) | 🔴 rewrite | 🟢 lift (legacy) | 🔴 rewrite | `AKS.Windows` interim, then CoreWCF rewrite |
| 3 | Windows Service + MSMQ | 🟢 lift | 🔴 ServiceBase + MSMQ | 🟢 ACA Worker + Service Bus | 🟢 lift | 🟢 ACA Worker + Service Bus | **`ACA`** with ServiceBusProcessor |
| 4 | WebForms portal | 🟢 lift | ❌ not supported | ❌ not supported | 🟢 lift | ❌ not supported | `AppService.Windows` interim, plan Blazor rewrite |
| 5 | WebAPI 2 + OWIN | 🟢 lift | 🔴 rewrite to ASP.NET Core | 🔴 rewrite | 🟢 lift | 🔴 rewrite | `AppService.Linux` after .NET upgrade scenario |
| 6 | Console + MSMQ | 🟢 lift | 🟠 needs IHost wrapper | 🟢 ACA Worker + Service Bus | 🟢 lift | 🟢 ACA Worker + Service Bus | **`ACA`** — easiest of all archetypes |
| 7 | Class library (old deps) | n/a (depends on consumer) | n/a | n/a | n/a | n/a | Manual + **"Newtonsoft.Json upgrade"** scenario |
| 8 | Functions in-process | 🟠 isolated worker on Win | 🟢 isolated worker on Linux | n/a (Functions only) | n/a | n/a | `AppService.Linux` (Functions Premium) after **"Azure Functions upgrade"** |
| 9 | WPF desktop | n/a (desktop) | n/a | n/a | n/a | n/a | .NET 8 Windows + Entra ID auth |

### How to read this matrix

- **🟢 lift** = the existing binary runs with config changes only.
- **🟠 modernize-and-run** = mechanical changes (universal findings + small refactor); same architecture.
- **🔴 rewrite** = different runtime, different framework — major rework.
- **❌ not supported** = not a viable target at all.

### Pick a sample by your scenario

| Your situation | Read first |
|----------------|-----------|
| "We have a generic ASP.NET app and want to know what Azure costs" | [Sample 1](example-outputs/appmod-assessment-report.md) |
| "We have a WCF service and the team is asking about CoreWCF vs gRPC" | [Sample 2](samples/wcf-billing-service/) |
| "We have Windows Services and our customers asked us to leave Windows VMs" | [Sample 3](samples/windows-service-fileprocessor/) |
| "We have WebForms and don't know if we should lift or rewrite" | [Sample 4](samples/webforms-employee-portal/) |
| "We have WebAPI 2 services and want to ASP.NET Core them" | [Sample 5](samples/webapi2-owin-service/) |
| "We have small console workers reading from MSMQ" | [Sample 6](samples/console-msmq-worker/) |
| "We have a class library with old NuGet packages and consumers won't upgrade" | [Sample 7](samples/class-library-old-deps/) |
| "We have Azure Functions on the in-process model and need to move to isolated worker" | [Sample 8](samples/azure-functions-inprocess/) |
| "We have a WPF desktop app on .NET Framework 4.5" | [Sample 9](samples/wpf-desktop-app/) |
| "I'm new to GHCP App Mod and want to set up properly" | [`guides/setup-for-success.md`](guides/setup-for-success.md) |
| "The agent is misbehaving" | [`guides/common-issues.md`](guides/common-issues.md) |

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
