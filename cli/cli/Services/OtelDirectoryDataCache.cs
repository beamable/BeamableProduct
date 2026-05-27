using Beamable.Common.BeamCli.Contracts;
using cli.OtelCommands;
using cli.Utils;

namespace cli.Services;

public class OtelDirectoryDataCache : IDisposable
{
	public OtelFileStatus LatestOtelFileStatus => _latestOtelFileStatus;
	
    private readonly string _path;
    private readonly string _lastPublishedFilePath;
    private readonly FileSystemWatcher _watcher;
    private readonly SemaphoreSlim _recalcLock = new(1, 1);
    private readonly TimeSpan _debounceInterval;

    private DirectoryInfoUtils _cached;
    private volatile bool _isDirty = true;
    private CancellationTokenSource? _debounceCts;
    private OtelFileStatus _latestOtelFileStatus = new OtelFileStatus();

    public OtelDirectoryDataCache(string path)
    {
        _path = path;
        _debounceInterval = TimeSpan.FromMilliseconds(200);

        _cached = new DirectoryInfoUtils();
        _lastPublishedFilePath = Path.Join(_path, PushTelemetryCommand.LAST_PUBLISH_OTEL_FILE_NAME);
        _isDirty = true;
        _watcher = new FileSystemWatcher(path)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName
                           | NotifyFilters.DirectoryName
                           | NotifyFilters.Size,
            InternalBufferSize = 64 * 1024  // 64 KB — default is 8 KB
        };

        _watcher.Created += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += OnChanged;
        _watcher.Changed += OnChanged;
        _watcher.Error += OnError;
        _watcher.EnableRaisingEvents = true;
        
    }
    

    /// <summary>
    /// Returns the cached size. If the cache is dirty, recalculates first.
    /// Multiple concurrent callers will wait on a single recalculation.
    /// </summary>
    public async Task RecalculateSize(
        CancellationToken ct = default)
    {
        if (!_isDirty)
            return;

        await _recalcLock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock — another thread may
            // have recalculated while we were waiting.
            if (!_isDirty)
                return;

            _cached = await Task.Run(
	            () => DirectoryUtils.CalculateDirectorySize(_path), ct);
            _latestOtelFileStatus.FileCount = _cached.FileCount;
            _latestOtelFileStatus.FolderSize = _cached.Size;
            _latestOtelFileStatus.LastPublishTimestamp = await GetLastPublishTimestamp(ct);
            _isDirty = false;
        }
        finally
        {
            _recalcLock.Release();
        }
    }

    async Task<long> GetLastPublishTimestamp(CancellationToken cancellationToken)
    {
	    if (!File.Exists(_lastPublishedFilePath))
	    {
		    return 0;
	    }

	    string fileText = await File.ReadAllTextAsync(_lastPublishedFilePath, cancellationToken);
	    long.TryParse(fileText, out long output);
	    return output;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce: reset the timer on every event.
        // Only mark dirty after the burst settles.
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();

        var token = _debounceCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_debounceInterval, token);
                _isDirty = true;  // mark stale — next GetSizeAsync recalculates
            }
            catch (TaskCanceledException)
            {
                // Another event came in, debounce restarted — this is fine.
            }
        }, token);
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        // Buffer overflow or other watcher failure.
        // The only safe recovery is a full recalculation.
        _isDirty = true;
    }

    public void Dispose()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _recalcLock.Dispose();
    }
}
