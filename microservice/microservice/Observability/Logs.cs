using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography.Xml;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace microservice.Observability;

public class Logs
{
    public static ILoggerFactory LogFactory;

    public static ILogger CreateLogger<T>()
    {
        return LogFactory.CreateLogger<T>();
    }
    
    public static void SetupZLogger(DefaultActivityProvider activityProvider)
    {
        LogFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddZLoggerLogProcessor((opts) => BuildOtelZLogger(opts, activityProvider));
            // builder.AddZLoggerConsole(config =>
            // {
            // });
        });
    }

    public static IAsyncLogProcessor BuildOtelZLogger(ZLoggerOptions opts, DefaultActivityProvider provider)
    {
        // TODO: handle opts? 
        return new OtelZLogProcessor(provider);
    }

    public class OtelZLogProcessor : IAsyncLogProcessor
    {
        private readonly DefaultActivityProvider _provider;
        public HttpClient _client;
        
        
        public OtelZLogProcessor(DefaultActivityProvider provider)
        {
            _provider = provider;
            _client = new HttpClient();
        }
        
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public static (string text, int level) GetSeverityText(LogLevel logLevel)
        {
            // https://opentelemetry.io/docs/specs/otel/logs/data-model/#field-severitynumber
            
            //  map the string names to the commonly used Beamable phrasings.
            switch (logLevel)
            {
                case LogLevel.Trace:
                    // some call it trace, we call it verbose
                    return ("verbose", 1);
                case LogLevel.Debug:
                    return ("debug", 5);
                case LogLevel.Information:
                    return ("info", 9);
                case LogLevel.Warning:
                    return ("warn", 13);
                case LogLevel.Error:
                    return ("error", 17);
                case LogLevel.Critical:
                    // some call it critical, we call it fatal
                    return ("fatal", 21);
                default:
                    return ("none", 100);
            }
        }
        
        public void Post(IZLoggerEntry log)
        {
            var msg = log.ToString();
            
            ulong nanoseconds = (ulong)(log.LogInfo.Timestamp.Utc.ToUnixTimeMilliseconds() * 1_000_000);
            nanoseconds += (ulong)(log.LogInfo.Timestamp.Utc.Ticks % TimeSpan.TicksPerMillisecond) * 100;
            
            var sigNoz = new SigNozLogMessage
            {
                timestamp = nanoseconds,
                message = msg,
                resources = new Dictionary<string, string>
                {
                    // https://signoz.io/docs/logs-management/features/logs-quick-filters/#service-name
                    ["service.name"] = _provider.ServiceName,
                    ["service.namespace"] = _provider.ServiceNamespace,
                    ["service.instance.id"] = _provider.ServiceId,
                    
                    // https://signoz.io/docs/logs-management/features/logs-quick-filters/#environment
                    ["deployment.environment"] = "Local",
                    
                    // https://signoz.io/docs/logs-management/features/logs-quick-filters/#hostname
                    ["host.name"] = "localhost"
                }
            };
            (sigNoz.severity, sigNoz.level) = GetSeverityText(log.LogInfo.LogLevel);
            
            
            for (var i = 0; i < log.ParameterCount; i++)
            {
                var parameterName = log.GetParameterKeyAsString(i);
                sigNoz.attributes[parameterName] = log.GetParameterValue(i)?.ToString();
            }

            var currentActivity = _provider.CurrentActivity.Value;
            if (currentActivity != null)
            {
                sigNoz.spanId = currentActivity.SpanId.ToString();
                sigNoz.traceId = currentActivity.TraceId.ToString();
            }
            
            // TODO: play with attributes and such
            
            var message = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:8082");
            message.Content = JsonContent.Create(new List<SigNozLogMessage>{sigNoz}, options: new JsonSerializerOptions
            {
                IncludeFields = true
            });
            // TODO: post the message to a channel, and have a separate consumer. 
            var _ = _client.SendAsync(message);
            
            Console.WriteLine(msg);
        }

        // public class SigNozLogRequest
        // {
        //     public List<SigNozLogMessage> 
        // }
        
        /// <summary>
        /// https://signoz.io/docs/userguide/send-logs-http/
        /// </summary>
        public class SigNozLogMessage
        {
            [JsonPropertyName("timestamp")]
            public ulong timestamp;
            
            [JsonPropertyName("severity_text")]
            public string severity;
            
            [JsonPropertyName("severity_number")]
            public int level;
            
            [JsonPropertyName("trace_id")]
            public string traceId;
            
            [JsonPropertyName("span_id")]
            public string spanId;
            
            [JsonPropertyName("message")]
            public string message;

            [JsonPropertyName("attributes")]
            public Dictionary<string, string> attributes = new Dictionary<string, string>();
            
            [JsonPropertyName("resources")]
            public Dictionary<string, string> resources = new Dictionary<string, string>();
        }
    }
}