# BetweenStandups.AgentInterview

AgentInterview is a deterministic evaluation framework for comparing models, prompts, agent configurations, skills, instructions, and multi-agent workflows.

The idea is to interview an AI setup with standardized engineering tasks: every candidate receives the same immutable project, prompt, fixtures, dependencies, limits, and deterministic graders.

## Current Status

This repository is in early V1 development. The first scaffold includes:

- .NET 10 solution structure.
- Core manifest and runner contracts.
- Manifest validation.
- CLI `list` and `validate` commands.
- JSON schemas for interview manifests and run results.
- A seed `coding.calculator-api@1.0.0` interview package.

## Commands

```bash
dotnet build
dotnet test
dotnet run --project src/AgentInterview.Cli -- list
dotnet run --project src/AgentInterview.Cli -- validate --interview coding.calculator-api@1.0.0
```

## Planning

See [docs/project-plan.md](docs/project-plan.md) for the V1 roadmap.
