using System.CommandLine;
using Beamable.Server;
using cli.Utils;
using Docker.DotNet.Models;
using Spectre.Console;
using UnityEngine;

namespace cli.BackendCommands;

public class BackendPsCommandArgs : CommandArgs
{
    public bool watch;
    public string backendHome;
}

public class BackendPsCommandResults
{
    public BackendPsCoreStatus core;
}

public class BackendPsCoreStatus
{
    public List<BackendDockerContainerInfo> coreServices;
}

public class BackendDockerContainerInfo
{
    public string service;
    public string containerId;
    public bool isRunning;
}

public class BackendPsCommand 
    : AppCommand<BackendPsCommandArgs>
    , IResultSteam<DefaultStreamResultChannel, BackendPsCommandResults>
{
    public BackendPsCommand() : base("ps", "Get the current local state")
    {
    }

    public override void Configure()
    {
        AddOption(new Option<bool>(new string[] { "--watch", "-w" }, "Listen for changes to the state"),
            (args, i) => args.watch = i);
        
        BackendCommandGroup.AddBackendHomeOption(this, (args, i) => args.backendHome = i);
    }

    public override async Task Handle(BackendPsCommandArgs args)
    {
        var initialStatus = await CheckDockerStatus(args);
        
        Report(initialStatus);

        if (args.watch)
        {
            await ListenForLocalDocker(args, args.Lifecycle.CancellationToken, Report);
        }

        void Report(BackendPsCoreStatus coreStatus)
        {
            var status = new BackendPsCommandResults
            {
                core = coreStatus
            };
            this.SendResults(status);
            
            var table = new Table();
            table.Border(TableBorder.Simple);
            table.AddColumn("[bold]type[/]");
            table.AddColumn("[bold]name[/]");
            table.AddColumn("[bold]containerId[/]");
            // table.AddColumn("[bold]version[/]");
            // table.AddColumn("[bold]req count[/]");

            foreach (var coreService in coreStatus.coreServices)
            {
                if (!coreService.isRunning) continue;
                table.AddRow(
                    new Text("docker"), 
                    new Text(coreService.service), 
                    new Text(coreService.containerId?.Substring(0, 8) ?? "")
                );
            }
		
            AnsiConsole.Write(table);

            var missing = coreStatus.coreServices.Where(x => !x.isRunning).ToList();
            if (missing.Count > 0)
            {
                AnsiConsole.MarkupLine($"[bold red]Missing core services:[/]");
                AnsiConsole.MarkupLine($"[red] {string.Join(", ", missing.Select(x => x.service))}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[green]All core services are running[/]");
            }
        }
    }

    public static async Task ListenForLocalDocker(BackendPsCommandArgs args, CancellationToken ct, Action<BackendPsCoreStatus> onCoreChange)
    {
        
        var action = new Debouncer(TimeSpan.FromMilliseconds(250), async void () =>
        {
            try
            {
                var statusCheck = await CheckDockerStatus(args);
                onCoreChange?.Invoke(statusCheck);
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Failed to parse docker status");
            }
        });

        var task = args.BeamoLocalSystem.Client.System.MonitorEventsAsync(new ContainerEventsParameters
        {
            
        }, new Progress<Message>(DockerSystemEventHandler), ct);

        void DockerSystemEventHandler(Message message)
        {
            var type = message.Type;
            
            // this only cares about containers turning on or off.
            if (!string.Equals(type, "container", StringComparison.InvariantCultureIgnoreCase))
                return;
            
            action.Signal();
        }

        try
        {
            await task;
        }
        catch (TaskCanceledException)
        {
            // let it gooooo
            Log.Verbose("docker watch was cancelled.");
        }
        catch
        {
            throw;
        }
    }

    public static async Task<BackendPsCoreStatus> CheckDockerStatus(BackendPsCommandArgs args)
    {
        const string beamableLabel = "com.beamable.local";
        var containers = await args.BeamoLocalSystem.Client.Containers.ListContainersAsync(new ContainersListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["label"] = new Dictionary<string, bool>
                {
                    [beamableLabel] = true
                }
            }
        });

        var coreStatus = new BackendPsCoreStatus
        {
            coreServices = new List<BackendDockerContainerInfo>
            {
                new BackendDockerContainerInfo
                {
                    service = "broker"
                },
                new BackendDockerContainerInfo
                {
                    service = "redis"
                },
                new BackendDockerContainerInfo
                {
                    service = "mongo_router"
                },
                new BackendDockerContainerInfo
                {
                    service = "mongo_master01"
                },
                new BackendDockerContainerInfo
                {
                    service = "mongo_shard01a"
                },
                new BackendDockerContainerInfo
                {
                    service = "mongo_config"
                },
                new BackendDockerContainerInfo
                {
                    service = "mongo_master02"
                },
                new BackendDockerContainerInfo
                {
                    service = "mongo_shard02a"
                }
            }
        };
        
        
        const string serviceLabel = "com.docker.compose.service";
        foreach (var container in containers)
        {
            Log.Debug($"found container=[{container.ID}]");
            if (!container.Labels.TryGetValue(serviceLabel, out var service))
            {
                Log.Warning($"Found a docker container=[{container.ID}] with the label={beamableLabel}, but no {serviceLabel}.");
                continue;
            }

            var coreService = coreStatus.coreServices.FirstOrDefault(c => c.service == service);
            if (coreService == null)
            {
                Log.Warning($"Found a docker container=[{container.ID}] service=[{service}], but it does not match a known required service.");
                continue;
            }

            coreService.isRunning = true;
            coreService.containerId = container.ID;
            // maybe we need to extract other properties from docker?
        }

        return coreStatus;
    }
}