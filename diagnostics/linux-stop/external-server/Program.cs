using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

if (args.Length != 2 || !int.TryParse(args[0], out var port) || port is < 1 or > 65535)
{
    Console.Error.WriteLine("Usage: BeamStopProbeServer <port> <control-directory>");
    return 2;
}

var control = Path.GetFullPath(args[1]);
Directory.CreateDirectory(control);
var retryStop = Environment.GetEnvironmentVariable("PROBE_RETRY_STOP") == "1";
var writeOutput = Environment.GetEnvironmentVariable("PROBE_WRITE_OUTPUT") != "0";
var awaitSignalObservation = false;
var stopStatus = int.TryParse(Environment.GetEnvironmentVariable("PROBE_STOP_STATUS"), out var status) ? status : 204;
var elapsed = Stopwatch.StartNew();
using var shutdown = new CancellationTokenSource();
using var listener = new HttpListener();
listener.Prefixes.Add($"http://127.0.0.1:{port}/");
Console.CancelKeyPress += (_, e) => { e.Cancel = true; shutdown.Cancel(); };
using var closeOnShutdown = shutdown.Token.Register(() => listener.Close());

void Log(string name, object? data = null)
{
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        utc = DateTime.UtcNow.ToString("O"), elapsedMs = elapsed.Elapsed.TotalMilliseconds,
        thread = Environment.CurrentManagedThreadId, pid = Environment.ProcessId,
        @event = name, data
    }));
}

void Signal(string name, object? data = null)
{
    var destination = Path.Combine(control, name + ".json");
    var temporary = destination + ".tmp";
    File.WriteAllText(temporary, JsonSerializer.Serialize(new
    {
        utc = DateTime.UtcNow.ToString("O"), elapsedMs = elapsed.Elapsed.TotalMilliseconds,
        pid = Environment.ProcessId, data
    }));
    File.Move(temporary, destination, overwrite: true);
    Log(name, data);
}

async Task Gate(string name)
{
    Log("gate-wait", new { gate = name });
    while (!File.Exists(Path.Combine(control, name)))
        await Task.Delay(10, shutdown.Token);
    Log("gate-open", new { gate = name });
}

async Task<string> ReadRequest(HttpListenerContext context, string phase)
{
    using var reader = new StreamReader(context.Request.InputStream);
    var body = await reader.ReadToEndAsync(shutdown.Token);
    using var json = JsonDocument.Parse(body);
    var requestId = json.RootElement.GetProperty("requestId").GetString()
        ?? throw new InvalidDataException("Missing requestId.");
    Signal(phase + "-received", new { requestId, path = context.Request.Url?.AbsolutePath });
    if (awaitSignalObservation) await Gate("observed-" + phase);
    return requestId;
}

var watchShutdown = Task.Run(async () =>
{
    try
    {
        while (!shutdown.IsCancellationRequested)
        {
            if (File.Exists(Path.Combine(control, "shutdown")))
            {
                Log("shutdown-requested");
                shutdown.Cancel();
                return;
            }
            await Task.Delay(10, shutdown.Token);
        }
    }
    catch (OperationCanceledException) { }
});

try
{
    listener.Start();
    Signal("ready", new { port, framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription });
    var execute = await listener.GetContextAsync();
    // Read after accept so a fixture's object initializer can publish its final settings.
    // The test adapter writes this file atomically before sending its first request.
    int[]? infoPids = null;
    var owner = "";
    var version = "";
    var configuration = Path.Combine(control, "config.json");
    if (File.Exists(configuration))
    {
        using var document = JsonDocument.Parse(File.ReadAllText(configuration));
        var config = document.RootElement;
        if (config.TryGetProperty("retryStop", out var retryValue)) retryStop = retryValue.GetBoolean();
        if (config.TryGetProperty("writeOutput", out var outputValue)) writeOutput = outputValue.GetBoolean();
        if (config.TryGetProperty("awaitSignalObservation", out var observationValue)) awaitSignalObservation = observationValue.GetBoolean();
        if (config.TryGetProperty("stopStatus", out var statusValue)) stopStatus = statusValue.GetInt32();
        if (config.TryGetProperty("infoPids", out var pids) && pids.ValueKind != JsonValueKind.Null)
            infoPids = pids.EnumerateArray().Select(value => value.GetInt32()).ToArray();
        if (config.TryGetProperty("owner", out var ownerValue)) owner = ownerValue.GetString() ?? "";
        if (config.TryGetProperty("version", out var versionValue)) version = versionValue.GetString() ?? "";
    }
    Log("config-applied", new { retryStop, writeOutput, stopStatus, infoPids, awaitSignalObservation });
    if (infoPids != null)
    {
        for (var i = 0; i < infoPids.Length; i++)
        {
            var info = i == 0 ? execute : await listener.GetContextAsync();
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { owner, version, pid = infoPids[i] }));
            await info.Response.OutputStream.WriteAsync(bytes);
            info.Response.Close();
            Signal("info-" + i + "-sent", new { pid = infoPids[i] });
        }
        Signal("complete", new { reason = "served-info" });
        return 0;
    }
    var requestId = await ReadRequest(execute, "execute");
    await Gate("allow-headers");
    execute.Response.Headers.Set("X-Beam-Request-Id", requestId);
    var ready = Encoding.UTF8.GetBytes(": ready\n");
    await execute.Response.OutputStream.WriteAsync(ready);
    await execute.Response.OutputStream.FlushAsync();
    Signal("headers-sent", new { requestId });

    var stop = await listener.GetContextAsync();
    var stopId = await ReadRequest(stop, "stop");
    await Gate("release-worker");
    if (writeOutput)
    {
        var output = Encoding.UTF8.GetBytes("data: {\"type\":\"test\",\"data\":{},\"ts\":1}\n");
        await execute.Response.OutputStream.WriteAsync(output);
    }
    execute.Response.Close();
    Signal("execute-closed", new { requestId });

    await Gate("allow-ack");
    stop.Response.StatusCode = stopStatus;
    stop.Response.Close();
    Signal("ack-sent", new { requestId = stopId, status = stopStatus });

    if (retryStop)
    {
        var retry = await listener.GetContextAsync();
        var retryId = await ReadRequest(retry, "retry");
        await Gate("allow-retry-ack");
        retry.Response.StatusCode = 204;
        retry.Response.Close();
        Signal("retry-ack-sent", new { requestId = retryId, status = 204 });
    }
    Signal("complete", new { reason = "served" });
    return 0;
}
catch (Exception error) when (shutdown.IsCancellationRequested)
{
    Signal("complete", new { reason = "shutdown", errorType = error.GetType().FullName });
    return 0;
}
catch (Exception error)
{
    Signal("fault", new { type = error.GetType().FullName, error.Message, error.StackTrace });
    return 1;
}
finally
{
    shutdown.Cancel();
    await watchShutdown;
}
