namespace AgentInterview.Runner.Tests;

public sealed class DirectoryContentHasherTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public DirectoryContentHasherTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public async Task HashDirectoryAsyncReturnsStableHashForSameContent()
    {
        File.WriteAllText(Path.Combine(_directory, "b.txt"), "two");
        File.WriteAllText(Path.Combine(_directory, "a.txt"), "one");
        var hasher = new DirectoryContentHasher();

        var first = await hasher.HashDirectoryAsync(_directory, CancellationToken.None);
        var second = await hasher.HashDirectoryAsync(_directory, CancellationToken.None);

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task HashDirectoryAsyncChangesWhenContentChanges()
    {
        var file = Path.Combine(_directory, "a.txt");
        File.WriteAllText(file, "one");
        var hasher = new DirectoryContentHasher();

        var first = await hasher.HashDirectoryAsync(_directory, CancellationToken.None);
        File.WriteAllText(file, "two");
        var second = await hasher.HashDirectoryAsync(_directory, CancellationToken.None);

        Assert.NotEqual(first, second);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
