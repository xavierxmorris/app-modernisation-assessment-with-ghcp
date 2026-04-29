# Sample 3 — Windows Service File Processor

> **Archetype:** .NET Framework 4.5 Windows Service (`ServiceBase`) that
> polls a local inbox folder for CSV files and listens to an MSMQ queue.

## Why this archetype matters

Windows Services + MSMQ are the **classic Worker workload** that maps almost
1:1 to **Azure Container Apps with a Worker Service container** or **AKS** —
once you replace MSMQ with Azure Service Bus and `EventLog` with
OpenTelemetry, the lift is mostly mechanical.

| Target | Path |
|--------|------|
| 🟢 `ACA` | Convert to `Microsoft.Extensions.Hosting` Worker Service, container, ACA. **Recommended.** |
| 🟢 `AKS.Linux` | Same Worker Service + container, AKS Linux. |
| 🟢 `AKS.Windows` | Lift the existing service to a Windows container. Keeps EventLog + MSMQ semantics; expensive long-term. |
| ❌ `AppService.Linux` / `AppService.Windows` | Not designed for long-running background services — use ACA or AKS. |

## What App Mod will surface

For the source in [`legacy-code/`](legacy-code/), an assessment with
`target: "ACA"` produces (see the [example report](example-outputs/appmod-assessment-report.md)):

- 🔴 `System.Messaging` (MSMQ) — not supported on Linux at all.
- 🔴 `System.ServiceProcess.ServiceBase` — Windows-only hosting model.
- 🔴 `System.Diagnostics.EventLog` — Windows-only.
- 🔴 .NET Framework 4.5 target — must upgrade to .NET 8+.
- 🟠 Synchronous file I/O on the timer thread — convert to `async` + `IHostedService`.
- 🟠 Local filesystem paths (`C:\Inbox\...`) — move to Azure Blob / Azure File Storage.

## Run the assessment

```bash
copilot
> @modernize-dotnet
> Run an App Modernization assessment against samples/windows-service-fileprocessor/legacy-code/.
> Use target: "ACA" so we get container-friendly recommendations.
```

## Layout

```
windows-service-fileprocessor/
├── README.md                                  ← This file
├── legacy-code/
│   ├── Program.cs                             ← ServiceBase.Run entrypoint
│   ├── FileProcessorService.cs                ← OnStart/OnStop, Timer, EventLog
│   ├── MessageQueueListener.cs                ← System.Messaging MSMQ listener
│   ├── App.config                             ← Local paths + MSMQ name
│   └── FileProcessor.csproj                   ← Legacy non-SDK csproj (assessment input only)
└── example-outputs/
    └── appmod-assessment-report.md            ← Worked assessment report
```

> 📖 Cross-cutting guidance lives in [`../../guides/`](../../guides/).
