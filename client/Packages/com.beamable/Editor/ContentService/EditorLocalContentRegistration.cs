using System;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Content;
using UnityEditor;

namespace Beamable.Editor.ContentService
{
    [BeamContextSystem]
    public static class EditorLocalContentRegistration
    {
        [RegisterBeamableDependencies(-999, RegistrationOrigin.RUNTIME)]
        public static void Register(IDependencyBuilder builder)
        {
            builder.RemoveIfExists<ILocalContentSource>();
            builder.AddSingleton<ILocalContentSource>(provider =>
                new EditorLocalContentSession(
                    ContentConfiguration.Instance,
                    provider.GetService<IRuntimeConfigProvider>(),
                    provider.GetService<ContentParameterProvider>().EnableLocalContentInEditor));
        }
    }

    /// <summary>
    /// Owns a read-only working-copy snapshot per runtime context and realm.
    /// Mode/namespace settings are captured at context creation; restart Play Mode
    /// to change them or capture newer authoring JSON. Does not control CLI sync.
    /// </summary>
    public sealed class EditorLocalContentSession : ILocalContentSource, IBeamableDisposable
    {
        private readonly bool _enabled;
        private readonly string _manifestId;
        private readonly float _manifestDelay;
        private readonly float _contentDelay;
        private readonly IRuntimeConfigProvider _runtimeConfig;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private EditorLocalContentSource _source;
        private string _directory, _cid, _pid;
        private bool _disposed;

        public EditorLocalContentSession(ContentConfiguration config, IRuntimeConfigProvider runtimeConfig, bool? enabledOverride = null)
        {
            _enabled = enabledOverride ?? config.EnableLocalContentInEditor;
            _manifestId = config.EditorManifestID;
            _manifestDelay = config.LocalContentManifestDelaySeconds?.HasValue == true
                ? config.LocalContentManifestDelaySeconds.Value : 0;
            _contentDelay = config.LocalContentReferenceDelaySeconds?.HasValue == true
                ? config.LocalContentReferenceDelaySeconds.Value : 0;
            _runtimeConfig = runtimeConfig;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
        }

        public bool HandlesManifest(string manifestId) =>
            _enabled && string.Equals(_manifestId, manifestId, StringComparison.Ordinal);

        public async Promise<ClientManifest> GetManifest(string manifestId, ContentQuery query = null)
        {
            var source = await GetSource(manifestId, _manifestDelay);
            return await source.GetManifest(manifestId, query);
        }

        public async Promise<IContentObject> GetContent(string contentId, Type contentType, string manifestId)
        {
            var source = await GetSource(manifestId, _contentDelay);
            return await source.GetContent(contentId, contentType, manifestId);
        }

        private async Promise<EditorLocalContentSource> GetSource(string manifestId, float delaySeconds)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(EditorLocalContentSession));
            if (!HandlesManifest(manifestId))
                throw new InvalidOperationException($"Local content does not handle namespace '{manifestId}'.");

            var editor = BeamEditorContext.Default;
            // Registration happens before editor configuration necessarily finishes loading.
            if (editor.BeamCli.latestConfig == null)
                await WaitForReady(editor.OnReady);
            await WaitForDelay(delaySeconds);
            if (_disposed) throw new ObjectDisposedException(nameof(EditorLocalContentSession));

            var directory = editor.BeamCli.latestConfig?.configPath;
            var cid = _runtimeConfig.Cid;
            var pid = _runtimeConfig.Pid;
            if (!string.Equals(cid, editor.BeamCli.Cid, StringComparison.Ordinal))
                throw new InvalidOperationException("The runtime customer does not match the editor's local workspace.");

            if (_source == null || directory != _directory || cid != _cid || pid != _pid)
            {
                _source?.Dispose();
                _source = new EditorLocalContentSource(true, directory, cid, pid, _manifestId);
                _directory = directory;
                _cid = cid;
                _pid = pid;
            }
            return _source;
        }

        private async Task WaitForReady(Promise ready)
        {
            if (ready == null)
                throw new InvalidOperationException("The editor context has not started loading configuration.");
            var completion = new TaskCompletionSource<bool>();
            using (_cancellation.Token.Register(() => completion.TrySetCanceled()))
            {
                _ = ready.Then(_ => completion.TrySetResult(true))
                    .Error(error => completion.TrySetException(error));
                await completion.Task;
            }
        }

        private Task WaitForDelay(float seconds)
        {
            return seconds > 0 && !float.IsInfinity(seconds) && !float.IsNaN(seconds)
                ? Task.Delay(TimeSpan.FromSeconds(Math.Min(seconds, 86400)), _cancellation.Token)
                : Task.CompletedTask;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) Dispose();
        }

        public Promise OnDispose()
        {
            Dispose();
            return Promise.Success;
        }

        private void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
            _cancellation.Cancel();
            _cancellation.Dispose();
            _source?.Dispose();
            _source = null;
        }
    }
}
