using System.Diagnostics;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace cli.Utils;


[Serializable]
public class SolutionLogs
{
    public Dictionary<string, ProjectLogs> projects = new Dictionary<string, ProjectLogs>();
}

[Serializable]
public class ProjectLogs
{
    public bool success;
    public string message;
    public List<CustomLogEvent> errors = new List<CustomLogEvent>();
    public List<CustomLogEvent> warnings = new List<CustomLogEvent>();
}

[Serializable]
[DebuggerDisplay("[{severity}] {message} - {file}:{lineNumber},{colNumber}")]
public class CustomLogEvent
{
    public string severity;
    public string message;
    public string code;
    public string file;
    public int lineNumber;
    public int colNumber;
    
}

public class MsBuildSolutionLogger : Logger
{
    public const string LOG_PATH_ENV_VAR = "BEAM_MSBUILD_LOG_PATH";
    
    private SolutionLogs logs = new SolutionLogs();
    private string _path;
    
    public override void Initialize(IEventSource eventSource)
    {
        eventSource.ErrorRaised += EventSourceOnErrorRaised;
        eventSource.WarningRaised += EventSourceOnWarningRaised;
        eventSource.ProjectFinished += EventSourceOnProjectFinished;
        _path = Environment.GetEnvironmentVariable(LOG_PATH_ENV_VAR);
        _path ??= "publishLogs.json";
        
    }


    public override void Shutdown()
    {
        var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions
        {
            IncludeFields = true,
            WriteIndented = true
        });
        File.WriteAllText(_path, json);
        base.Shutdown();
    }
    
    
    private void EventSourceOnProjectFinished(object sender, ProjectFinishedEventArgs e)
    {
        if (!logs.projects.TryGetValue(e.ProjectFile, out var project))
        {
            project = logs.projects[e.ProjectFile] = new ProjectLogs();
        }

        project.success = e.Succeeded;
        project.message = e.Message;
    }

    private void EventSourceOnWarningRaised(object sender, BuildWarningEventArgs e)
    {
        if (!logs.projects.TryGetValue(e.ProjectFile, out var project))
        {
            project = logs.projects[e.ProjectFile] = new ProjectLogs();
        }
        project.warnings.Add(new CustomLogEvent
        {
            code = e.Code,
            file = e.File,
            colNumber = e.ColumnNumber,
            lineNumber = e.LineNumber,
            message = e.Message,
            severity = "WARN"
        });
    }


    private void EventSourceOnErrorRaised(object sender, BuildErrorEventArgs e)
    {
        if (!logs.projects.TryGetValue(e.ProjectFile, out var project))
        {
            project = logs.projects[e.ProjectFile] = new ProjectLogs();
        }
        project.errors.Add(new CustomLogEvent
        {
            code = e.Code,
            file = e.File,
            colNumber = e.ColumnNumber,
            lineNumber = e.LineNumber,
            message = e.Message,
            severity = "ERR"
        });
    }
}