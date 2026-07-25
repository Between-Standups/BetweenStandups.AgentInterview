using AgentInterview.Core;
using AgentInterview.Runner;

namespace AgentInterview.IntegrationTests;

public sealed class CalculatorInterviewGraderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SampleGraderFailsStarterImplementation()
    {
        var repositoryRoot = FindRepositoryRoot();
        var package = await LoadPackageAsync(repositoryRoot);
        var workspace = CreateWorkspace(repositoryRoot);

        var result = await new ProcessGrader().GradeAsync(
            new GraderRunRequest(package, new RunWorkspace(_root, workspace)),
            CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public async Task SampleGraderPassesKnownGoodImplementation()
    {
        var repositoryRoot = FindRepositoryRoot();
        var package = await LoadPackageAsync(repositoryRoot);
        var workspace = CreateWorkspace(repositoryRoot);
        File.WriteAllText(Path.Combine(workspace, "Program.cs"), KnownGoodProgram());

        var result = await new ProcessGrader().GradeAsync(
            new GraderRunRequest(package, new RunWorkspace(_root, workspace)),
            CancellationToken.None);

        Assert.True(result.Passed);
        Assert.Equal(100, result.Score);
        Assert.All(result.Cases, item => Assert.True(item.Passed, item.Message));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static async Task<InterviewPackage> LoadPackageAsync(string repositoryRoot)
    {
        var catalog = new FileSystemInterviewCatalog(repositoryRoot);
        return await catalog.GetAsync(new InterviewRef("coding.calculator-api", "1.0.0"), CancellationToken.None);
    }

    private string CreateWorkspace(string repositoryRoot)
    {
        var source = Path.Combine(repositoryRoot, "interviews", "coding", "calculator-api", "v1", "starter");
        var workspace = Path.Combine(_root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(source, file);
            if (relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(part => part is "bin" or "obj"))
            {
                continue;
            }

            var destination = Path.Combine(workspace, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? workspace);
            File.Copy(file, destination);
        }

        return workspace;
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

    private static string KnownGoodProgram() =>
        """
        using System.Text.Json;

        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapPost("/calculate", async (HttpContext context) =>
        {
            CalculationRequest request;
            try
            {
                request = await JsonSerializer.DeserializeAsync<CalculationRequest>(context.Request.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                    ?? throw new JsonException("Request body was empty.");
            }
            catch (JsonException)
            {
                return Error("invalid_request", "Request body must be valid JSON.");
            }

            if (string.IsNullOrWhiteSpace(request.Operation))
            {
                return Error("invalid_request", "Request body must include operation, left, and right.");
            }

            try
            {
                var result = request.Operation switch
                {
                    "add" => checked(request.Left + request.Right),
                    "subtract" => checked(request.Left - request.Right),
                    "multiply" => checked(request.Left * request.Right),
                    "divide" when request.Right == 0 => throw new DivideByZeroException(),
                    "divide" => request.Left / request.Right,
                    _ => throw new InvalidOperationException()
                };

                return Results.Json(new
                {
                    operation = request.Operation,
                    left = request.Left,
                    right = request.Right,
                    result
                });
            }
            catch (DivideByZeroException)
            {
                return Error("division_by_zero", "Cannot divide by zero.");
            }
            catch (OverflowException)
            {
                return Error("overflow", "Arithmetic operation overflowed.");
            }
            catch (InvalidOperationException)
            {
                return Error("invalid_operation", "Unsupported operation.");
            }
        });

        app.Run();

        static IResult Error(string error, string message) =>
            Results.Json(new { error, message }, statusCode: StatusCodes.Status400BadRequest);

        public sealed record CalculationRequest(string Operation, int Left, int Right);
        """;
}
