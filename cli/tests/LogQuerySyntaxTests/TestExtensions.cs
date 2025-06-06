using System.Text;
using Beamable.Server.LogQuerySyntax;

namespace tests.LogQuerySyntaxTests;

public static class TestExtensions
{
    public static string Dump(this LogQueryTokenCollection collection)
    {
        var sb = new StringBuilder();

        foreach (var token in collection.Tokens)
        {
            sb.AppendLine(token.ToString());
        }
        
        return sb.ToString();
    }
}