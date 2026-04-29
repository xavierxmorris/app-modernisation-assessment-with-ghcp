# Predefined Tasks Cheatsheet — GHCP App Modernization

> A practitioner's lookup table mapping common Mandatory findings to the
> GHCP App Modernization predefined task that fixes them, with before/after
> code, preconditions, and the artifacts each task produces under
> `.github/upgrades/{scenarioId}/`.

---

## How to use this cheatsheet

1. Look up your finding in the **finding → task** table.
2. Confirm the **preconditions** are met (most failures come from skipping
   these).
3. Trigger the task with the prompt template provided.
4. Review the **before/after** to know what to expect in the diff.
5. Verify the **artifacts** under `.github/upgrades/{scenarioId}/`.

---

## Finding → Task Map

| Finding (where you'll see it) | Predefined task | Sample in this repo |
|--------------------------------|-----------------|--------------------|
| Plaintext secret in `Web.config` / `App.config` / `local.settings.json` | **Migrate to secured credentials by using Managed Identity and Azure Key Vault** | [Sample 1](../example-outputs/appmod-assessment-report.md), [2](../samples/wcf-billing-service/), [5](../samples/webapi2-owin-service/), [8](../samples/azure-functions-inprocess/) |
| `System.Data.SqlClient` + `Integrated Security=True` SQL connection | **Migrate to Managed Identity based Database on Azure** | All web/data samples |
| `System.Diagnostics.EventLog`, `log4net`, `Trace.WriteLine` | **Migrate to OpenTelemetry on Azure** | Samples [1](../example-outputs/appmod-assessment-report.md), [2](../samples/wcf-billing-service/), [3](../samples/windows-service-fileprocessor/), [4](../samples/webforms-employee-portal/) |
| `System.Messaging` (MSMQ), Amazon SQS, RabbitMQ | **Migrate to Azure Service Bus** | Samples [3](../samples/windows-service-fileprocessor/), [6](../samples/console-msmq-worker/) |
| `System.Net.Mail.SmtpClient` to internal SMTP | **Migrate to Azure Communication Service email** | Samples [1](../example-outputs/appmod-assessment-report.md), [2](../samples/wcf-billing-service/) |
| Local file I/O (`File.Read*`, `Directory.*`) on absolute Windows paths | **Migrate to Azure Blob Storage** (or **Azure File Storage** if a real filesystem is required) | Sample [3](../samples/windows-service-fileprocessor/) |
| Forms / Windows AD authentication | **Migrate to Microsoft Entra ID** | Sample [4](../samples/webforms-employee-portal/) |
| In-proc / sticky session state, local `MemoryCache` | **Migrate to Azure Cache for Redis by using Managed Identity** | Sample [4](../samples/webforms-employee-portal/) |
| Azure Functions in-process model | **Azure Functions upgrade** (in-process → isolated worker) | Sample [8](../samples/azure-functions-inprocess/) |
| `Newtonsoft.Json` references everywhere | Scenario — **Newtonsoft.Json upgrade** | Sample 7 *(Batch 3)* |
| `System.Data.SqlClient` package reference (without managed identity intent) | Scenario — **SqlClient upgrade** | All web/data samples |
| Legacy non-SDK `csproj` | Scenario — **SDK-style conversion** | Most samples |
| .NET Framework / .NET Core 1.x–3.x / .NET 5+ TFM | Scenario — **.NET version upgrade** | All samples |
| Apache Kafka / Confluent on-prem | **Migrate to Confluent Cloud / Azure Event Hub for Apache Kafka** | (not in repo — pattern reference only) |

---

## Task: Migrate to secured credentials by using Managed Identity and Azure Key Vault

### When to run it

Run this **first** — almost every other task assumes plaintext secrets are
out of source-controlled config.

### Preconditions

- You have an Azure subscription and permission to create a Key Vault and
  assign `Key Vault Secrets User` role.
- You have **snapshotted every secret** somewhere safe (private vault, 1Password
  vault, etc.) — they're about to disappear from the file.
- The app uses `IConfiguration` (post-modernization) or `ConfigurationManager`
  (pre-modernization). Both work, but `ConfigurationManager` produces a
  rougher diff.

### Prompt template

```
@modernize-dotnet
Run the predefined task "Migrate to secured credentials by using Managed Identity
and Azure Key Vault" for this project. Use Key Vault name "<my-vault>".
Snapshot of secrets is captured externally — please remove plaintext values.
```

### Before / after

```xml
<!-- BEFORE: Web.config -->
<appSettings>
  <add key="ApiSecret" value="sk_live_real_value_here" />
</appSettings>
```

```csharp
// AFTER: Program.cs (.NET 8+)
builder.Configuration.AddAzureKeyVault(
    new Uri("https://<my-vault>.vault.azure.net/"),
    new DefaultAzureCredential());

// Code reads via IConfiguration as if it were any other setting
var apiSecret = builder.Configuration["ApiSecret"];
```

```xml
<!-- AFTER: Web.config -->
<appSettings>
  <!-- ApiSecret moved to Azure Key Vault -->
</appSettings>
```

### Artifacts produced

Under `.github/upgrades/migrate-to-keyvault/`:

- `assessment.md` — list of secrets the task identified.
- `plan.md` — proposed Key Vault structure (per-environment vault?).
- `validation.md` — post-run checks (Key Vault accessible? RBAC granted?).

### Common mistakes

- ❌ Forgetting to **rotate** snapshotted secrets — moving a leaked secret
  to Key Vault doesn't un-leak it.
- ❌ Using a single Key Vault across dev/staging/prod — the task supports
  per-environment, use it.
- ❌ Not granting `Key Vault Secrets User` to the deployment identity
  *before* the first deploy — runtime fails to bootstrap.

---

## Task: Migrate to Managed Identity based Database on Azure

### When to run it

After the Key Vault task. This task replaces both `System.Data.SqlClient`
**and** the `Integrated Security=True` / username+password connection
strings in one pass.

### Preconditions

- The Azure SQL server has **Microsoft Entra authentication enabled**.
- The deployment identity is assigned the `db_datareader` / `db_datawriter`
  / `db_owner` roles in the target database.
- Key Vault task has run (or there are no plaintext SQL passwords in
  config).

### Prompt template

```
@modernize-dotnet
Run the predefined task "Migrate to Managed Identity based Database on Azure"
for this project. Target server: contoso-sql.database.windows.net,
database: ContosoOrders.
```

### Before / after

```xml
<!-- BEFORE -->
<add name="OrderDB"
     connectionString="Server=SQLPROD01;Database=ContosoOrders;Integrated Security=True;"
     providerName="System.Data.SqlClient" />
```

```json
// AFTER (in appsettings.json)
{
  "ConnectionStrings": {
    "OrderDB": "Server=tcp:contoso-sql.database.windows.net,1433;Database=ContosoOrders;Authentication=Active Directory Managed Identity;Encrypt=True;"
  }
}
```

```csharp
// AFTER (in code) — package change
// - using System.Data.SqlClient;
// + using Microsoft.Data.SqlClient;
```

```xml
<!-- AFTER (in csproj) -->
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.x.x" />
```

### Artifacts produced

Under `.github/upgrades/managed-identity-sql/`:

- `assessment.md` — every `SqlConnection` / `SqlCommand` instance touched.
- `plan.md` — connection string template + RBAC assignments needed.
- `cve-report.md` — any known CVEs in the old `System.Data.SqlClient`
  version.

### Common mistakes

- ❌ Running before Key Vault — the agent has to leave the password in
  config, reducing the diff quality.
- ❌ Forgetting to grant the managed identity database role — runtime
  fails with `Login failed for user '<token-identified principal>'`.
- ❌ Forgetting `Encrypt=True` — Azure SQL requires encryption; old
  on-prem connections often had `Encrypt=False`.

---

## Task: Migrate to OpenTelemetry on Azure

### When to run it

After Key Vault + Managed Identity SQL. This task touches every logging
call site, so doing it after the security work means a smaller diff per PR.

### Preconditions

- Application Insights resource exists (or accept the agent's offer to
  create one).
- Connection string for Application Insights is in Key Vault (per the
  earlier task).

### Prompt template

```
@modernize-dotnet
Run the predefined task "Migrate to OpenTelemetry on Azure" for this project.
Replace System.Diagnostics.EventLog and any log4net usage with ILogger.
Configure the Application Insights exporter via OTEL_EXPORTER_OTLP_ENDPOINT.
```

### Before / after

```csharp
// BEFORE
EventLog.WriteEntry("OrderSystem", "Processed order " + id,
    EventLogEntryType.Information);

// AFTER
_logger.LogInformation("Processed order {OrderId}", id);
```

```csharp
// AFTER (Program.cs additions)
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(options =>
    {
        options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    });
```

### Artifacts produced

Under `.github/upgrades/migrate-to-opentelemetry/`:

- `assessment.md` — every `EventLog` / `log4net` / `Trace.*` call site.
- `plan.md` — proposed `ILogger` injection map.
- `categories.md` — proposed log categories per class.

### Common mistakes

- ❌ Keeping log4net side-by-side with the new `ILogger` — pick one,
  delete the other.
- ❌ Logging `EventLog.WriteEntry("Source", ...)` when running on Linux —
  the assessment will surface this even if you've already added `ILogger`,
  because both code paths exist.

---

## Task: Migrate to Azure Service Bus

### Preconditions

- Service Bus namespace exists.
- The deployment identity has the **Azure Service Bus Data Sender** /
  **Receiver** role.

### Prompt template

```
@modernize-dotnet
Run the predefined task "Migrate to Azure Service Bus" for this project.
Replace System.Messaging (MSMQ). Namespace: contoso-orders.servicebus.windows.net.
Queue: order-events.
```

### Before / after

```csharp
// BEFORE
var queue = new MessageQueue(@".\private$\order-events");
queue.Formatter = new XmlMessageFormatter(new[] { typeof(OrderEvent) });
var msg = queue.Receive(TimeSpan.FromSeconds(5));
var evt = (OrderEvent)msg.Body;

// AFTER
var processor = client.CreateProcessor("order-events");
processor.ProcessMessageAsync += async args =>
{
    var evt = JsonSerializer.Deserialize<OrderEvent>(args.Message.Body);
    // ... handle ...
    await args.CompleteMessageAsync(args.Message);
};
await processor.StartProcessingAsync();
```

### Common mistakes

- ❌ Trying to keep `XmlMessageFormatter` semantics — Service Bus uses byte
  payloads. Pick `System.Text.Json` and a versioned schema.
- ❌ Not setting `MaxDeliveryCount` on the queue — poison messages loop
  forever. Configure DLQ before going live.

---

## Task: Migrate to Azure Communication Service email

### Preconditions

- Azure Communication Services resource exists with an Email subdomain.
- Sender domain verified.

### Before / after

```csharp
// BEFORE
using (var client = new SmtpClient("smtp.contoso.internal", 25))
{
    client.Send(new MailMessage("from@contoso.com", "to@example.com",
        "Subject", "Body"));
}

// AFTER
var emailClient = new EmailClient(builder.Configuration["AcsConnectionString"]);
await emailClient.SendAsync(WaitUntil.Started, new EmailMessage(
    senderAddress: "DoNotReply@contoso.azurecomm.net",
    recipientAddress: "to@example.com",
    content: new EmailContent("Subject") { PlainText = "Body" }));
```

---

## Task: Migrate to Azure Cache for Redis by using Managed Identity

### Preconditions

- Azure Cache for Redis with **Microsoft Entra authentication** enabled.

### Common mistakes

- ❌ Using the legacy connection string with `accesskey=` — Entra-auth
  Redis uses `connectionString=...,User=<oid>,Password=<token>` shape.
  Let the task generate it.

---

## Scenario: .NET version upgrade

This isn't a "predefined task" but a full **scenario**. It does the .NET
version bump (e.g. .NET Framework 4.5 → .NET 8) and a *lot* of
mechanical refactoring along the way.

### Preconditions (in this order)

1. Key Vault task complete.
2. Managed Identity SQL task complete.
3. OpenTelemetry task complete.
4. Repo on a clean branch.
5. Build is green.

If any precondition is missing, the upgrade scenario will surface dozens
of build errors that should have been pre-resolved.

### What it touches

- `<TargetFrameworkVersion>v4.5</TargetFrameworkVersion>` → `<TargetFramework>net8.0</TargetFramework>`
- `csproj` → SDK-style.
- `packages.config` → `PackageReference`.
- `Web.config` → `appsettings.json` (web apps).
- `Global.asax.cs` → `Program.cs`.
- `System.Web.HttpContext` → `IHttpContextAccessor`.
- WebAPI 2 controllers → ASP.NET Core controllers.
- WCF service contracts → CoreWCF (if you opt in) or REST/gRPC suggestion.
- Async-ifies sync APIs where mechanical (`ExecuteNonQuery` →
  `ExecuteNonQueryAsync`).

### Common mistakes

- ❌ Running this scenario *first*. See [common-issues.md #3](common-issues.md#3-predefined-task-fails-because-of-a-missing-precondition).
- ❌ Skipping the **SDK-style conversion** scenario beforehand on a legacy
  csproj — leaves the project in a half-converted state.
- ❌ Trying to run on a multi-targeting project (`net48;netstandard2.0`).
  Split first.

---

## Quick reference — preconditions chain

```
SDK-style conversion (if needed)
        ↓
Migrate to Key Vault                  ── secrets out of config
        ↓
Migrate to Managed Identity DB        ── SqlClient + connection string
        ↓
Migrate to OpenTelemetry              ── EventLog / log4net out
        ↓
Migrate to Azure Service Bus          ── MSMQ → Service Bus
(if applicable)
        ↓
Migrate to Azure Comm Service email   ── SmtpClient → ACS Email
(if applicable)
        ↓
Migrate to Azure Blob Storage         ── local file I/O → Blob
(if applicable)
        ↓
Azure Functions upgrade               ── in-process → isolated worker
(if applicable)
        ↓
.NET version upgrade                  ── final TFM bump + mechanical refactors
        ↓
Re-assess                             ── prove zero Mandatory
```

> 💡 If you skip a step that *was* applicable, the next step will produce
> a noisy diff or fail outright. The order is dependency, not preference.
