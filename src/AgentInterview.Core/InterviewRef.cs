namespace AgentInterview.Core;

public sealed record InterviewRef(string Id, string Version)
{
    public static bool TryParse(string value, out InterviewRef? interviewRef)
    {
        ArgumentNullException.ThrowIfNull(value);

        var separatorIndex = value.LastIndexOf('@');
        if (separatorIndex <= 0 || separatorIndex == value.Length - 1)
        {
            interviewRef = null;
            return false;
        }

        interviewRef = new InterviewRef(
            value[..separatorIndex],
            value[(separatorIndex + 1)..]);
        return true;
    }

    public override string ToString() => $"{Id}@{Version}";
}
