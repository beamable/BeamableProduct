using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Beamable.Server;
using cli.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ZLogger;
using ZLogger.Providers;

namespace cli.AICommands;

public class McpCommandArgs : CommandArgs
{
    
}
public class McpCommand 
    : AppCommand<McpCommandArgs>
    , IStandaloneCommand
    , ISkipManifest
    
{
    public McpCommand() : base("mcp", "Start an MCP server using stdio for communication")
    {
    }

    public static void Test()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddMcpServer()
            ;

        var app = builder.Build();
        app.StartAsync();
        // var transport = new StreamableHttpServerTransport();
        // var server = McpServer.Create(transport, new McpServerOptions
        // {
        //
        // });
        // server.RunAsync();
    }

    public override void Configure()
    {
        
    }

    public override async Task Handle(McpCommandArgs args)
    {
        int Add(int a, int b)
        {
            Log.Information($"Running ai add method with {a} and {b}");
            return a + b;
        }
        var x = McpServerTool.Create(Add, new McpServerToolCreateOptions
        {
            Idempotent = true,
            Description = "adds two numbers together",
            Title = "beam_add",
            ReadOnly = false,
            Destructive = false,
            UseStructuredContent = true,
            SerializerOptions = new JsonSerializerOptions
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                IncludeFields = true
            },
            Name = "beam_add"
        });

        var tools = new List<McpServerTool>();
        var generatorContext = args.DependencyProvider.GetService<CliGenerator>().GetCliContext();
        generatorContext.Commands.Sort((a, b) => a.executionPath.CompareTo(b.executionPath));
        foreach (var descriptor in generatorContext.Commands)
        {
            if (descriptor == generatorContext.Root) continue;
            if (!(descriptor.command is IAppCommand appCommand)) continue;

            void Invoke()
            {
                // need to make an invocation context. 
                
            }
            
            var tool = McpServerTool.Create(Invoke, new McpServerToolCreateOptions
            {
                Name = descriptor.GetSlug("_"),
                Description = descriptor.command.Description,
                Idempotent = false,
                Title = descriptor.ExecutionPathAsCapitalizedStringWithoutBeam(),
                Destructive = true, 
                UseStructuredContent = true,
                OpenWorld = true, 
                ReadOnly = false,
                SchemaCreateOptions = new AIJsonSchemaCreateOptions
                {
                    
                }
            });
            tools.Add(tool);
        }

        var builder = Host.CreateApplicationBuilder();
        builder.Services
            .AddMcpServer()
            .WithTools(tools)
            .WithListToolsHandler((req, token) =>
            {
                var res = new ListToolsResult
                {
                    Tools = tools.Select(x => x.ProtocolTool).ToList()
                };
                return new ValueTask<ListToolsResult>(res);
            })
            .WithStdioServerTransport()
            
            ;
        builder.Logging.ClearProviders();
        builder.Logging.AddZLoggerConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });
        var app = builder.Build();
        await app.RunAsync();
        // host.ConfigureLogging(logging =>
        // {
        //     logging.ClearProviders();
        //     logging.AddZLoggerConsole(opts =>
        //     {
        //         opts.LogToStandardErrorThreshold = LogLevel.Trace;
        //     });
        // })
        // .ConfigureServices((services) =>
        // {
        //     services.AddMcpServer()
        //         .WithStdioServerTransport() // stdio transport
        //         .WithTools(x);
        // })
        // .RunConsoleAsync();
    }

}