using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using microservice.Extensions;
using ZLogger;

namespace Beamable.Server.Common;

/// <summary>
/// Defines a regex-based masking rule that replaces sensitive log content.
/// </summary>
public class BeamMasker
{
    public Regex Matcher;
    public MatchEvaluator MaskerFunction;

    private static readonly Regex TokenRegex = new(
	    @"((token|Token|TOKEN).?.?.?.?.?)""........-....-....-....-............", RegexOptions.Compiled,
	    TimeSpan.FromMilliseconds(100));

    private static readonly Regex PasswordFieldRegex = new(
	    @"""password""\s*:\s*""([^""]*)""", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Matches env-var name/value pairs where the name contains a sensitive keyword (password, secret, or token),
    /// </summary>
    private static readonly Regex EnvVarSensitiveValueRegex = new(
	    @"(\{""name"":""[^""]*(?:password|secret|token)[^""]*"",""value"":"")[^""]*(""\})",
	    RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private static string ProcessToken(Match match)
    {
	    return $"{match.Groups[1].Value}\"<hidden_token last4=({match.Value.Substring(match.Value.Length - 4)})>";
    }

    private static string ProcessPassword(Match _)
    {
	    return "\"password\":\"<hidden_password>\"";
    }

    private static string ProcessEnvVarValue(Match match)
    {
	    return $"{match.Groups[1].Value}<hidden_value>{match.Groups[2].Value}";
    }
    
    /// <summary>
    /// The default set of maskers applied to log output to hide tokens and passwords.
    /// </summary>
    public static readonly List<BeamMasker> DefaultMaskers = new List<BeamMasker>
    {
        new BeamMasker
        {
            Matcher = TokenRegex,
            MaskerFunction = ProcessToken
        },
        new BeamMasker
        {
            Matcher = PasswordFieldRegex,
            MaskerFunction = ProcessPassword
        },
        new BeamMasker
        {
            Matcher = EnvVarSensitiveValueRegex,
            MaskerFunction = ProcessEnvVarValue
        }
    };
}

/// <summary>
/// A ZLogger formatter that wraps an inner formatter and applies <see cref="BeamMasker"/> rules
/// to scrub sensitive data (tokens, passwords) from log output.
/// </summary>
public class BeamMaskedZLogFormatter : IZLoggerFormatter
{
    private readonly IZLoggerFormatter _inner;
    private readonly IReadOnlyList<BeamMasker> _maskers;

    public BeamMaskedZLogFormatter(IZLoggerFormatter inner, IReadOnlyList<BeamMasker> maskers)
    {
        _inner = inner;
        _maskers = maskers;
    }

    public bool WithLineBreak => _inner.WithLineBreak;

    public void FormatLogEntry(IBufferWriter<byte> writer, IZLoggerEntry entry)
    {
        // format using the inner formatter into a temporary buffer
        var buffer = new ArrayBufferWriter<byte>();
        _inner.FormatLogEntry(buffer, entry);
        var result = Encoding.UTF8.GetString(buffer.WrittenMemory.Span);

        // apply each masking rule in sequence
        foreach (var mask in _maskers)
        {
            result = mask.Matcher.Replace(result, mask.MaskerFunction);
        }

        // write the scrubbed result to the destination writer
        writer.BeamWrite(Encoding.UTF8.GetBytes(result));
    }
}


