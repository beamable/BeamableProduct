using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api.Content;
using Beamable.Common.Dependencies;
using Beamable.Common.Inventory;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using Beamable.Server.Api.RealmConfig;
using Beamable.Server.Content;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests;

[TestFixture, NonParallelizable]
public class EagerManifestReplyTests : CommonTest
{
    [Microservice(nameof(EagerManifestService), EnableEagerContentLoading = true)]
    public class EagerManifestService : Microservice
    {
        public static int Initializations;

        [InitializeServices]
        public static void Initialize(IServiceInitializer _) => Interlocked.Increment(ref Initializations);
    }

    private readonly ConcurrentQueue<int> _exits = new();
    private Action<int> _previousExit;
    private TimeSpan _previousBudget;
    private Promise<IContentApi> _previousContent;
    private TestSocket _socket;

    [SetUp]
    public void SetStartupBudget()
    {
        _previousExit = BeamableMicroService.ExitProcess;
        _previousBudget = BeamableMicroService.StartupPlatformReplyTimeout;
        _previousContent = ContentApi.Instance;
        ContentApi.Instance = new Promise<IContentApi>();
        BeamableMicroService.StartupPlatformReplyTimeout = TimeSpan.FromSeconds(1);
        BeamableMicroService.ExitProcess = _exits.Enqueue;
        _exits.Clear();
        EagerManifestService.Initializations = 0;
        allowErrorLogs = true;
    }

    [TearDown]
    public void RestoreRuntimeSettings()
    {
        BeamableMicroService.ExitProcess = _previousExit;
        BeamableMicroService.StartupPlatformReplyTimeout = _previousBudget;
        ContentApi.Instance = _previousContent;
    }

    private TestSetup SetupWithManifest(TestSocketResponseGenerator respond, IContentResolver resolver = null)
    {
        return new TestSetup(new TestSocketProvider(socket =>
        {
            _socket = socket;
            SocketRecoveryTests.AddReconnectHandlers(socket);
            socket.AddMessageHandler(MessageMatcher.WithRouteContains("basic/content/manifest").WithGet(),
                respond, MessageFrequency.AtLeast(0));
        }), resolver);
    }

    // A custom config client can begin fetching the manifest before eager loading joins its pending promise.
    private sealed class PrefetchingRealmConfig : IRealmConfigService
    {
        private readonly ContentService _content;
        public PrefetchingRealmConfig(ContentService content) => _content = content;
        public Promise<RealmConfig> GetRealmConfigSettings()
        {
            _ = _content.GetManifest();
            return new MockRealmConfig().GetRealmConfigSettings();
        }

        public void UpdateLogLevel() { }
    }

    [TestCase(false)]
    [TestCase(true)]
    [TimeoutWithTeardown(15_000)]
    public async Task MissingManifestReplyEndsStartupAndOnlyCancelsTheListenerItOwns(bool alreadyFetching)
    {
        var held = new TaskCompletionSource<WebsocketResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = 0;
        var setup = SetupWithManifest(request =>
        {
            Interlocked.Increment(ref requests);
            held.TrySetResult(request);
            return null;
        });
        var start = setup.Start<EagerManifestService>(configurator: builder =>
        {
            if (!alreadyFetching) return;
            builder.RemoveIfExists<IRealmConfigService>();
            builder.AddSingleton<IRealmConfigService>(p => new PrefetchingRealmConfig(p.GetService<ContentService>()));
        });
        var request = await held.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var context = ((BeamableMicroService)setup.Service).SocketContext;
        var error = Assert.CatchAsync(async () => await start.WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.That(error.GetBaseException(), Is.TypeOf<StartupPlatformReplyTimeoutException>());
        Assert.That(_exits, Is.EqualTo(new[] { 12 }));
        Assert.That(requests, Is.EqualTo(1));
        Assert.That(await SocketRecoveryTests.Eventually(
            () => context.TryGetListener(request.id, out _) == alreadyFetching, TimeSpan.FromSeconds(2)), Is.True);
        _socket.SendToClient(request.Succeed(new ContentManifest { id = "global", references = new() }));
        Assert.That(await SocketRecoveryTests.Eventually(
            () => !context.TryGetListener(request.id, out _), TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(setup.HasInitialized, Is.False);
        Assert.That(EagerManifestService.Initializations, Is.Zero);
    }

    [Test]
    [TimeoutWithTeardown(15_000)]
    public void Gateway504RepliesExhaustManifestRetriesAndPreserveTheGatewayError()
    {
        BeamableMicroService.StartupPlatformReplyTimeout = TimeSpan.FromSeconds(8);
        var requests = 0;
        var setup = SetupWithManifest(request =>
        {
            Interlocked.Increment(ref requests);
            return request.Timeout();
        });
        var error = Assert.CatchAsync(async () => await setup.Start<EagerManifestService>().WaitAsync(TimeSpan.FromSeconds(10)));

        var cause = error.GetBaseException();
        Assert.That(cause, Is.TypeOf<WebsocketRequesterException>());
        Assert.That(((WebsocketRequesterException)cause).Status, Is.EqualTo(504));
        lock (setup.StartupLogs.allLogs)
        {
            Assert.That(setup.StartupLogs.allLogs.Any(entry => entry.LogInfo.LogLevel == LogLevel.Critical &&
                entry.ToString().Contains(cause.Message)), Is.True, "The fatal startup log should identify the failed manifest request.");
        }
        Assert.That(requests, Is.EqualTo(3));
        Assert.That(_exits, Is.EqualTo(new[] { 12 }));
        Assert.That(setup.HasInitialized, Is.False);
    }

    [Test]
    [TimeoutWithTeardown(15_000)]
    public async Task ItemDownloadCanOutlastTheManifestReplyBudget()
    {
        var downloading = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var item = new ItemContent();
        item.SetContentName("foo");
        var resolver = new TestContentResolver(async _ =>
        {
            downloading.TrySetResult(true);
            await release.Task;
            return new MicroserviceContentSerializer().Serialize(item);
        });
        var manifest = new ContentManifest
        {
            id = "global",
            references = new List<ContentReference>
            {
                new() { id = "items.foo", uri = "items.foo.json", version = "1", visibility = "public" }
            }
        };
        var setup = SetupWithManifest(MessageResponder.Success(manifest), resolver);
        var start = setup.Start<EagerManifestService>();
        try
        {
            await downloading.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Task.Delay(BeamableMicroService.StartupPlatformReplyTimeout + TimeSpan.FromMilliseconds(300));
            Assert.That(start.IsCompleted, Is.False);
            release.TrySetResult(true);
            await start.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(EagerManifestService.Initializations, Is.EqualTo(1));
            Assert.That(_exits, Is.Empty);
        }
        finally
        {
            release.TrySetResult(true);
            if (setup.HasInitialized) await setup.OnShutdown(this, null);
        }
    }
}
