using AgentInterview.Core;
using AgentInterview.Reporting;
using AgentInterview.Runner;

return await AgentInterviewCli.RunAsync(args, Directory.GetCurrentDirectory(), CancellationToken.None).ConfigureAwait(false);

internal static class AgentInterviewCli
{
    public static async Task<int> RunAsync(string[] args, string repositoryRoot, CancellationToken cancellationToken)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            WriteHelp();
            return 0;
        }

        var catalog = new FileSystemInterviewCatalog(repositoryRoot);

        try
        {
            return args[0] switch
            {
                "list" => await ListAsync(catalog, cancellationToken).ConfigureAwait(false),
                "validate" => await ValidateAsync(catalog, args, cancellationToken).ConfigureAwait(false),
                "run" => await RunAsync(catalog, repositoryRoot, args, cancellationToken).ConfigureAwait(false),
                "compare" => await CompareAsync(repositoryRoot, args, cancellationToken).ConfigureAwait(false),
                _ => UnknownCommand(args[0])
            };
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static async Task<int> ListAsync(IInterviewCatalog catalog, CancellationToken cancellationToken)
    {
        var summaries = await catalog.ListAsync(cancellationToken).ConfigureAwait(false);
        if (summaries.Count == 0)
        {
            Console.WriteLine("No interviews found.");
            return 0;
        }

        foreach (var summary in summaries)
        {
            Console.WriteLine($"{summary.Id}@{summary.Version}\t{summary.Title}\t{summary.Category}\t{summary.Difficulty}");
        }

        return 0;
    }

    private static async Task<int> ValidateAsync(IInterviewCatalog catalog, string[] args, CancellationToken cancellationToken)
    {
        var interviewValue = ReadOption(args, "--interview");
        if (interviewValue is null)
        {
            Console.Error.WriteLine("Missing required option: --interview");
            return 1;
        }

        if (!InterviewRef.TryParse(interviewValue, out var interviewRef) || interviewRef is null)
        {
            Console.Error.WriteLine("Invalid interview reference. Expected format: id@version");
            return 1;
        }

        var package = await catalog.GetAsync(interviewRef, cancellationToken).ConfigureAwait(false);
        var validation = ManifestValidator.Validate(package.Manifest, package.PackageDirectory);

        if (validation.IsValid)
        {
            Console.WriteLine($"{interviewRef} is valid.");
            return 0;
        }

        Console.Error.WriteLine($"{interviewRef} is invalid:");
        foreach (var error in validation.Errors)
        {
            Console.Error.WriteLine($"- {error}");
        }

        return 1;
    }

    private static async Task<int> RunAsync(
        IInterviewCatalog catalog,
        string repositoryRoot,
        string[] args,
        CancellationToken cancellationToken)
    {
        var interviewValue = ReadOption(args, "--interview");
        var candidateConfigurationPath = ReadOption(args, "--candidate");
        var outputDirectory = ReadOption(args, "--output");
        var repetitionsValue = ReadOption(args, "--repetitions");

        if (interviewValue is null)
        {
            Console.Error.WriteLine("Missing required option: --interview");
            return 1;
        }

        if (candidateConfigurationPath is null)
        {
            Console.Error.WriteLine("Missing required option: --candidate");
            return 1;
        }

        if (outputDirectory is null)
        {
            Console.Error.WriteLine("Missing required option: --output");
            return 1;
        }

        if (!InterviewRef.TryParse(interviewValue, out var interviewRef) || interviewRef is null)
        {
            Console.Error.WriteLine("Invalid interview reference. Expected format: id@version");
            return 1;
        }

        var runner = new InterviewRunner(
            catalog,
            new FileSystemWorkspaceManager(),
            new NoOpCandidateAdapter(),
            new ProcessGrader(),
            new DirectoryContentHasher());

        var repetitions = ParseRepetitions(repetitionsValue);
        for (var attempt = 1; attempt <= repetitions; attempt++)
        {
            var result = await runner.RunAsync(
                new InterviewRunRequest(
                    interviewRef,
                    Path.GetFullPath(Path.Combine(repositoryRoot, candidateConfigurationPath)),
                    Path.GetFullPath(Path.Combine(repositoryRoot, outputDirectory))),
                cancellationToken).ConfigureAwait(false);

            Console.WriteLine($"Run {attempt}/{repetitions} {result.RunId:N} completed with status '{result.Status}'.");
            Console.WriteLine($"Result: {Path.Combine(outputDirectory, $"{result.RunId:N}.json")}");
        }

        return 0;
    }

    private static async Task<int> CompareAsync(string repositoryRoot, string[] args, CancellationToken cancellationToken)
    {
        var resultsDirectory = ReadOption(args, "--results");
        var outputDirectory = ReadOption(args, "--output");

        if (resultsDirectory is null)
        {
            Console.Error.WriteLine("Missing required option: --results");
            return 1;
        }

        if (outputDirectory is null)
        {
            Console.Error.WriteLine("Missing required option: --output");
            return 1;
        }

        var generator = new MarkdownReportGenerator();
        await generator.GenerateAsync(
            new ReportRequest(
                Path.GetFullPath(Path.Combine(repositoryRoot, resultsDirectory)),
                Path.GetFullPath(Path.Combine(repositoryRoot, outputDirectory))),
            cancellationToken).ConfigureAwait(false);

        Console.WriteLine($"Comparison: {Path.Combine(outputDirectory, "comparison.md")}");
        Console.WriteLine($"Summary: {Path.Combine(outputDirectory, "summary.csv")}");
        return 0;
    }

    private static string? ReadOption(string[] args, string optionName)
    {
        for (var index = 1; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], optionName, StringComparison.Ordinal))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        WriteHelp();
        return 1;
    }

    private static bool IsHelp(string value) =>
        string.Equals(value, "--help", StringComparison.Ordinal)
        || string.Equals(value, "-h", StringComparison.Ordinal)
        || string.Equals(value, "help", StringComparison.Ordinal);

    private static int ParseRepetitions(string? value)
    {
        if (value is null)
        {
            return 1;
        }

        if (!int.TryParse(value, out var repetitions) || repetitions < 1)
        {
            throw new ArgumentException("--repetitions must be a positive integer.");
        }

        return repetitions;
    }

    private static void WriteHelp()
    {
        Console.WriteLine("AgentInterview");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  agent-interview list");
        Console.WriteLine("  agent-interview validate --interview <id@version>");
        Console.WriteLine("  agent-interview run --interview <id@version> --candidate <config.json> --repetitions <count> --output <directory>");
        Console.WriteLine("  agent-interview compare --results <directory> --output <directory>");
    }
}
