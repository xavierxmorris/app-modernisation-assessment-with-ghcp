# Agent Feedback Loop

> How to give the GHCP App Modernization agent build, test, and review
> feedback during remediation — and why a tight, short loop produces better
> diffs than letting the agent run unsupervised.

---

## Why the loop matters

The agent's predefined tasks are **heuristic transformations**. They handle
the common 80% of cases and stub the rest. The other 20% — custom wrappers
around `ConfigurationManager`, partial classes, generated code, edge cases
in your project setup — produce a build break or a runtime regression.

Without a feedback loop, the agent keeps generating diffs that compound the
problem. **With** a feedback loop, you converge in 2–3 turns to a clean
build.

> The agent is a fast typist. You are the compiler. Pair them.

---

## The basic loop

```
┌─────────────────────────────────────────────────┐
│ 1. Agent applies a predefined task              │
│    (writes diff + plan.md)                      │
└────────────────────┬────────────────────────────┘
                     ▼
┌─────────────────────────────────────────────────┐
│ 2. You build (and ideally run tests)            │
│    `dotnet build` / `dotnet test`               │
└────────────────────┬────────────────────────────┘
                     ▼
        ┌────────────┴────────────┐
        ▼                         ▼
   ✅ green                    ❌ red
        │                         │
        ▼                         ▼
  Continue to next        ┌──────────────────────────┐
  scenario or             │ 3. Show the agent the    │
  open PR                 │    error verbatim        │
                          └────────────┬─────────────┘
                                       ▼
                          ┌──────────────────────────┐
                          │ 4. Agent proposes fix    │
                          └────────────┬─────────────┘
                                       ▼
                                  back to step 2
```

Aim for **3 turns max** to a clean build. If you're past 3 turns, revert
the agent's diff and apply the change manually — the agent has run out of
context for your specific edge case.

---

## What to send the agent (and what not to)

### ✅ Send

- The **exact** error message, including file path and line number.
- The **command** you ran (`dotnet build`, `dotnet test --filter ...`).
- Any **environment differences** the agent should know about (e.g. "I'm
  on Windows, the CI uses Linux containers").
- A **link to the failing build log** if the agent can fetch it.

### ❌ Don't send

- Editorial commentary ("This doesn't work, please try again"). It tells
  the agent nothing.
- Truncated errors. The first line of the error usually doesn't have the
  root cause.
- Multiple unrelated errors in one turn. Fix one, re-build, fix the next.

### Good prompt template

```
The build broke after the last change. Output:

  $ dotnet build
  Determining projects to restore...
  Restored ./MyApp.csproj
  ./Services/OrderService.cs(42,17): error CS0103: The name 'ConfigurationManager' does not exist in the current context

This is the file at line 42:

  public OrderService()
  {
      _conn = ConfigurationManager.ConnectionStrings["OrderDB"].ConnectionString;
  }

The Key Vault task was supposed to convert this to IConfiguration. Please fix
just this one error — don't re-touch other files.
```

The agent now has:

1. The exact error.
2. The exact code.
3. A scoped instruction (just this file, just this error).

It will produce a small, targeted diff. Apply, re-build.

---

## Tests in the loop

If your project has a fast test suite (< 60 seconds), run it after every
agent diff. Build green doesn't mean correct — only tests do.

Recommended order:

1. `dotnet build` — must pass.
2. `dotnet test --filter "FullyQualifiedName~Smoke"` — fast smoke tests.
3. Full `dotnet test` — before opening the PR.

If a test fails, send the failure verbatim:

```
The build is green but a smoke test fails:

  $ dotnet test --filter "FullyQualifiedName~Smoke"
  Failed OrderServiceSmokeTests.LoadByCustomer_ReturnsNonNull
    Expected: not null
    Actual:   null

  at OrderServiceSmokeTests.cs(31)

Looking at OrderService.LoadByCustomer (after the Managed Identity migration),
the method returns null when the connection string is missing the
"Authentication=" segment. This connection string in test config is the old
SQL-auth shape. Please update the test config or add a fallback.
```

---

## When the agent is stuck

If you've sent 3 turns of build errors and the agent's diff is making
things worse:

### Option A — Revert and try a smaller scope

```
@modernize-dotnet
Revert all changes from this scenario. Run the same predefined task again,
but this time only apply it to ./Services/OrderService.cs. Leave every
other file untouched.
```

### Option B — Revert and apply the change manually

The agent's plan.md tells you what *should* happen. Read it, apply the
change yourself, commit. Don't re-invoke the same scenario — you'll loop.

### Option C — Reload context

Long sessions accumulate state. Start a fresh chat:

```
@modernize-dotnet
Fresh context. The repository is in this state: <git status output>.
The Key Vault task was supposed to be complete; the build now fails with
<error>. Please diagnose without assuming any prior conversation.
```

---

## Loop hygiene

| Habit | Why |
|-------|-----|
| **Commit between agent diffs.** Each agent turn = one commit. | If the next turn breaks something, you can `git reset --hard HEAD~1` cleanly. |
| **Stay on the scenario branch.** Never feedback-loop on `main` or your wave branch. | Bad diffs poison the wave. |
| **Don't edit while the agent is mid-turn.** | The diff the agent applies will conflict with yours. |
| **Capture the conversation.** Copy chat transcripts into the PR description. | Reviewers see the rationale. |
| **One scenario per loop.** | Mixing "Key Vault" and "OpenTelemetry" tasks in one loop produces incoherent diffs. |

---

## Build/test in CI vs locally

| Layer | Speed | Use for |
|-------|-------|---------|
| **Local `dotnet build`** | seconds | Every agent turn. The fastest possible signal. |
| **Local fast tests** | < 60s | Every 2–3 agent turns. Catches semantic breaks. |
| **CI build + full tests** | minutes | Before opening the PR. Ensures clean Linux build, integration tests. |
| **CI assessment gate** | minutes | Before merge. Confirms zero Mandatory items. |

The agent should never wait on CI. The agent should converge locally; CI
is the final gate before merge.

---

## Real example — Key Vault task feedback loop

### Turn 1 — Agent applies Key Vault task

Diff: removed `<add key="MerchantSecret" ... />` from `Web.config`, added
`builder.Configuration.AddAzureKeyVault(...)` to `Program.cs`.

### Turn 2 — Local build

```
$ dotnet build
./Services/PaymentService.cs(23,52): error CS0103: The name 'ConfigurationManager' does not exist in the current context
./Services/PaymentService.cs(85,52): error CS0103: The name 'ConfigurationManager' does not exist in the current context
```

### Turn 3 — Send to agent

```
The Key Vault task migrated Web.config but missed two PaymentService.cs lines:
- line 23: var merchantSecret = ConfigurationManager.AppSettings["MerchantSecret"];
- line 85: var merchantSecret = ConfigurationManager.AppSettings["MerchantSecret"];
The class is currently `public class PaymentService` with no DI. Please convert
PaymentService to inject IConfiguration so the Key Vault path works.
```

### Turn 4 — Agent proposes targeted fix

Diff: adds `IConfiguration _config` field + constructor param to
`PaymentService`, replaces both `ConfigurationManager.AppSettings[...]`
calls with `_config["MerchantSecret"]`. Updates one call site to pass the
service.

### Turn 5 — Local build

```
$ dotnet build
Build succeeded. 0 Warning(s) 0 Error(s)
```

✅ Done. Commit, run tests, open PR.

Total: **3 turns to green**. If turn 5 was still red, you'd revert the
scenario and apply manually.

---

## Quick reference

| Situation | Action |
|-----------|--------|
| Build broke after agent diff | Send exact error + offending code, scope to one file |
| Test broke after agent diff | Send test name + assertion details |
| Same error after 3 turns | Revert scenario, apply manually or split scope |
| Diff keeps growing across turns | Revert, run scenario on a smaller subset (`--include` filter) |
| Agent suggesting unrelated changes | "Only fix the build error, don't touch other files" |
| About to open PR | Run `dotnet build`, full `dotnet test`, then re-assess (zero Mandatory expected) |

---

## See also

- [`setup-for-success.md`](setup-for-success.md) — pre-flight that
  prevents most loops from being needed.
- [`common-issues.md`](common-issues.md#7-build-breaks-after-a-predefined-task-runs)
  — issue #7 specifically covers build breaks.
- [`predefined-tasks-cheatsheet.md`](predefined-tasks-cheatsheet.md)
  — preconditions per task; meeting them shortens the loop.
