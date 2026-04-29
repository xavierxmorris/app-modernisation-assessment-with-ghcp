# AppCAT CLI Integration

> When and how to use the **.NET App Container & Azure Toolkit (AppCAT) CLI**
> alongside the GHCP App Modernization agent. AppCAT is the static-analysis
> engine the agent wraps; running the CLI directly gives you scriptable
> assessments, CI integration, and a way to handle large solutions that
> exceed the agent's context budget.

---

## When to reach for the CLI

| Situation | Use CLI? | Use Agent? |
|-----------|:-------:|:----------:|
| First-time exploration of a small project | ❌ | ✅ |
| First-time exploration of a 50-project solution | ✅ | ✅ (after CLI scopes the work) |
| CI gate: "fail if Mandatory > 0" | ✅ | ❌ |
| Nightly assessment of trunk | ✅ | ❌ |
| Importing the result into a teammate's editor for review | ✅ → Import in agent | ✅ (after import) |
| Driving remediation (predefined tasks, code edits) | ❌ | ✅ |
| Triage with the team | ❌ (CLI output is harder to read) | ✅ |

**Rule of thumb:** the **agent** is for humans; the **CLI** is for pipelines.
Use both, in that order.

---

## Install

AppCAT ships as a **.NET tool**. Install once:

```bash
dotnet tool install --global appcat
```

Or pin to your team's tools manifest:

```bash
dotnet new tool-manifest                        # if not present
dotnet tool install appcat --create-manifest-if-needed
```

This produces `.config/dotnet-tools.json`:

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "appcat": {
      "version": "8.0.0",
      "commands": [ "appcat" ]
    }
  }
}
```

> 💡 **Pin the version.** The agent and the CLI must be on compatible
> schema versions. See [common-issues.md #9](common-issues.md#9-appcat-cli-import-schema-mismatch).

Verify:

```bash
dotnet tool restore
dotnet appcat --version
```

---

## Run an assessment from the CLI

Basic shape:

```bash
dotnet appcat \
  --source <path-to-csproj-or-solution> \
  --target AppService.Linux \
  --output ./appcat-output \
  --format json
```

Common flags:

| Flag | Purpose |
|------|---------|
| `--source` | Path to a `.csproj` / `.sln` / source folder. |
| `--target` | One of `Any`, `AppService.Linux`, `AppService.Windows`, `ACA`, `AKS.Windows`, `AKS.Linux`, `AppServiceContainer.Linux`, `AppServiceContainer.Windows`, `AppServiceManagedInstance.Windows`. Same set as the agent. |
| `--output` | Output directory. Created if it doesn't exist. |
| `--format` | `json` (machine-readable, recommended) or `html` (human-readable, for one-off review). |
| `--exclude` | Glob patterns to skip — e.g. `--exclude "**/Generated/**"`. |
| `--include` | Glob patterns to include only. Useful for slicing a monorepo. |
| `--quiet` | Suppress progress output (use in CI). |

Example for the legacy sample in this repo:

```bash
dotnet appcat \
  --source samples/wcf-billing-service/legacy-code/BillingService.csproj \
  --target AppService.Linux \
  --output appmod/reports/wcf-billing-AppService.Linux \
  --format json \
  --exclude "**/bin/**" --exclude "**/obj/**"
```

---

## Output shape

`--format json` produces a directory like:

```
appmod/reports/wcf-billing-AppService.Linux/
├── assessment.json          ← Findings, severities, file refs
├── application.json         ← App info: projects, frameworks, lines of code
├── target.json              ← Target service + recommendations
└── log.txt                  ← Run log; useful for debugging
```

The `assessment.json` is what you `Import` in the agent dashboard, and
what CI scripts parse for gating.

---

## CI integration — fail the build on Mandatory issues

GitHub Actions step that runs the CLI and fails the job if any Mandatory
finding is present:

```yaml
- name: Run App Mod assessment
  run: |
    dotnet tool restore
    dotnet appcat \
      --source ./MyApp.sln \
      --target AppService.Linux \
      --output ./appcat-output \
      --format json \
      --quiet

- name: Fail on Mandatory findings
  run: |
    MANDATORY=$(jq '.issues | map(select(.criticality == "Mandatory")) | length' \
      ./appcat-output/assessment.json)
    echo "Mandatory findings: $MANDATORY"
    if [ "$MANDATORY" -gt 0 ]; then
      echo "::error::App Modernization assessment has $MANDATORY Mandatory findings."
      exit 1
    fi

- name: Upload assessment as artifact
  if: always()
  uses: actions/upload-artifact@v4
  with:
    name: appcat-output
    path: ./appcat-output/
```

This pattern means:

- Every PR runs the assessment.
- The PR can't merge while Mandatory findings remain.
- The full report is downloadable as a workflow artifact.

> 💡 **Don't gate on `Potential` or `Optional`.** They're advisory. Failing
> CI on advisories drives engineers to suppress the assessment instead of
> reading it.

---

## Importing CLI output into the agent dashboard

In the agent's chat panel:

```
@modernize-dotnet
import assessment report
Path: appmod/reports/wcf-billing-AppService.Linux/assessment.json
```

Or use the **Import** button in the dashboard.

The dashboard renders the imported report exactly as if you'd run the
assessment in the agent — including target switching (if the CLI was run
with `--target Any`).

---

## When to assess in pieces

For monorepos with 30+ projects, single-shot assessment can:

- Take 20+ minutes.
- Hit context limits.
- Produce reports too large to triage.

**Slice the work.** Per-project or per-layer:

```bash
for csproj in $(find ./services -name '*.csproj'); do
  service=$(basename $(dirname $csproj))
  dotnet appcat \
    --source "$csproj" \
    --target AppService.Linux \
    --output "./appmod/reports/$service" \
    --format json --quiet
done
```

Then import each report individually for triage. Aggregate Mandatory
counts in a summary CSV:

```bash
echo "service,mandatory,potential,optional" > summary.csv
for d in ./appmod/reports/*/; do
  service=$(basename "$d")
  m=$(jq '.issues | map(select(.criticality=="Mandatory")) | length' "$d/assessment.json")
  p=$(jq '.issues | map(select(.criticality=="Potential")) | length' "$d/assessment.json")
  o=$(jq '.issues | map(select(.criticality=="Optional")) | length' "$d/assessment.json")
  echo "$service,$m,$p,$o" >> summary.csv
done
```

The summary CSV is the planning input for the wave.

---

## Comparing two CLI runs (regression check)

After remediation:

```bash
# Before
dotnet appcat --source ./MyApp.sln --target AppService.Linux \
  --output ./baseline --format json --quiet

# Run remediation, then:
dotnet appcat --source ./MyApp.sln --target AppService.Linux \
  --output ./after --format json --quiet

# Diff
diff <(jq '.issues | map(.id) | sort' ./baseline/assessment.json) \
     <(jq '.issues | map(.id) | sort' ./after/assessment.json)
```

`Removed` lines = findings the remediation resolved. ✅
`Added` lines = findings the remediation introduced. ⚠ Read carefully.

---

## Common pitfalls

- **Running CLI without `--target`** — defaults to `Any`, which is great
  for exploration but produces noisy output for CI gating. Be explicit.
- **Not pinning AppCAT version** — see
  [common-issues.md #9](common-issues.md#9-appcat-cli-import-schema-mismatch).
- **Treating CLI output as the source of truth without team review** —
  CLI doesn't know which findings are false positives. Always triage with
  humans before remediating.
- **Running the CLI on a dirty tree** — agent and CLI both prefer a clean
  working directory. CI hosts naturally have this; local runs don't.
- **Forgetting `--exclude` for generated code** — you'll spend an hour
  triaging findings that were never going to be hand-edited.

---

## Quick recipe summary

| Task | Command |
|------|---------|
| Install | `dotnet tool install --global appcat` |
| Local one-off | `dotnet appcat --source <csproj> --target Any --output ./out --format html` |
| CI gate | See [GitHub Actions snippet above](#ci-integration--fail-the-build-on-mandatory-issues) |
| Per-project monorepo sweep | `for csproj in $(find . -name '*.csproj') ; do dotnet appcat --source "$csproj" --target AppService.Linux --output "./reports/$(basename $(dirname $csproj))" --format json ; done` |
| Diff against baseline | `diff <(jq '.issues | map(.id) | sort' baseline.json) <(jq '.issues | map(.id) | sort' after.json)` |
| Hand off to teammate | Run with `--format json`, commit `assessment.json`, teammate `Import`s in agent |
