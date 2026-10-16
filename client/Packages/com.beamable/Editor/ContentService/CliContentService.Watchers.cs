using System;
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
        private bool _restartContentAfterModeSwitch;
        private bool _restartHistoryAfterModeSwitch;
        private bool _observedLocalContentMode;
        private bool _contentWatcherLocalMode;
        private bool _historyWatcherLocalMode;
        private bool _watchersDisposed;

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
                if (!_restartContentAfterModeSwitch && !_restartHistoryAfterModeSwitch &&
                    _retiringContentCommand == null && _retiringHistoryCommand == null &&
                    (_contentWatcher == null || _contentWatcherLocalMode == local) &&
                    (_contentHistoryWatcher == null || _historyWatcherLocalMode == local)) return;
                // Keep restart intent if only one shutdown succeeds before the other fails.
                _restartContentAfterModeSwitch |= _contentWatcher != null || _retiringContentCommand != null;
                _restartHistoryAfterModeSwitch |= _contentHistoryWatcher != null || _retiringHistoryCommand != null;
                await Task.WhenAll(StopContentWatcher(), StopHistoryWatcher());
                if (_watchersDisposed) return;
                // Read the setting after shutdown: rapid changes use the latest requested mode.
                if (_restartHistoryAfterModeSwitch) StartContentHistoryCore();
                if (_restartContentAfterModeSwitch) ready = await StartContentWatcher();
                _restartHistoryAfterModeSwitch = false;
                _restartContentAfterModeSwitch = false;
            }
            finally { WatcherGate.Release(); }
            await ready;
        }

        private async Task StopContentWatcher()
        {
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

        public async Promise OnDispose()
        {
            _watchersDisposed = true;
            EditorApplication.update -= CheckLocalContentMode;
            await WatcherGate.WaitAsync();
            try { await Task.WhenAll(StopContentWatcher(), StopHistoryWatcher()); }
            finally { WatcherGate.Release(); }
        }
    }
}
