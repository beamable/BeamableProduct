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
    public partial class CliContentService
    {
        private SemaphoreSlim _watcherGate;
        private SemaphoreSlim WatcherGate => _watcherGate ??= new SemaphoreSlim(1, 1);
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
                var local = _contentConfiguration.EnableLocalContentInEditor;
                var contentMatches = _contentWatchRequested
                    ? _contentWatcher != null && _contentWatcherLocalMode == local
                    : _contentWatcher == null;
                var historyMatches = _historyWatchRequested
                    ? _contentHistoryWatcher != null && _historyWatcherLocalMode == local
                    : _contentHistoryWatcher == null;
                if (contentMatches && historyMatches &&
                    _retiringContentCommand == null && _retiringHistoryCommand == null) return;
                await Task.WhenAll(StopContentWatcher(preserveRequest: true), StopHistoryWatcher());
                if (_watchersDisposed) return;
                // Read intent and mode after shutdown: closes and rapid toggles may arrive while waiting.
                if (_historyWatchRequested) StartContentHistoryCore();
                if (_contentWatchRequested) ready = await StartContentWatcher();
            }
            finally { WatcherGate.Release(); }
            await ready;
        }

        private async Task StopContentWatcher(bool preserveRequest = false)
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
            if (command == null) return;
            if (_contentStop == null || _contentStop.IsFaulted || _contentStop.IsCanceled)
                _contentStop = StopWatcher(command, _contentWatcherCompletion);
            await _contentStop;
            if (ReferenceEquals(_retiringContentCommand, command)) _retiringContentCommand = null;
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
                _retiringContentCommand, _retiringHistoryCommand };
            _contentWatcher = null;
            _contentHistoryWatcher = null;
            _retiringContentCommand = _retiringHistoryCommand = null;
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
            try { await Task.WhenAll(StopContentWatcher(), StopHistoryWatcher()); }
            finally { WatcherGate.Release(); }
        }
    }
}
