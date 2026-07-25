using System.Text;

namespace AgentInterview.Runner;

internal static class CommandLine
{
    public static IReadOnlyList<string> Split(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);

        var tokens = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var character in command)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(character) && !inQuotes)
            {
                AddCurrent(tokens, current);
                continue;
            }

            current.Append(character);
        }

        AddCurrent(tokens, current);
        return tokens;
    }

    private static void AddCurrent(List<string> tokens, StringBuilder current)
    {
        if (current.Length == 0)
        {
            return;
        }

        tokens.Add(current.ToString());
        current.Clear();
    }
}
