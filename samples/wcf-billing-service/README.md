# Sample 2 — WCF Billing Service

> **Archetype:** WCF (Windows Communication Foundation) SOAP service hosted in
> IIS, with `basicHttpBinding`, MEX metadata, and `<system.serviceModel>` config.

## Why this archetype matters

WCF services are one of the **top-three legacy migration blockers** enterprises
hit. The runtime is .NET Framework only — `System.ServiceModel` is **not**
available on .NET 8 / Linux compute. The result is consistent across every
modern Azure target:

- 🔴 `AppService.Linux` — **rewrite required**.
- 🔴 `ACA` — **rewrite required**.
- 🟠 `AKS.Windows` — lift possible, but you inherit Windows-node pricing and a
  dead-end runtime.

The agent typically suggests one of two paths:

1. **CoreWCF** — port to .NET 8 with the [CoreWCF](https://github.com/CoreWCF/CoreWCF) project (lowest contract change).
2. **gRPC or ASP.NET Core minimal API** — clean break, modern wire format.

## What App Mod will surface

For the source in [`legacy-code/`](legacy-code/), an assessment with
`target: "Any"` produces (see the [example report](example-outputs/appmod-assessment-report.md)):

- 🔴 `System.ServiceModel` (`BillingService.svc.cs:7`, `IBillingService.cs:2`) — not supported on Linux.
- 🔴 `System.Data.SqlClient` (3 incidents) — replace with `Microsoft.Data.SqlClient` + managed identity.
- 🔴 SQL injection in `CreateInvoice` (`BillingService.svc.cs:24`) — currency string concatenated into `INSERT`.
- 🔴 `BillingApiKey` plaintext secret in `Web.config:11` — Azure Key Vault.
- 🔴 `Integrated Security=True` SQL connection (`Web.config:6`) — managed identity.
- 🔴 `EventLog.WriteEntry` (2 incidents) — replace with `ILogger` + Application Insights.
- 🔴 `SmtpClient` to internal host on port 25 — Azure Communication Service email.

## Run the assessment

```bash
copilot
> @modernize-dotnet
> Run an App Modernization assessment against samples/wcf-billing-service/legacy-code/.
> Use the target in .appmod/.appcat/assessment-config.json.
```

To compare targets, copy the `Any` config first:

```bash
mkdir -p .appmod/.appcat
cp ../../appmod/assessment-config.Any.json .appmod/.appcat/assessment-config.json
```

## Layout

```
wcf-billing-service/
├── README.md                                  ← This file
├── legacy-code/
│   ├── IBillingService.cs                     ← Service contract + DataContract
│   ├── BillingService.svc.cs                  ← Implementation, ADO.NET, SmtpClient, EventLog
│   ├── Web.config                             ← <system.serviceModel> + secrets
│   └── BillingService.csproj                  ← Legacy non-SDK csproj (assessment input only)
└── example-outputs/
    └── appmod-assessment-report.md            ← Worked assessment report
```

> 📖 Cross-cutting guidance lives in [`../../guides/`](../../guides/).
