# BetweenStandups.AgentInterview

AgentInterview is a deterministic evaluation framework for comparing models, prompts, agent configurations, skills, instructions, and multi-agent workflows.

The idea is to interview an AI setup with standardized engineering tasks: every candidate receives the same immutable project, prompt, fixtures, dependencies, limits, and deterministic graders.

## Current Status

V1 foundation milestones are complete:

- .NET 10 solution structure.
- Core manifest and runner contracts.
- Manifest validation.
- CLI `list`, `validate`, `run`, and `compare` commands.
- JSON schemas for interview manifests and run results.
- A complete local `coding.calculator-api@1.0.0` starter and deterministic grader.
- JSON result output, JSONL run logs, CSV summaries, and Markdown comparison reports.

## Commands

```bash
dotnet build
dotnet test
dotnet run --project src/AgentInterview.Cli -- list
dotnet run --project src/AgentInterview.Cli -- validate --interview coding.calculator-api@1.0.0
dotnet run --project src/AgentInterview.Cli -- run --interview coding.calculator-api@1.0.0 --candidate configs/example-agent.json --repetitions 3 --output reports/run-001
dotnet run --project src/AgentInterview.Cli -- compare --results reports/run-001 --output reports/comparison
```

The sample starter is intentionally incomplete, so the sample grader returns a deterministic failing result until a candidate implements the calculator API.

## Architecture

- `AgentInterview.Core` defines manifests, run/result contracts, and extension interfaces.
- `AgentInterview.Runner` discovers interviews, creates clean workspaces, runs candidate adapters, executes graders, hashes package contents, writes JSON results, and emits JSONL run logs.
- `AgentInterview.Reporting` reads result JSON files and generates CSV and Markdown comparisons.
- `AgentInterview.Cli` provides the local command surface.
- `interviews/` contains immutable interview packages. Each version owns its prompt, starter project, fixtures, expected artifacts, and grader.

## Extension Points

- Add interview packages under `interviews/<category>/<name>/v<version>/`.
- Add candidate adapters by implementing `ICandidateAdapter`.
- Add grader strategies by implementing `IGrader`; the default process grader expects deterministic JSON on stdout.
- Add result destinations by implementing `IResultStore`.
- Add report formats by implementing `IReportGenerator`.

Published interview versions should be treated as immutable. Change the version when prompts, starters, fixtures, or graders change.

## Planning

See [docs/project-plan.md](docs/project-plan.md) for the V1 roadmap.
