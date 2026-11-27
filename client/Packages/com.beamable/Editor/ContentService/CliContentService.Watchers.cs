using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Editor.BeamCli;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.ContentService
{
    public enum ContentAutoSyncState { Enabled, Paused, Switching, Failed }

    public partial class CliContentService
    {
        private SemaphoreSlim _watcherGate;
        private SemaphoreSlim WatcherGate => _watcherGate ??= new SemaphoreSlim(1, 1);
        // Ordinary content ps can sync files before its first snapshot. Only advancing sync work renews this budget.
        private const double ContentStartupIdleSeconds = 30;
        internal const string ContentStartupTimeoutMessage = "Content watcher timed out waiting for its first snapshot. Refresh Content to retry.";
        // Keep the startup or shutdown error visible until the next attempt.
        internal string ContentWatcherError { get; private set; }
        internal Func<double> ContentWatcherClock;
        private double WatcherTime => ContentWatcherClock?.Invoke() ?? EditorApplication.timeSinceStartup;
        private TaskCompletionSource<bool> _ordinaryContentReady;
        private int _watcherStopTimeoutMs = 10000;
        private Task _contentWatcherCompletion;
        private Task _historyWatcherCompletion;
        private Task _contentStop = Task.CompletedTask;
        private Task _historyStop = Task.CompletedTask;
        private IBeamCommand _retiringContentCommand;
        private IBeamCommand _retiringHistoryCommand;
        // Requested activity is independent of active commands and pending cleanup.
        private bool _contentWatchRequested;
        private bool _historyWatchRequested;
        private bool _observedLocalContentMode;
        private bool _contentWatcherLocalMode;
        private bool _historyWatcherLocalMode;
        private bool _watchersDisposed;
        private bool _preparedForAssemblyReload;
        private Exception _localContentModeError;

        public ContentAutoSyncState AutoSyncState
        {
            get
            {
                if (_localModeExitCheck != null)
                    return _localModeExitStopFailed ? ContentAutoSyncState.Failed : ContentAutoSyncState.Switching;
                if (ContentWatcherError != null)
                    return ContentAutoSyncState.Failed;
                // A saved preference is not proof that the previous commands have stopped.
                if ((_retiringContentCommand != null && (_contentStop?.IsFaulted == true || _contentStop?.IsCanceled == true)) ||
                    (_retiringHistoryCommand != null && (_historyStop?.IsFaulted == true || _historyStop?.IsCanceled == true)))
                    return ContentAutoSyncState.Failed;
                if (_retiringContentCommand != null || _retiringHistoryCommand != null)
                    return ContentAutoSyncState.Switching;
                var local = _contentConfiguration.EnableLocalContentInEditor;
                if (WatchersMatchMode(local))
                    return local ? ContentAutoSyncState.Paused : ContentAutoSyncState.Enabled;
                if ((_localContentModeError != null && !isReloading) ||
                    (_contentWatcher != null && _contentWatcherCompletion?.IsCompleted == true) ||
                    (_historyWatchRequested && ContentHistoryWatcherError != null) ||
                    (_contentHistoryWatcher != null && _historyWatcherCompletion?.IsCompleted == true))
                    return ContentAutoSyncState.Failed;
                return ContentAutoSyncState.Switching;
            }
        }

        private bool WatchersMatchMode(bool local) =>
            (_contentWatchRequested
                ? _contentWatcher != null && _contentWatcherLocalMode == local && _contentWatcherCompletion?.IsCompleted != true
                : _contentWatcher == null) &&
            (_historyWatchRequested
                ? _contentHistoryWatcher != null && _historyWatcherLocalMode == local && _historyWatcherCompletion?.IsCompleted != true
                : _contentHistoryWatcher == null);

        // Stop commands while the dispatcher can still deliver their final callbacks.
        public int DisposeOrder => -1;

        private void CheckLocalContentMode()
        {
            var local = _contentConfiguration.EnableLocalContentInEditor;
            if (_watchersDisposed || local == _observedLocalContentMode) return;
            _observedLocalContentMode = local;
            ApplyLocalContentMode().Error(error => Debug.LogException(error));
        }

        /// <summary>Stops both old commands before restarting active watchers with the current policy.</summary>
        public async Promise ApplyLocalContentMode()
        {
            Task ready = Task.CompletedTask;
            await WatcherGate.WaitAsync();
            try
            {
                if (_watchersDisposed) return;
                _localContentModeError = null;
                await StopLocalModeExitCheck();
                var local = _contentConfiguration.EnableLocalContentInEditor;
                if (ContentWatcherError == null && WatchersMatchMode(local) && _retiringContentCommand == null && _retiringHistoryCommand == null) return;
                await Task.WhenAll(StopContentWatcher(preserveRequest: true), StopHistoryWatcher());
                if (_watchersDisposed) return;
                if (_historyWatchRequested) StartContentHistoryCore();
                if (_contentWatchRequested) ready = await StartContentWatcher();
            }
            catch (Exception error) { _localContentModeError = error; throw; }
            finally { WatcherGate.Release(); }
            await ready;
        }

        private Task BeginContentWatcherStop(bool preserveRequest)
        {
            if (!preserveRequest) _contentWatchRequested = false;
            var watcher = _contentWatcher;
            _contentWatcher = null; // Reject callbacks from the retiring command.
            if (watcher != null)
            {
                _retiringContentCommand = watcher.Command;
                _contentStop = null;
            }
            var command = _retiringContentCommand;
            if (command == null) return Task.CompletedTask;
            if (_contentStop == null || _contentStop.IsFaulted || _contentStop.IsCanceled)
                _contentStop = StopWatcher(command, _contentWatcherCompletion);
            return _contentStop;
        }

        private async Task StopContentWatcher(bool preserveRequest = false)
        {
            ContentWatcherError = null;
            var stop = BeginContentWatcherStop(preserveRequest);
            var command = _retiringContentCommand;
            var startup = _ordinaryContentReady;
            try
            {
                if (!_watchersDisposed && !stop.IsCompleted && startup != null && startup.Task.Status != TaskStatus.RanToCompletion)
                    await WaitForWatcherStop(stop, "Could not confirm content watcher shutdown. Refresh Content to retry.");
                else
                    await stop;
            }
            catch (Exception error)
            {
                if (ReferenceEquals(_retiringContentCommand, command) && ReferenceEquals(_contentStop, stop))
                {
                    ContentWatcherError = error.Message;
                    isReloading = false;
                }
                // Retirement disarms the first-snapshot timer. Release its caller even if the command never exits.
                startup?.TrySetException(error);
                throw;
            }
            if (ReferenceEquals(_retiringContentCommand, command)) _retiringContentCommand = null;
            if (command != null) startup?.TrySetResult(false);
        }

        // A timeout releases this caller. The owner keeps the original stop task until shutdown is acknowledged.
        private async Task WaitForWatcherStop(Task stop, string timeoutMessage)
        {
            _ = stop.ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
            using var timer = new CancellationTokenSource();
            try
            {
                var deadline = Task.Delay(_watcherStopTimeoutMs > 0 ? _watcherStopTimeoutMs : 10000, timer.Token);
                if (await Task.WhenAny(stop, deadline) != stop) throw new TimeoutException(timeoutMessage);
                await stop;
            }
            finally { timer.Cancel(); }
        }

        private async Task StopHistoryWatcher()
        {
            var watcher = _contentHistoryWatcher;
            _contentHistoryWatcher = null;
            if (watcher != null)
            {
                _retiringHistoryCommand = watcher.Command;
                _historyStop = null;
            }
            var command = _retiringHistoryCommand;
            if (command == null) return;
            if (_historyStop == null || _historyStop.IsFaulted || _historyStop.IsCanceled)
                _historyStop = StopWatcher(command, _historyWatcherCompletion);
            await _historyStop;
            if (ReferenceEquals(_retiringHistoryCommand, command)) _retiringHistoryCommand = null;
        }

        private static async Task StopWatcher(IBeamCommand command, Task completion)
        {
            if (command is BeamWebCommand web) await web.StopAsync();
            else command.Cancel();
            if (completion == null) return;
            try { await completion; }
            catch (OperationCanceledException) { }
            // StopAsync above already confirmed server cleanup and drained this failed run.
            catch (Exception) when (command is BeamWebCommand) { }
        }

        private static async Task AwaitCommand(Promise run) => await run;

        internal void PrepareForAssemblyReload()
        {
            if (_preparedForAssemblyReload) return;
            _preparedForAssemblyReload = true;
            _watchersDisposed = true;
            _contentWatchRequested = _historyWatchRequested = false;
            EditorApplication.update -= CheckLocalContentMode;
            var commands = new[] { _contentWatcher?.Command, _contentHistoryWatcher?.Command,
                _retiringContentCommand, _retiringHistoryCommand, _localModeExitCheck };
            _contentWatcher = null;
            _contentHistoryWatcher = null;
            _retiringContentCommand = _retiringHistoryCommand = null;
            ClearLocalModeExitCheck();
            CancelContentHistoryRequests();
            // Reload cannot pump the gate, Unity continuations, or dispatched callbacks.
            foreach (var command in commands.Distinct())
            {
                try
                {
                    if (command is BeamWebCommand web) web.StopForAssemblyReload();
                    else command?.Cancel();
                }
                catch (Exception error) { Debug.LogException(error); }
            }
        }

        public async Promise OnDispose()
        {
            if (BeamEditor.IsReloading) PrepareForAssemblyReload();
            if (_preparedForAssemblyReload) return;
            _watchersDisposed = true;
            _contentWatchRequested = false;
            _historyWatchRequested = false;
            EditorApplication.update -= CheckLocalContentMode;
            await WatcherGate.WaitAsync();
            try { await Task.WhenAll(StopContentWatcher(), StopHistoryWatcher(), StopLocalModeExitCheck()); }
            finally { WatcherGate.Release(); }
        }
    }
}
