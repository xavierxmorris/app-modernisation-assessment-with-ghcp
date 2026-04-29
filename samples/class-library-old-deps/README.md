# Sample 7 — Class Library with Old Dependencies

> **Archetype:** .NET Framework 4.5.2 class library with legacy
> non-SDK csproj, `packages.config`, AutoMapper 6.x, log4net 2.0.8,
> Newtonsoft.Json 9.0.1, and `System.Net.Http` 4.3.0.

## Why this archetype matters

Class libraries are usually shared across many projects in an enterprise.
Modernizing one of them touches every consumer, so you want to know **what
you're committing to** before clicking the agent's "Apply" button.

This sample is engineered to surface the classic dependency-pile-up that
hits almost every legacy library:

| Dependency | Version in sample | Problem |
|------------|-------------------|---------|
| `AutoMapper` | 6.2.2 | Static `Mapper.Initialize` API — removed in 9.0+. Migration to instance-based `IMapper` + DI required. |
| `log4net` | 2.0.8 | Cross-platform OK in .NET 8, but the broader move is to `Microsoft.Extensions.Logging` + OpenTelemetry. |
| `Newtonsoft.Json` | 9.0.1 | Dedicated **"Newtonsoft.Json upgrade"** scenario migrates to `System.Text.Json`. The `TypeNameHandling.Auto` usage in this sample is a known security risk. |
| `System.Net.Http` | 4.3.0 | The notorious "transitive dep mess" version with multiple high-severity CVEs. |
| `packages.config` | — | Replaced by `<PackageReference>` after the SDK-style conversion. |

## What App Mod will surface

For the source in [`legacy-code/`](legacy-code/), an assessment with
`target: "Any"` produces (see the [example report](example-outputs/appmod-assessment-report.md)):

- 🔴 .NET Framework 4.5.2 — out of support.
- 🔴 `Newtonsoft.Json 9.0.1` with `TypeNameHandling.Auto` — security risk.
- 🔴 `System.Net.Http 4.3.0` — CVE-laden version.
- 🔴 `AutoMapper.Mapper.Initialize` static API — removed in modern AutoMapper.
- 🔴 `packages.config` — convert to `PackageReference`.
- 🟠 `log4net.Config.XmlConfigurator.Configure()` — replace with
  `Microsoft.Extensions.Logging` for cloud-native consumers.
- 🟠 Local file-system log path `C:\Logs\...` — won't exist in containers.

## Run the assessment

```bash
copilot
> @modernize-dotnet
> Run an App Modernization assessment against samples/class-library-old-deps/legacy-code/.
> Use target: "Any" and call out which findings ship to consumers as
> breaking changes (e.g. AutoMapper 9 API).
```

## Layout

```
class-library-old-deps/
├── README.md
├── legacy-code/
│   ├── MappingService.cs                  ← AutoMapper 6.x static init
│   ├── LoggingHelper.cs                   ← log4net direct usage
│   ├── JsonExtensions.cs                  ← Newtonsoft.Json + TypeNameHandling.Auto
│   ├── packages.config                    ← Old-style package refs
│   ├── App.config                         ← log4net file appender at C:\Logs\...
│   └── Contoso.Mapping.csproj             ← Legacy non-SDK csproj
└── example-outputs/
    └── appmod-assessment-report.md
```

> 📖 Cross-cutting guidance lives in [`../../guides/`](../../guides/).
