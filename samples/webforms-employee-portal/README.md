# Sample 4 — WebForms Employee Portal

> **Archetype:** ASP.NET WebForms — `.aspx` markup with code-behind, ViewState,
> server controls (`<asp:GridView>`), `Page_Load`, postback events.

## Why this archetype matters

WebForms is the **worst-case modernization scenario**:

- 🔴 **Cannot run on .NET 8+ at all.** ASP.NET WebForms is .NET Framework only.
  There is no shim, no compatibility pack, no Linux story.
- 🔴 **No incremental upgrade path.** You either keep it on Windows (lift to
  AKS Windows nodes) or you **rewrite** the UI as Blazor / ASP.NET Core MVC /
  Razor Pages.
- 🔴 The agent will mark almost everything Mandatory on Linux targets — the
  honest answer is "this is a rewrite, not a migration."

The value of running App Mod here is **decision support** — quantify what's
involved before committing, and inventory the Web.config / Session / ViewState
patterns the rewrite must replace.

| Target | Path |
|--------|------|
| ❌ `AppService.Linux` | Not supported. |
| ❌ `ACA` (Linux) | Not supported. |
| 🟠 `ACA` with Windows containers | Possible but ACA Windows support is limited. |
| 🟢 `AKS.Windows` | Lift-and-shift onto Windows nodes. Same code, different bill. |
| 🟢 `AppService.Windows` | Lift-and-shift to App Service Windows. Easiest path if you're not ready to rewrite. |
| 🟢 Rewrite to **Blazor Server** + ASP.NET Core | Modern, .NET 8+, runs anywhere. The "right" answer for most teams. |

## What App Mod will surface

For the source in [`legacy-code/`](legacy-code/), an assessment with
`target: "Any"` produces (see the [example report](example-outputs/appmod-assessment-report.md)):

- 🔴 ASP.NET WebForms (`.aspx`, `<asp:GridView>`, `Page_Load`, ViewState) — no .NET 8+ equivalent.
- 🔴 `System.Web.HttpContext` (`Global.asax.cs:14, 21`) — replaced by `IHttpContextAccessor` in ASP.NET Core.
- 🔴 `<sessionState mode="InProc">` — incompatible with multi-instance Azure compute.
- 🔴 SQL injection in `BindEmployees` (`Default.aspx.cs:43`) — `department` concatenated.
- 🔴 `System.Data.SqlClient` + `Integrated Security=True` — managed identity.
- 🔴 `EventLog.WriteEntry` in `Application_Error` — OpenTelemetry.
- 🔴 .NET Framework 4.5 — out of support.

## Run the assessment

```bash
copilot
> @modernize-dotnet
> Run an App Modernization assessment against samples/webforms-employee-portal/legacy-code/.
> Use target: "AppService.Windows" — if you want to evaluate the rewrite cost,
> compare with target: "AppService.Linux".
```

## Layout

```
webforms-employee-portal/
├── README.md                                  ← This file
├── legacy-code/
│   ├── Default.aspx                           ← Markup with server controls
│   ├── Default.aspx.cs                        ← Code-behind: Page_Load, GridView events
│   ├── Global.asax.cs                         ← Application_Start / Session_Start
│   ├── Web.config                             ← sessionState, authentication mode="Forms"
│   └── EmployeePortal.csproj                  ← Legacy non-SDK csproj (assessment input only)
└── example-outputs/
    └── appmod-assessment-report.md            ← Worked assessment report
```

> 📖 Cross-cutting guidance lives in [`../../guides/`](../../guides/).
