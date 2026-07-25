using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var workspace = Environment.GetEnvironmentVariable("AGENT_INTERVIEW_WORKSPACE");
if (string.IsNullOrWhiteSpace(workspace))
{
    WriteResult([GraderCase.Fail("environment.workspace", 0, "AGENT_INTERVIEW_WORKSPACE was not set.")]);
    return;
}

var projectPath = Path.Combine(workspace, "CalculatorApi.csproj");
if (!File.Exists(projectPath))
{
    WriteResult([GraderCase.Fail("starter.project", 0, "CalculatorApi.csproj was not found in the candidate workspace.")]);
    return;
}

var port = GetFreePort();
using var process = StartApi(projectPath, port);

try
{
    using var client = new HttpClient
    {
        BaseAddress = new Uri($"http://127.0.0.1:{port}")
    };

    if (!await WaitForServerAsync(client))
    {
        WriteResult([GraderCase.Fail("server.startup", 0, "The candidate API did not start within the timeout.")]);
        return;
    }

    var cases = new List<GraderCase>
    {
        await ExpectCalculationAsync(client, "calculator.add", "add", 40, 2, 42, 20),
        await ExpectCalculationAsync(client, "calculator.subtract", "subtract", 10, 7, 3, 15),
        await ExpectCalculationAsync(client, "calculator.multiply", "multiply", 6, 7, 42, 15),
        await ExpectCalculationAsync(client, "calculator.divide", "divide", 84, 2, 42, 15),
        await ExpectErrorAsync(client, "calculator.divide-by-zero", new { operation = "divide", left = 1, right = 0 }, "division_by_zero", 10),
        await ExpectErrorAsync(client, "calculator.overflow", new { operation = "multiply", left = int.MaxValue, right = 2 }, "overflow", 10),
        await ExpectRawErrorAsync(client, "calculator.malformed", "{", "invalid_request", 10),
        await ExpectErrorAsync(client, "calculator.unknown-operation", new { operation = "mod", left = 7, right = 3 }, "invalid_operation", 5)
    };

    WriteResult(cases);
}
finally
{
    KillProcess(process);
}

static Process StartApi(string projectPath, int port)
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardError = true,
            RedirectStandardOutput = true
        }
    };
    process.StartInfo.ArgumentList.Add("run");
    process.StartInfo.ArgumentList.Add("--project");
    process.StartInfo.ArgumentList.Add(projectPath);
    process.StartInfo.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
    process.Start();
    return process;
}

static async Task<bool> WaitForServerAsync(HttpClient client)
{
    var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
    while (DateTimeOffset.UtcNow < deadline)
    {
        try
        {
            using var response = await client.GetAsync("/");
            return true;
        }
        catch (HttpRequestException)
        {
            await Task.Delay(250);
        }
    }

    return false;
}

static async Task<GraderCase> ExpectCalculationAsync(
    HttpClient client,
    string name,
    string operation,
    int left,
    int right,
    int expectedResult,
    int score)
{
    var response = await client.PostAsJsonAsync("/calculate", new { operation, left, right });
    var body = await response.Content.ReadAsStringAsync();

    if (response.StatusCode != HttpStatusCode.OK)
    {
        return GraderCase.Fail(name, 0, $"Expected HTTP 200, received {(int)response.StatusCode}. Body: {body}");
    }

    try
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var actualOperation = root.GetProperty("operation").GetString();
        var actualLeft = root.GetProperty("left").GetInt32();
        var actualRight = root.GetProperty("right").GetInt32();
        var actualResult = root.GetProperty("result").GetInt32();

        return actualOperation == operation && actualLeft == left && actualRight == right && actualResult == expectedResult
            ? GraderCase.Pass(name, score)
            : GraderCase.Fail(name, 0, $"Unexpected response JSON: {body}");
    }
    catch (JsonException exception)
    {
        return GraderCase.Fail(name, 0, $"Response was not valid JSON: {exception.Message}");
    }
    catch (KeyNotFoundException exception)
    {
        return GraderCase.Fail(name, 0, $"Response JSON was missing a required property: {exception.Message}");
    }
}

static Task<GraderCase> ExpectErrorAsync(HttpClient client, string name, object request, string expectedError, int score) =>
    ExpectErrorResponseAsync(name, score, expectedError, () => client.PostAsJsonAsync("/calculate", request));

static Task<GraderCase> ExpectRawErrorAsync(HttpClient client, string name, string body, string expectedError, int score) =>
    ExpectErrorResponseAsync(
        name,
        score,
        expectedError,
        () => client.PostAsync("/calculate", new StringContent(body, Encoding.UTF8, "application/json")));

static async Task<GraderCase> ExpectErrorResponseAsync(
    string name,
    int score,
    string expectedError,
    Func<Task<HttpResponseMessage>> sendAsync)
{
    using var response = await sendAsync();
    var body = await response.Content.ReadAsStringAsync();
    if (response.StatusCode != HttpStatusCode.BadRequest)
    {
        return GraderCase.Fail(name, 0, $"Expected HTTP 400, received {(int)response.StatusCode}. Body: {body}");
    }

    try
    {
        using var document = JsonDocument.Parse(body);
        var error = document.RootElement.GetProperty("error").GetString();
        var message = document.RootElement.GetProperty("message").GetString();
        return error == expectedError && !string.IsNullOrWhiteSpace(message)
            ? GraderCase.Pass(name, score)
            : GraderCase.Fail(name, 0, $"Unexpected error response JSON: {body}");
    }
    catch (JsonException exception)
    {
        return GraderCase.Fail(name, 0, $"Error response was not valid JSON: {exception.Message}");
    }
    catch (KeyNotFoundException exception)
    {
        return GraderCase.Fail(name, 0, $"Error response JSON was missing a required property: {exception.Message}");
    }
}

static void WriteResult(IReadOnlyList<GraderCase> cases)
{
    var score = cases.Sum(item => item.Score);
    var result = new GraderResult(
        Passed: score == 100 && cases.All(item => item.Passed),
        Score: score,
        MaximumScore: 100,
        Cases: cases);

    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    }));
}

static int GetFreePort()
{
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}

static void KillProcess(Process process)
{
    try
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }
    }
    catch (InvalidOperationException)
    {
    }
}

public sealed record GraderResult(
    [property: JsonPropertyName("passed")] bool Passed,
    [property: JsonPropertyName("score")] int Score,
    [property: JsonPropertyName("maximumScore")] int MaximumScore,
    [property: JsonPropertyName("cases")] IReadOnlyList<GraderCase> Cases);

public sealed record GraderCase(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("passed")] bool Passed,
    [property: JsonPropertyName("score")] int Score,
    [property: JsonPropertyName("message")] string? Message)
{
    public static GraderCase Pass(string name, int score) => new(name, true, score, null);

    public static GraderCase Fail(string name, int score, string message) => new(name, false, score, message);
}
