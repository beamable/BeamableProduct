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
                if ((_contentWatcher == null || _contentWatcherLocalMode == local) &&
                    (_contentHistoryWatcher == null || _historyWatcherLocalMode == local)) return;
                var contentWasActive = _contentWatcher != null;
                var historyWasActive = _contentHistoryWatcher != null;
                await Task.WhenAll(StopContentWatcher(), StopHistoryWatcher());
                if (_watchersDisposed) return;
                // Read the setting after shutdown: rapid changes use the latest requested mode.
                if (historyWasActive) StartContentHistoryCore();
                if (contentWasActive) ready = await StartContentWatcher();
            }
            finally { WatcherGate.Release(); }
            await ready;
        }

        private Task StopContentWatcher()
        {
            var watcher = _contentWatcher;
            _contentWatcher = null; // Reject callbacks from the retiring command.
            return watcher == null ? _contentStop ?? Task.CompletedTask :
                _contentStop = StopWatcher(watcher.Command, _contentWatcherCompletion);
        }

        private Task StopHistoryWatcher()
        {
            var watcher = _contentHistoryWatcher;
            _contentHistoryWatcher = null;
            return watcher == null ? _historyStop ?? Task.CompletedTask :
                _historyStop = StopWatcher(watcher.Command, _historyWatcherCompletion);
        }

        private static async Task StopWatcher(IBeamCommand command, Task completion)
        {
            if (command is BeamWebCommand web) await web.StopAsync();
            else command.Cancel();
            if (completion == null) return;
            try { await completion; }
            catch (OperationCanceledException) { }
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
