using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Common.Inventory;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using Beamable.Server.Api.RealmConfig;
using Beamable.Server.Content;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests;

[Microservice(nameof(EagerContentStartupService), EnableEagerContentLoading = true)]
public class EagerContentStartupService : Microservice
{
    internal static readonly ConcurrentQueue<string> InitializationSteps = new();

    [InitializeServices]
    public static void Initialize(IServiceInitializer initializer) => InitializationSteps.Enqueue("user initialization");
}

[TestFixture, NonParallelizable]
public class EagerContentReconnectTests : CommonTest
{
    public enum DisconnectDuring { None, ManifestRequest, ItemDownload }

    [TestCase(DisconnectDuring.None)]
    [TestCase(DisconnectDuring.ManifestRequest)]
    [TestCase(DisconnectDuring.ItemDownload)]
    [TimeoutWithTeardown(45_000)]
    public async Task StartupFinishesEagerPreloadAndInitializersOnce(DisconnectDuring disconnect)
    {
        var steps = EagerContentStartupService.InitializationSteps;
        steps.Clear();
        var exits = new ConcurrentQueue<int>();
        var sockets = new ConcurrentQueue<TestSocket>();
        var manifestRequests = new ConcurrentQueue<TestSocket>();
        var blocked = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseItem = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var manifest = new ContentManifest
        {
            id = "global",
            references = new List<ContentReference>
            {
                new() { id = "items.foo", uri = "items.foo.json", version = "1", visibility = "public" }
            }
        };
        var provider = new TestSocketProvider(socket =>
        {
            var firstConnection = sockets.IsEmpty;
            sockets.Enqueue(socket);
            SocketRecoveryTests.AddReconnectHandlers(socket);
            socket.AddMessageHandler(MessageMatcher.WithRouteContains("realms/config").WithGet(),
                MessageResponder.Success(new GetRealmConfigResponse { config = new() }), MessageFrequency.OnlyOnce());
            socket.AddMessageHandler(MessageMatcher.WithRouteContains("basic/content/manifest").WithGet(),
                MessageResponder.Custom(request =>
                {
                    manifestRequests.Enqueue(socket);
                    if (firstConnection && disconnect == DisconnectDuring.ManifestRequest)
                    {
                        blocked.TrySetResult(true);
                        return null;
                    }
                    return request.Succeed(manifest);
                }), MessageFrequency.AtLeast(0));
        });
        var content = new TestContentResolver(async uri =>
        {
            if (disconnect == DisconnectDuring.ItemDownload)
            {
                blocked.TrySetResult(true);
                await releaseItem.Task;
            }
            var item = new ItemContent();
            item.SetContentName("foo");
            steps.Enqueue(uri);
            return new MicroserviceContentSerializer().Serialize(item);
        });
        var setup = new TestSetup(provider, content);
        var previousExit = BeamableMicroService.ExitProcess;
        BeamableMicroService.ExitProcess = exits.Enqueue;
        allowErrorLogs = disconnect != DisconnectDuring.None;
        try
        {
            var startup = setup.Start<EagerContentStartupService>(configurator: builder =>
            {
                builder.RemoveIfExists<IRealmConfigService>();
                builder.AddSingleton<IRealmConfigService, RealmConfigService>();
            }, perServiceInitializer: _ =>
            {
                steps.Enqueue("per-service setup");
                return Task.CompletedTask;
            });

            if (disconnect != DisconnectDuring.None)
            {
                await blocked.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.That(setup.HasInitialized, Is.False);
                Assert.That(steps, Is.Empty);
                sockets.First().Fault();
                releaseItem.TrySetResult(true);
            }

            await startup.WaitAsync(TimeSpan.FromSeconds(25));
            Assert.That(setup.HasInitialized, Is.True);
            Assert.That(sockets.Count, Is.EqualTo(disconnect == DisconnectDuring.None ? 1 : 2));
            Assert.That(steps, Is.EqualTo(new[] { "items.foo.json", "user initialization", "per-service setup" }));

            // Once ready, reconnect refreshes the manifest but must not repeat the eager work or hooks.
            var serving = sockets.Last();
            serving.Fault();
            Assert.That(sockets.Last(), Is.Not.SameAs(serving));
            Assert.That(manifestRequests, Is.EqualTo(sockets.ToArray()));
            Assert.That(steps.Count, Is.EqualTo(3));
            Assert.That(exits, Is.Empty);

            var realm = setup.Service.Provider.GetService<IRealmConfigService>();
            var configChanged = typeof(RealmConfigService).GetField("ConfigChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            var subscribers = (Delegate)configChanged.GetValue(realm);
            Assert.That(subscribers.GetInvocationList(), Has.Length.EqualTo(1));
        }
        finally
        {
            releaseItem.TrySetResult(true);
            try
            {
                if (setup.Service != null) await setup.OnShutdown(this, null);
            }
            finally
            {
                BeamableMicroService.ExitProcess = previousExit;
            }
        }
    }
}
