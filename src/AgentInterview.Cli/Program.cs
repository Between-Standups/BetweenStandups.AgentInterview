using AgentInterview.Core;
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

    private static void WriteHelp()
    {
        Console.WriteLine("AgentInterview");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  agent-interview list");
        Console.WriteLine("  agent-interview validate --interview <id@version>");
    }
}
