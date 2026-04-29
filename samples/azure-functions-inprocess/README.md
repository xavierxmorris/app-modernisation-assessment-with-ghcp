# Sample 8 — Azure Functions In-Process (v3) → Isolated Worker

> **Archetype:** Azure Functions on the **in-process** model (`Microsoft.NET.Sdk.Functions`),
> targeting `netcoreapp3.1` / Functions runtime v3, using `[FunctionName]`,
> `[ServiceBusTrigger]`, and `FunctionsStartup` for DI.

## Why this archetype matters

The in-process Functions model is **deprecated**. Functions runtime v3 reached
end of support and v4 in-process is the last in-process version. The future
is the **isolated worker model**, which gives you:

- Decoupled function host and worker — upgrade .NET independently.
- Real `Program.cs` minimal hosting model.
- DI / middleware / configuration that match every other ASP.NET Core app.
- Support for .NET 8 / 9 / 10.

The agent has a **dedicated scenario** for this migration:

> **"Azure Functions upgrade"** — Upgrades Azure Functions from in-process
> to isolated worker model.

## What App Mod will surface

For the source in [`legacy-code/`](legacy-code/), an assessment with
`target: "AppService.Linux"` (Functions Premium / Consumption are Linux by
default for .NET 8) produces (see the [example report](example-outputs/appmod-assessment-report.md)):

- 🔴 Functions runtime v3 + in-process model — must move to isolated worker.
- 🔴 `Microsoft.NET.Sdk.Functions` 3.x → `Microsoft.Azure.Functions.Worker` SDK.
- 🔴 `[FunctionName]` attribute → `[Function]` attribute.
- 🔴 `FunctionsStartup` → `Program.cs` with `HostBuilder`.
- 🔴 `System.Data.SqlClient` → `Microsoft.Data.SqlClient` + managed identity.
- 🔴 `local.settings.example.json` contains a SAS key and a SQL password — should be Key Vault references / managed identity.
- 🟠 Synchronous `cmd.ExecuteNonQuery()` inside an async function — convert to `ExecuteNonQueryAsync`.
- 🟠 Service Bus binding using a connection string — move to identity-based connection.

## Run the assessment

```bash
copilot
> @modernize-dotnet
> Run an App Modernization assessment against samples/azure-functions-inprocess/legacy-code/.
> Use target: "AppService.Linux" (Functions consumption / premium).
```

## Layout

```
azure-functions-inprocess/
├── README.md
├── legacy-code/
│   ├── OrderQueueTrigger.cs            ← [FunctionName] + [ServiceBusTrigger]
│   ├── Startup.cs                      ← FunctionsStartup-based DI
│   ├── host.json
│   ├── local.settings.example.json     ← Contains placeholder SAS key + SQL password
│   └── OrderFunctions.csproj           ← SDK-style csproj, Functions v3
└── example-outputs/
    └── appmod-assessment-report.md
```

> 📖 Cross-cutting guidance lives in [`../../guides/`](../../guides/).
