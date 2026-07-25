# AgentInterview Project Plan

## Purpose

AgentInterview is a deterministic evaluation framework for comparing AI models, prompts, agent configurations, skills, and multi-agent workflows through standardized engineering interview packages.

The core product goal is repeatability: every run of a published interview version must use the same starter project, instructions, fixtures, dependencies, grading rules, resource limits, and scoring logic. The runner should never grade with an LLM as the source of truth.

## V1 Outcomes

V1 is complete when the repository can:

- Discover versioned interview packages from `interviews/`.
- Validate interview manifests against JSON Schema.
- Create a clean temporary workspace for each attempt.
- Copy the immutable starter project into that workspace.
- Execute a configurable candidate adapter.
- Capture execution traces, usage, retries, latency, and tool call counts.
- Run deterministic graders with timeout and process isolation.
- Write machine-readable JSON results.
- Generate CSV summaries and comparison reports.
- Ship one complete local sample interview for a calculator API.
- Pass unit and integration tests without relying on external services during interview execution.

## Proposed Repository Shape

```text
AgentInterview/
├── interviews/
│   └── coding/
│       └── calculator-api/
│           └── v1/
│               ├── interview.json
│               ├── prompt.md
│               ├── starter/
│               ├── grader/
│               ├── expected/
│               └── fixtures/
├── src/
│   ├── AgentInterview.Cli/
│   ├── AgentInterview.Core/
│   ├── AgentInterview.Runner/
│   └── AgentInterview.Reporting/
├── tests/
│   ├── AgentInterview.Core.Tests/
│   ├── AgentInterview.Runner.Tests/
│   └── AgentInterview.IntegrationTests/
├── schemas/
│   ├── interview-manifest.schema.json
│   └── result.schema.json
├── reports/
├── docs/
│   └── project-plan.md
├── Directory.Build.props
├── AgentInterview.sln
└── README.md
```

## Workstreams

### 1. Foundation

- Create the .NET solution and project structure.
- Configure nullable reference types and warnings-as-errors.
- Add shared package management if the repo needs centrally pinned versions.
- Establish test projects and a basic CI-ready test command.
- Define core domain models for manifests, runs, results, scores, usage, and traces.

### 2. Schemas and Validation

- Create `schemas/interview-manifest.schema.json`.
- Create `schemas/result.schema.json`.
- Implement manifest loading and validation.
- Ensure validation errors are explicit and suitable for CLI output.
- Add tests for valid manifests, missing required fields, invalid limits, invalid paths, and unsupported schema versions.

### 3. Interview Catalog

- Implement `IInterviewCatalog`.
- Discover interviews by `id@version`.
- Resolve paths relative to the manifest location.
- Enforce immutability expectations through content hashing.
- Report duplicate IDs, duplicate versions, malformed layouts, and missing candidate or grader assets.

### 4. Workspace and Integrity

- Implement `IWorkspaceManager`.
- Create clean temporary workspaces per run.
- Copy starter files deterministically.
- Exclude forbidden generated directories when needed.
- Compute stable hashes for the interview package, starter project, and grader project.
- Add tests proving identical content yields identical hashes across runs.

### 5. Candidate Adapter Layer

- Define `ICandidateAdapter`.
- Define request and response contracts for candidate execution.
- Support at least one local deterministic adapter for tests and sample development.
- Keep model-provider-specific behavior outside the runner core.
- Capture trace events in a provider-neutral format.

### 6. Runner Lifecycle

- Implement `IInterviewRunner`.
- Orchestrate manifest load, workspace setup, candidate execution, grader execution, scoring, result persistence, and reporting hooks.
- Support repetitions.
- Propagate cancellation tokens.
- Enforce run timeout and resource limits where feasible in V1.
- Persist partial failure results when candidate or grader execution fails.

### 7. Grading

- Implement `IGrader`.
- Run the manifest-provided grader command in an isolated process.
- Capture stdout, stderr, exit code, duration, and structured grader output.
- Define the grader result contract consumed by scoring.
- Ensure graders are deterministic and do not use external services.

### 8. Results and Reporting

- Implement `IResultStore`.
- Write one JSON result per run attempt.
- Implement `IReportGenerator`.
- Generate CSV summaries.
- Generate comparison reports with pass rate, average score, token totals, cost per passing interview, latency, retries, tool calls, and determinism across repeated runs.

### 9. CLI

Implement the suggested commands:

```text
agent-interview list
agent-interview validate --interview coding.calculator-api@1.0.0
agent-interview run --interview coding.calculator-api@1.0.0 --candidate configs/example-agent.json --repetitions 3 --output reports/run-001
agent-interview compare --results reports --output reports/comparison
```

CLI behavior should favor stable machine-readable output where useful, while keeping human-facing validation and run failures clear.

### 10. Sample Interview

Create `interviews/coding/calculator-api/v1` as the first complete interview package.

Candidate-visible requirements:

- Implement an ASP.NET Core calculator API.
- Support the specified arithmetic endpoints.
- Return deterministic response JSON.
- Handle malformed requests consistently.
- Handle division by zero.
- Handle overflow behavior.
- Return consistent HTTP status codes.

Hidden grading:

- Use integration tests from the `grader/` project.
- Run entirely locally.
- Avoid network access after dependencies are restored.
- Cover happy paths, validation paths, edge cases, response shape, and status codes.

## Milestones

Status as of current development:

- Milestone 0: Complete.
- Milestone 1: Complete.
- Milestone 2: Complete.
- Milestones 3-6: Not started.

### Milestone 0: Repository Scaffold

Deliverables:

- Solution and project layout.
- Build configuration.
- Empty core interfaces.
- Initial README updates.

Acceptance checks:

- `dotnet build`
- `dotnet test`

### Milestone 1: Manifest and Catalog

Deliverables:

- Manifest schema.
- Manifest model.
- Catalog discovery.
- Validation command.

Acceptance checks:

- `agent-interview list`
- `agent-interview validate --interview coding.calculator-api@1.0.0`
- Unit tests for validation and discovery.

### Milestone 2: Runner Skeleton

Deliverables:

- Workspace manager.
- Hashing service.
- Candidate adapter interface.
- Deterministic local test adapter.
- JSON result writer.

Acceptance checks:

- A run can execute with a no-op or scripted local adapter.
- Result JSON validates against schema.
- Hashes are stable.

### Milestone 3: Grader Execution

Deliverables:

- Process-based grader runner.
- Timeout handling.
- Grader result parsing.
- Failure result handling.

Acceptance checks:

- Grader success, failure, timeout, and malformed-output scenarios are tested.

### Milestone 4: Sample Calculator Interview

Deliverables:

- Starter ASP.NET Core project.
- Candidate prompt.
- Hidden grader tests.
- Fixtures and expected artifacts where useful.

Acceptance checks:

- A known-good implementation passes.
- A starter implementation fails with useful grader output.
- The interview runs locally without internet access after restore.

### Milestone 5: Reporting and Repetitions

Deliverables:

- Repetition support.
- CSV summary output.
- Comparison report output.
- Determinism metrics across repeated runs.

Acceptance checks:

- `agent-interview run --repetitions 3` writes three result files.
- `agent-interview compare` generates CSV and human-readable comparison artifacts.

### Milestone 6: Hardening

Deliverables:

- Cancellation-token coverage.
- Structured logging.
- Better CLI error formatting.
- README setup, architecture, commands, and extension instructions.
- Integration test coverage for the full lifecycle.

Acceptance checks:

- Clean `dotnet test`.
- README can guide a new contributor through running the sample interview.

## Initial Interface Targets

```csharp
public interface IInterviewCatalog
{
    Task<IReadOnlyList<InterviewSummary>> ListAsync(CancellationToken cancellationToken);
    Task<InterviewPackage> GetAsync(InterviewRef interviewRef, CancellationToken cancellationToken);
}

public interface IWorkspaceManager
{
    Task<RunWorkspace> CreateAsync(InterviewPackage package, CancellationToken cancellationToken);
}

public interface ICandidateAdapter
{
    Task<CandidateRunResult> ExecuteAsync(CandidateRunRequest request, CancellationToken cancellationToken);
}

public interface IInterviewRunner
{
    Task<InterviewRunResult> RunAsync(InterviewRunRequest request, CancellationToken cancellationToken);
}

public interface IGrader
{
    Task<GraderRunResult> GradeAsync(GraderRunRequest request, CancellationToken cancellationToken);
}

public interface IUsageCalculator
{
    UsageSummary Calculate(CandidateRunResult candidateResult);
}

public interface IResultStore
{
    Task SaveAsync(InterviewRunResult result, CancellationToken cancellationToken);
}

public interface IReportGenerator
{
    Task GenerateAsync(ReportRequest request, CancellationToken cancellationToken);
}
```

## Key Design Decisions To Make Early

- Target framework: the handoff suggests `net10.0`; confirm whether the local toolchain supports it or whether the repository should begin on the latest installed SDK and move later.
- Grader output format: decide whether graders emit TRX, JSON, or both.
- Candidate adapter config shape: define a minimal provider-neutral JSON contract.
- Environment isolation depth: determine whether V1 uses process isolation only, container execution, or a pluggable abstraction that can support containers later.
- Network blocking enforcement: determine what V1 can enforce locally versus document as a runner contract.
- Report format: decide whether the human-readable comparison is Markdown, HTML, or both.

## Risks

- Strong isolation may be platform-specific if implemented with containers or OS-level sandboxing.
- Offline execution requires careful dependency restore and package lock discipline.
- Hidden graders must be kept out of candidate-visible workspaces while still remaining easy to run locally.
- Cost and usage accounting will vary by provider, so V1 should distinguish measured usage from estimated usage.
- Reproducibility claims are only as strong as the captured environment metadata.

## Near-Term Next Steps

1. Create the .NET solution and project skeleton.
2. Add `Directory.Build.props` with nullable reference types and warnings-as-errors.
3. Add the two JSON schemas.
4. Implement manifest domain models and validation.
5. Create the `calculator-api` interview directory with an initial manifest and prompt.
6. Add a minimal CLI with `list` and `validate`.
7. Add unit tests around manifest discovery and validation.
