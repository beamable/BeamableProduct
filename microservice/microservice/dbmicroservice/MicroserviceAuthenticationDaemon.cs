using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api;
using Serilog;
using System.Collections.Generic;

namespace Beamable.Server;

/// <summary>
/// This class manages our authentication process between this C#MS and our Beamo service. It provides the following properties:
/// - Only a single <see cref="MicroserviceRequester.Request{T}"/> task can trigger an authentication process.
/// - <see cref="Authenticate"/> will only run from a single thread even if many <see cref="MicroserviceRequester.Request{T}"/> fail due to <see cref="UnauthenticatedException"/> simultaneously.
/// - All requests that come in while we <see cref="Authenticate"/> is running are waited to finish before this thread goes to sleep again.
/// </summary>
public class MicroserviceAuthenticationDaemon
{
	/// <summary>
	/// The <see cref="EventWaitHandle"/> we use to wake this thread up from its slumber so we can authenticate the C#MS with the Beamo service.
	/// </summary>
	public readonly EventWaitHandle AUTH_THREAD_WAIT_HANDLE = new ManualResetEvent(false);

	/// <summary>
	/// The total number of outgoing requests that actually go out through <see cref="MicroserviceRequester.Request{T}"/>.
	/// </summary>
	private ulong _OutgoingRequestCounter = 0;

	/// <summary>
	/// The total number of outgoing requests that went out through <see cref="MicroserviceRequester.Request{T}"/> and whose promise handlers (for error or success) have run.
	/// </summary>
	private ulong _OutgoingRequestProcessedCounter = 0;

	/// <summary>
	/// Bumps the <see cref="_OutgoingRequestCounter"/>. Here mostly so people are reminded of reading the comments on this class 😁
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BumpRequestCounter() => Interlocked.Increment(ref _OutgoingRequestCounter);

	/// <summary>
	/// Bumps the <see cref="_OutgoingRequestProcessedCounter"/>. Here mostly so people are reminded of reading the comments on this class 😁
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BumpRequestProcessedCounter() => Interlocked.Increment(ref _OutgoingRequestProcessedCounter);

	/// <summary>
	/// Increments the given <see cref="authCounter"/> and notifies the <see cref="AUTH_THREAD_WAIT_HANDLE"/> so that this thread wakes up.
	/// The auth counter is used to ensure that, if this thread gets woken up without the need to be authorized for some unknown reason, we don't bother running the <see cref="Authenticate"/> task.
	/// </summary>
	/// <param name="authCounter"><see cref="SocketRequesterContext.AuthorizationCounter"/> is what you should pass here.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WakeAuthThread()
	{
		Interlocked.Increment(ref AuthorizationCounter);
		Log.Debug($"Authorization Daemon is being requested. Requests=[{AuthorizationCounter}]");
		AUTH_THREAD_WAIT_HANDLE.Set();
	}

	/// <summary>
	/// Cancels the token and notifies the <see cref="AUTH_THREAD_WAIT_HANDLE"/> so that this thread wakes up and catches fire 🔥.
	/// </summary>
	/// <param name="cancellation"><see cref="BeamableMicroService._serviceShutdownTokenSource"/> is what you should pass here.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void KillAuthThread()
	{
		_tokenSource.Cancel();
		AUTH_THREAD_WAIT_HANDLE.Set();
	}

	/// <summary>
	/// The environment data that we need to make the <see cref="Authenticate"/> request.
	/// </summary>
	private readonly IMicroserviceArgs _env;

	/// <summary>
	/// The requester instance so that we can make the <see cref="Authenticate"/> request.
	/// </summary>
	private readonly MicroserviceRequester _requester;

	/// <summary>
	/// Tracks the number of requests that failed due to <see cref="UnauthenticatedException"/>.
	/// </summary>
	public int AuthorizationCounter = 0; // https://stackoverflow.com/questions/29411961/c-sharp-and-thread-safety-of-a-bool

	private CancellationTokenSource _tokenSource;

	private MicroserviceAuthenticationDaemon(IMicroserviceArgs env, MicroserviceRequester requester)
	{
		_env = env;
		_requester = requester;
	}

	private async Task Run(CancellationTokenSource cancellationTokenSource)
	{
		_tokenSource = cancellationTokenSource;
		// While this thread isn't cancelled...
		while (!cancellationTokenSource.IsCancellationRequested)
		{
			Log.Verbose($"Waiting at Thread ID = {Environment.CurrentManagedThreadId}");
			// Wait for it to be woken up via the Wait Handle. When it is woken up, it'll run the logic for us to [re]-auth with Beamo and then go back to sleep.
			AUTH_THREAD_WAIT_HANDLE.WaitOne();
			if (cancellationTokenSource.IsCancellationRequested)
			{
				Log.Verbose($"Authorization Daemon has been cancelled. At ThreadID = {Environment.CurrentManagedThreadId}");
				return;
			}

			Log.Verbose($"Authorization Daemon has been woken. At ThreadID = {Environment.CurrentManagedThreadId}");

			// Gets the number of requests that have been made by the service so far...
			var outgoingReqsCountAtStart = Interlocked.Read(ref _OutgoingRequestCounter);

			// Gets the number of requests that have been made by the service AND that have had their promise handlers run (for errors or success)
			var outgoingReqsProcessedAtStart = Interlocked.Read(ref _OutgoingRequestProcessedCounter);

			// Declare a variable that'll hold the total number of requests that have been made by the service AFTER we run authenticate.
			ulong outgoingReqsCountAtEnd;
			try
			{
				// If we need to run authenticate --- let's do that and reset the counter so that all request tasks waiting for auth get released.
				Log.Verbose($"Authorization Daemon checking for pending requests. At ThreadID = {Environment.CurrentManagedThreadId}, Requests=[{AuthorizationCounter}]");
				if (AuthorizationCounter > 0)
				{
					// Do the authorization back and forth with Beamo
					await Authenticate();

					// Resets the auth counter back to 0
					Log.Verbose($"Authorization Daemon clearing pending requests. At ThreadID = {Environment.CurrentManagedThreadId}");
					Interlocked.Exchange(ref AuthorizationCounter, 0);
				}
			}
			catch (Exception ex)
			{
				BeamableLogger.LogError("Authorization failed.");
				BeamableLogger.LogException(ex);
			}
			finally
			{
				// Get the total number of Outgoing Requests after we finished the authentication process
				outgoingReqsCountAtEnd = Interlocked.Read(ref _OutgoingRequestCounter);
			}

			// Wait until all requests that can fail, have actually failed or succeeded by waiting until the number of requests we've attempted since we started the auth process
			// is the same or above the number of made requests that have been processed.
			bool stillProcessingPotentiallyFailedReqs;
			do
			{
				var expectedReqsProcessed = outgoingReqsProcessedAtStart + (outgoingReqsCountAtEnd - outgoingReqsCountAtStart);
				stillProcessingPotentiallyFailedReqs = expectedReqsProcessed > Interlocked.Read(ref _OutgoingRequestProcessedCounter);
				await Task.Delay(1);
			} while (stillProcessingPotentiallyFailedReqs);

			// This solves an extremely unlikely race condition
			Log.Verbose($"Authorization Daemon clearing pending requests and waiting for call. At ThreadID = {Environment.CurrentManagedThreadId}");
			Interlocked.Exchange(ref AuthorizationCounter, 0);
			AUTH_THREAD_WAIT_HANDLE.Reset();
		}
	}

	private async Task Authenticate()
	{
		string CalculateSignature(string text)
		{
			System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
			byte[] data = Encoding.UTF8.GetBytes(text);
			byte[] hash = md5.ComputeHash(data);
			return Convert.ToBase64String(hash);
		}

		Log.Debug($"Authorizing WS connection at ThreadID = {Thread.CurrentThread.ManagedThreadId}");
		var res = await _requester.Request<MicroserviceNonceResponse>(Method.GET, "gateway/nonce");
		Log.Debug($"Got nonce ThreadID at = {Thread.CurrentThread.ManagedThreadId}");
		var sig = CalculateSignature(_env.Secret + res.nonce);
		var req = new MicroserviceAuthRequest { cid = _env.CustomerID, pid = _env.ProjectName, signature = sig };
		var authRes = await _requester.Request<MicroserviceAuthResponse>(Method.POST, "gateway/auth", req);
		if (!string.Equals("ok", authRes.result))
		{
			Log.Error("Authorization failed. result=[{result}]", authRes.result);
			throw new Exception("Authorization failed");
		}

		Log.Debug($"Authorization complete at ThreadID = {Thread.CurrentThread.ManagedThreadId}");
	}

	/// <summary>
	/// Kick off a long running task that will make sure the given <see cref="socketContext"/> is authenticated.
	/// The daemon is running in a loop, checking the <see cref="SocketRequesterContext.AuthorizationCounter"/> field.
	/// When it is positive, the daemon will start ONE authorization flow, and then set the value to zero.
	/// </summary>
	/// <param name="env"></param>
	/// <param name="requester"></param>
	/// <param name="socketContext"></param>
	/// <param name="cancellationTokenSource"></param>
	/// <returns>A task that completes the loop after the given <see cref="cancellationTokenSource"/> has requested a cancel</returns>
	public static (Task, MicroserviceAuthenticationDaemon) Start(
		IMicroserviceArgs env, MicroserviceRequester requester,
		CancellationTokenSource cancellationTokenSource)
	{
		var daemon = new MicroserviceAuthenticationDaemon(env, requester);
		return (new TaskFactory().StartNew(() => daemon.Run(cancellationTokenSource), cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default), daemon);
	}
}
