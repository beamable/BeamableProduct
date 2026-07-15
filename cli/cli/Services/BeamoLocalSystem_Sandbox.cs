using System;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Server;
using Beamable.Server.Api.Notifications;
using cli.Services.Sandbox;

namespace cli.Services;

/// <summary>
/// Sandbox-specific extensions to <see cref="BeamoLocalSystem"/>. Mirrors the
/// portal-extension partial: builds a <see cref="BeamServer"/> hosting a single
/// <see cref="SandboxDiscoveryService"/> in-process, registers it under the
/// <c>BeamSandbox_&lt;accountId&gt;_&lt;guid&gt;</c> service name, and runs until
/// <see cref="SandboxObserver.ShutdownSignal"/> fires or the caller's
/// <see cref="CancellationToken"/> cancels.
/// </summary>
public partial class BeamoLocalSystem
{
	/// <summary>
	/// Spins up the in-process sandbox MS and runs it until shut down. Returns when
	/// the shutdown signal fires (via the Shutdown route, deposition, or token
	/// cancellation). After this returns, the caller should clean up local state and
	/// let the process exit.
	/// </summary>
	public async Task RunLocalSandbox(SandboxObserver observer, IInvocationRunner runner, CancellationToken token = default)
	{
		// The sandbox class has no [Microservice] attribute — its name is configured
		// dynamically via OverrideConfig — so the framework's attribute resource check
		// must be disabled, exactly as the portal-extension flow does.
		Environment.SetEnvironmentVariable("BEAM_ALLOW_STARTUP_WITHOUT_ATTRIBUTES_RESOURCE", "true");

		observer.BindRunner(runner);

		// Background loop that pumps the notification batcher → IMicroserviceNotificationsApi
		// every 100 ms. The batcher itself enforces the at-most-one-per-second cadence; this
		// loop just gives it the tick it needs to find out the window has elapsed.
		using var flushLoopCts = CancellationTokenSource.CreateLinkedTokenSource(token);
		var flushLoop = Task.Run(() => PumpBatcherLoop(observer, flushLoopCts.Token));

		// RunForever() awaits Task.Delay(-1) once the MS is up — it never completes on
		// its own. We capture the task so we can race it against the shutdown signal
		// or external cancellation.
		var serverTask = BeamServer
			.Create()
			.InitializeServices(provider =>
			{
				// Once the framework provider is built, the notifications API is available.
				// Bind it to the observer so the flush loop can ship batches.
				var notifications = provider.GetService<IMicroserviceNotificationsApi>();
				observer.BindNotifications(notifications);
			})
			.ConfigureServices(deps =>
			{
				deps.AddSingleton(observer);
			})
			.IncludeRoutes<SandboxDiscoveryService>(routePrefix: "")
			.OverrideConfig(microserviceConfig =>
			{
				microserviceConfig.Attributes = new DefaultMicroserviceAttributes
				{
					MicroserviceName = observer.ServiceName,
					ServiceType = "sandbox",
				};
			})
			.RunForever();

		var cancellationTask = token.CanBeCanceled
			? WaitForCancellation(token)
			: Task.Delay(Timeout.Infinite, CancellationToken.None);

		await Task.WhenAny(serverTask, observer.ShutdownSignal, cancellationTask);

		if (!serverTask.IsCompleted)
		{
			// Give the gateway a moment to flush the in-flight Shutdown 200 (or any other
			// terminal response) back to Portal before the process unwinds.
			try { await Task.Delay(TimeSpan.FromMilliseconds(200), token); }
			catch (TaskCanceledException) { /* cancellation during the grace window is fine */ }

			// Force-flush anything still in the batcher so late events (e.g. a final
			// invocation-status emitted right before shutdown) ship before unwind.
			observer.FlushPending();
		}

		flushLoopCts.Cancel();
		try { await flushLoop; } catch { /* shutting down — best effort */ }

		// File watchers hold OS handles; dispose explicitly on host exit so they don't
		// linger past the sandbox process.
		observer.Dispose();
	}

	private static async Task PumpBatcherLoop(SandboxObserver observer, CancellationToken token)
	{
		try
		{
			while (!token.IsCancellationRequested)
			{
				observer.PumpBatcher();
				await Task.Delay(100, token);
			}
		}
		catch (TaskCanceledException) { /* expected on shutdown */ }
	}

	private static async Task WaitForCancellation(CancellationToken token)
	{
		try { await Task.Delay(Timeout.Infinite, token); }
		catch (TaskCanceledException) { /* expected when the token cancels */ }
	}
}
