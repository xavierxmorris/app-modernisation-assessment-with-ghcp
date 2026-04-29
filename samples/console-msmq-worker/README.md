# Sample 6 — Console + MSMQ Worker

> **Archetype:** .NET Framework 4.5 console app with a synchronous main loop
> reading from an MSMQ private queue and writing events to SQL Server.

## Why this archetype matters

This is the simplest "background worker" pattern, often built as a console
app + Task Scheduler / NSSM wrapper. It maps cleanly to:

- **Azure Container Apps** with a `Microsoft.Extensions.Hosting`
  `BackgroundService` and `ServiceBusProcessor` — replace MSMQ with Azure
  Service Bus and you have a scale-to-zero worker.
- **Azure Functions** (Service Bus trigger) — even simpler if the workload
  is event-driven and stateless.

Compared to Sample 3 (Windows Service), this archetype has **lower migration
cost** because there's no `ServiceBase` / SCM coupling — just `Main(...)` and
a loop. The agent's `.NET version upgrade` scenario handles the lift to
.NET 8 + `IHost` cleanly once MSMQ is replaced.

## What App Mod will surface

For the source in [`legacy-code/`](legacy-code/), an assessment with
`target: "ACA"` produces (see the [example report](example-outputs/appmod-assessment-report.md)):

- 🔴 `System.Messaging` (MSMQ) — Windows-only; replace with Azure Service Bus.
- 🔴 `System.Data.SqlClient` + `Integrated Security=True`.
- 🔴 `System.Diagnostics.EventLog`.
- 🔴 .NET Framework 4.5 — out of support.
- 🟠 Synchronous Main loop — convert to `BackgroundService.ExecuteAsync` + `CancellationToken`.
- 🟠 No retry / DLQ on poison messages.

## Run the assessment

```bash
copilot
> @modernize-dotnet
> Run an App Modernization assessment against samples/console-msmq-worker/legacy-code/.
> Use target: "ACA".
```

## Layout

```
console-msmq-worker/
├── README.md
├── legacy-code/
│   ├── Program.cs                  ← Main loop with manual shutdown
│   ├── QueueProcessor.cs           ← System.Messaging receiver + ADO.NET writer
│   ├── App.config
│   └── QueueWorker.csproj
└── example-outputs/
    └── appmod-assessment-report.md
```

> 📖 Cross-cutting guidance lives in [`../../guides/`](../../guides/).
