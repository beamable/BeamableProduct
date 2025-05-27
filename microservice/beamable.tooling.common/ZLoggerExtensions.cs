using System.Text.Json;
using ZLogger;
using ZLogger.Formatters;

namespace beamable.tooling.common;

public static class ZLoggerExtensions
{
    public static ZLoggerOptions UseBeamServiceJsonFormatter(this ZLoggerOptions options)
    {
        return options.UseJsonFormatter(x =>
        {
            // these settings mirror what the default Serilog settings did in CLI 4.x
            //  but in CLI 5, we migrated to ZLogger. For compat reasons, we want
            //  the log settings to be as similar as possible. 
            x.UseUtcTimestamp = true;
            x.IncludeProperties = IncludeProperties.LogLevel 
                                  | IncludeProperties.Message
                                  | IncludeProperties.Timestamp 
                                  | IncludeProperties.Exception
                ;
            x.JsonPropertyNames = JsonPropertyNames.Default with
            {
                LogLevel = JsonEncodedText.Encode("__l"), 
                Message = JsonEncodedText.Encode("__m"), 
                Timestamp = JsonEncodedText.Encode("__t"), 
                Exception = JsonEncodedText.Encode("__e"), 
            };
        });
    }
}