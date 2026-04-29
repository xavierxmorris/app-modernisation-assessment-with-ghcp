# Sample 9 — WPF Desktop Client

> **Archetype:** .NET Framework 4.5 WPF desktop application — `App.xaml`,
> `MainWindow.xaml`, MVVM `OrderViewModel`, `App.config` for settings.

## Why this archetype matters

WPF *is* supported on .NET 8+ — unlike WebForms, the upgrade path is
straightforward. The agent's `.NET version upgrade` scenario handles WPF
cleanly. This sample exists to show:

1. The **non-Azure** modernization path. WPF is a desktop framework; the
   "target" isn't an Azure compute service, it's a newer .NET runtime.
2. The **shared findings** that still apply: `EventLog`, plaintext API
   keys, internal-only URLs, `HttpWebRequest`, `ConfigurationManager`.
3. The unique WPF concern: **synchronous I/O on the UI thread freezes the
   window**. A modernization is a good time to fix that.

The agent treats the assessment more like a "**.NET version upgrade
readiness check**" than a cloud-readiness check. App Mod's assessment-config
target effectively reduces to "Any" (none of the App Service / ACA / AKS
targets apply to a desktop binary).

## What App Mod will surface

For the source in [`legacy-code/`](legacy-code/), an assessment produces
(see the [example report](example-outputs/appmod-assessment-report.md)):

- 🔴 .NET Framework 4.5 → .NET 8 (WPF supported on .NET 6/8/10).
- 🔴 Legacy non-SDK `csproj` — convert to SDK-style.
- 🔴 `ApiKey` plaintext in `App.config:5` — should be Azure Key Vault
  *or*, for a desktop app, Windows DPAPI / Credential Manager.
- 🔴 `EventLog.WriteEntry` — replace with `ILogger` + Application Insights
  (the desktop app *can* still send telemetry to Azure).
- 🟠 `HttpWebRequest` in `LoadOrdersSync` — `HttpClient` + `IHttpClientFactory`.
- 🟠 **Synchronous I/O on UI thread** — UI freeze. Convert to `LoadOrdersAsync` + `await`.
- 🟠 Internal-only API URL `http://internal-orders.contoso.local` — won't work for users outside the corporate network.

## Run the assessment

```bash
copilot
> @modernize-dotnet
> Run an App Modernization assessment against samples/wpf-desktop-app/legacy-code/.
> This is a desktop app — focus on .NET version upgrade readiness and call out
> non-cloud findings (plaintext secrets, sync I/O on UI thread, EventLog).
```

## Layout

```
wpf-desktop-app/
├── README.md
├── legacy-code/
│   ├── App.xaml.cs                  ← Application bootstrap
│   ├── MainWindow.xaml.cs           ← Window code-behind + button click
│   ├── OrderViewModel.cs            ← MVVM, sync HttpWebRequest, EventLog
│   ├── App.config                   ← ApiBaseUrl + ApiKey
│   └── OrderClient.csproj           ← Legacy WPF csproj (assessment input only)
└── example-outputs/
    └── appmod-assessment-report.md
```

> 📖 Cross-cutting guidance lives in [`../../guides/`](../../guides/).
