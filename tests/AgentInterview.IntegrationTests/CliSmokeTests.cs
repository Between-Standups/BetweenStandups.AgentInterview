using System.Diagnostics;

namespace AgentInterview.IntegrationTests;

public sealed class CliSmokeTests
{
    [Fact]
    public async Task ValidateCommandAcceptsSeedCalculatorInterview()
    {
        var repositoryRoot = FindRepositoryRoot();
        var cliAssemblyPath = Path.Combine(repositoryRoot, "src", "AgentInterview.Cli", "bin", "Debug", "net10.0", "AgentInterview.Cli.dll");

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }.WithArguments(cliAssemblyPath, "validate", "--interview", "coding.calculator-api@1.0.0"));

        Assert.NotNull(process);
        var output = await process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var error = await process.StandardError.ReadToEndAsync(CancellationToken.None);
        await process.WaitForExitAsync(CancellationToken.None);

        Assert.Equal(0, process.ExitCode);
        Assert.Contains("coding.calculator-api@1.0.0 is valid.", output);
        Assert.True(string.IsNullOrWhiteSpace(error), error);
    }

    [Fact]
    public async Task RunCommandWritesResultJson()
    {
        var repositoryRoot = FindRepositoryRoot();
        var cliAssemblyPath = Path.Combine(repositoryRoot, "src", "AgentInterview.Cli", "bin", "Debug", "net10.0", "AgentInterview.Cli.dll");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = repositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }.WithArguments(
                cliAssemblyPath,
                "run",
                "--interview",
                "coding.calculator-api@1.0.0",
                "--candidate",
                "configs/example-agent.json",
                "--output",
                outputDirectory));

            Assert.NotNull(process);
            var output = await process.StandardOutput.ReadToEndAsync(CancellationToken.None);
            var error = await process.StandardError.ReadToEndAsync(CancellationToken.None);
            await process.WaitForExitAsync(CancellationToken.None);

            Assert.Equal(0, process.ExitCode);
            Assert.Contains("completed with status 'failed'", output);
            Assert.True(string.IsNullOrWhiteSpace(error), error);
            Assert.Single(Directory.EnumerateFiles(outputDirectory, "*.json"));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunRepetitionsAndCompareGenerateReports()
    {
        var repositoryRoot = FindRepositoryRoot();
        var cliAssemblyPath = Path.Combine(repositoryRoot, "src", "AgentInterview.Cli", "bin", "Debug", "net10.0", "AgentInterview.Cli.dll");
        var outputRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var resultsDirectory = Path.Combine(outputRoot, "results");
        var comparisonDirectory = Path.Combine(outputRoot, "comparison");

        try
        {
            await RunProcessAsync(
                repositoryRoot,
                cliAssemblyPath,
                "run",
                "--interview",
                "coding.calculator-api@1.0.0",
                "--candidate",
                "configs/example-agent.json",
                "--repetitions",
                "3",
                "--output",
                resultsDirectory);
            Assert.Equal(3, Directory.EnumerateFiles(resultsDirectory, "*.json").Count());

            await RunProcessAsync(
                repositoryRoot,
                cliAssemblyPath,
                "compare",
                "--results",
                resultsDirectory,
                "--output",
                comparisonDirectory);

            Assert.True(File.Exists(Path.Combine(comparisonDirectory, "summary.csv")));
            Assert.True(File.Exists(Path.Combine(comparisonDirectory, "comparison.md")));
            Assert.Contains("stable", await File.ReadAllTextAsync(Path.Combine(comparisonDirectory, "summary.csv")));
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AgentInterview.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private static async Task RunProcessAsync(string workingDirectory, string cliAssemblyPath, params string[] arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }.WithArguments([cliAssemblyPath, .. arguments]));

        Assert.NotNull(process);
        var error = await process.StandardError.ReadToEndAsync(CancellationToken.None);
        await process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        await process.WaitForExitAsync(CancellationToken.None);
        Assert.Equal(0, process.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), error);
    }
}

internal static class ProcessStartInfoExtensions
{
    public static ProcessStartInfo WithArguments(this ProcessStartInfo startInfo, params string[] arguments)
    {
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}
