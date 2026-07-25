using AgentInterview.Core;

namespace AgentInterview.Core.Tests;

public sealed class InterviewRefTests
{
    [Fact]
    public void TryParseAcceptsIdAtVersion()
    {
        var parsed = InterviewRef.TryParse("coding.calculator-api@1.0.0", out var interviewRef);

        Assert.True(parsed);
        Assert.NotNull(interviewRef);
        Assert.Equal("coding.calculator-api", interviewRef.Id);
        Assert.Equal("1.0.0", interviewRef.Version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("@1.0.0")]
    [InlineData("coding.calculator-api@")]
    [InlineData("coding.calculator-api")]
    public void TryParseRejectsInvalidReferences(string value)
    {
        var parsed = InterviewRef.TryParse(value, out var interviewRef);

        Assert.False(parsed);
        Assert.Null(interviewRef);
    }
}
