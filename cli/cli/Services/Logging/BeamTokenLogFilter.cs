using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using microservice.Extensions;
using UnityEngine;
using ZLogger;
using ZLogger.Formatters;

namespace cli;

public static class BeamZLogFormatterExtensions
{
    public static ZLoggerOptions UseBeamFormatter(this ZLoggerOptions opts, IAppContext context)
    {
        opts.UseFormatter(() => new BeamZLogFormatter(context, new PlainTextZLoggerFormatter()));
        return opts;
    }
    
}

public class BeamMasker
{
    public Regex matcher;
    public MatchEvaluator maskerFunction;
} 

public partial class BeamZLogFormatter : IZLoggerFormatter
{
    private readonly IAppContext _context;
    private readonly IZLoggerFormatter _innerFormatter;

    [GeneratedRegex("((token|Token|TOKEN).?.?.?.?.?)\"........-....-....-....-............", RegexOptions.None, "en-US")]
    public static partial Regex TokenRegex();

    [GeneratedRegex(@"""password""\s*:\s*""([^""]*)""", RegexOptions.None, "en-US")]
    public static partial Regex PasswordFieldRegex();

    public static List<BeamMasker> customMasks = new List<BeamMasker>
    {
        new BeamMasker
        {
            matcher = TokenRegex(),
            maskerFunction = ProcessToken
        },
        new BeamMasker
        {
            matcher = PasswordFieldRegex(),
            maskerFunction = ProcessPassword
        }
    };
    
    public BeamZLogFormatter(IAppContext context, IZLoggerFormatter innerFormatter)
    {
        _context = context;
        _innerFormatter = innerFormatter;
    }

    public void FormatLogEntry(IBufferWriter<byte> writer, IZLoggerEntry entry)
    {

        if (!_context.ShouldMaskLogs)
        {
            // there is no work to be done, so just proxy out to the original formatter
            _innerFormatter.FormatLogEntry(writer, entry);
            return;
        }

        // format the message as it should look
        var buffer = new ArrayBufferWriter<byte>();
        _innerFormatter.FormatLogEntry(buffer, entry);
        var result = Encoding.UTF8.GetString(buffer.WrittenMemory.Span);

        // do masking operations to takeout sensitive data
        foreach (var mask in customMasks)
        {
            result = mask.matcher.Replace(result, mask.maskerFunction);
        }
        
        // and finally write the masked result to the destination
        var value = Encoding.UTF8.GetBytes(result);
        writer.BeamWrite(value);
    }

    public bool WithLineBreak => _innerFormatter.WithLineBreak;
    
    static string ProcessToken(Match match)
    {
        return $"{match.Groups[1].Value}\"<hidden_token last4=({match.Value.Substring(match.Value.Length - 4)})>";
    }

    static string ProcessPassword(Match _)
    {
        return "\"password\":\"<hidden_password>\"";
    }
}