# Sample 5 — WebAPI 2 + OWIN Service

> **Archetype:** ASP.NET WebAPI 2 (`System.Web.Http`) hosted via OWIN
> (`Microsoft.Owin`) on IIS, with attribute routing, `ApiController` base
> class, and `IHttpActionResult` returns.

## Why this archetype matters

WebAPI 2 + OWIN was the standard for HTTP APIs in .NET Framework 4.5–4.8.
Migration to ASP.NET Core is a **mechanical rewrite** — the controller
shape is similar but the namespaces, base classes, hosting model, and
DI/middleware story are different.

| Feature | WebAPI 2 (`System.Web.Http`) | ASP.NET Core (`Microsoft.AspNetCore.Mvc`) |
|---------|------------------------------|------------------------------------------|
| Base class | `ApiController` | `ControllerBase` |
| Action result | `IHttpActionResult` | `IActionResult` / `ActionResult<T>` |
| Routing | `config.MapHttpAttributeRoutes()` | `[Route]` + `app.MapControllers()` |
| Hosting | OWIN + IIS | Kestrel (cross-platform) |
| DI | Manual / 3rd-party (Autofac etc.) | Built-in `IServiceCollection` |
| Config | `Web.config` + `ConfigurationManager` | `appsettings.json` + `IConfiguration` |
| Logging | Trace / EventLog | `ILogger<T>` |

The agent's **".NET version upgrade"** scenario handles most of this with
minimal manual fix-up — once the universal findings (Key Vault, managed
identity, OpenTelemetry) are out of the way.

## What App Mod will surface

For the source in [`legacy-code/`](legacy-code/), an assessment with
`target: "AppService.Linux"` produces (see the [example report](example-outputs/appmod-assessment-report.md)):

- 🔴 `System.Web.Http` (WebAPI 2) → ASP.NET Core controllers.
- 🔴 OWIN hosting (`OwinStartup`, `Microsoft.Owin`) → Kestrel + ASP.NET Core middleware.
- 🔴 `System.Data.SqlClient` + `Integrated Security=True`.
- 🔴 SQL injection in `List` (`OrdersController.cs:53`).
- 🔴 `DownstreamApiKey` plaintext in `Web.config:9`.
- 🔴 `EventLog.WriteEntry` in error path.
- 🔴 `HttpWebRequest` for downstream call → `IHttpClientFactory` + Polly.
- 🔴 .NET Framework 4.6.2 (close-but-still EOL).

## Run the assessment

```bash
copilot
> @modernize-dotnet
> Run an App Modernization assessment against samples/webapi2-owin-service/legacy-code/.
> Use target: "AppService.Linux".
```

## Layout

```
webapi2-owin-service/
├── README.md
├── legacy-code/
│   ├── Startup.cs                             ← OWIN startup
│   ├── WebApiConfig.cs                        ← HttpConfiguration + routes
│   ├── OrdersController.cs                    ← ApiController with GET/POST
│   ├── Web.config                             ← .NET 4.6.2, secrets, OWIN
│   └── OrderApi.csproj
└── example-outputs/
    └── appmod-assessment-report.md
```

> 📖 Cross-cutting guidance lives in [`../../guides/`](../../guides/).
