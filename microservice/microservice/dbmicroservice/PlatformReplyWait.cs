using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;

namespace Beamable.Server;

internal sealed class StartupPlatformReplyTimeoutException : TimeoutException
{
    internal StartupPlatformReplyTimeoutException(string step, TimeSpan limit, RequestTimeoutException requestTimeout)
        : base(requestTimeout == null
            ? $"Startup exhausted its {limit.TotalSeconds:0.##}s platform reply budget waiting for {step}."
            : $"Startup received no reply for {step} before its outbound request deadline.", requestTimeout) { }
}

/// <summary>One authenticated attempt. Provider and event registration can wait concurrently.</summary>
internal sealed class PlatformReplyWait
{
    private readonly CancellationToken _connectionLost;
    private readonly TimeSpan? _limit;
    private readonly Stopwatch _waiting = new();
    private int _pending;
    private readonly TaskCompletionSource<StartupPlatformReplyTimeoutException> _expired =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal PlatformReplyWait(CancellationToken connectionLost, TimeSpan? limit)
    {
        _connectionLost = connectionLost;
        _limit = limit;
    }

    internal async Task<T> Wait<T>(string step, Func<Promise<T>> send)
    {
        _connectionLost.ThrowIfCancellationRequested();
        if (!_limit.HasValue) return await AsTask(send()).WaitAsync(_connectionLost);

        TimeSpan remaining;
        lock (_waiting)
        {
            if (_pending++ == 0) _waiting.Start();
            remaining = _limit.Value - _waiting.Elapsed;
        }
        try
        {
            if (remaining <= TimeSpan.Zero) throw Expire(step);
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(_connectionLost);
            lifetime.CancelAfter(remaining);
            return await WaitForStartupReply(step, send, lifetime.Token);
        }
        finally
        {
            lock (_waiting)
            {
                if (--_pending == 0) _waiting.Stop();
            }
        }
    }

    internal async Task<T> WaitForStartupReply<T>(string step, Func<Promise<T>> send, CancellationToken lifetime)
    {
        try
        {
            // A shared config client may send this request on a serving sibling's socket.
            // End that listener as well as our wait, without closing the sending socket.
            var reply = MicroserviceRequester.WithRequestLifetime(lifetime, send);
            var value = await AsTask(reply).WaitAsync(lifetime);
            lifetime.ThrowIfCancellationRequested();
            return value;
        }
        catch (OperationCanceledException error) when (error.CancellationToken == lifetime &&
            lifetime.IsCancellationRequested && !_connectionLost.IsCancellationRequested)
        {
            throw Expire(step);
        }
        catch (RequestTimeoutException error) when (!_connectionLost.IsCancellationRequested)
        {
            // With enforcement on, this particular reply can expire before the shared budget.
            throw Expire(step, error);
        }
    }

    // Custom code can delegate to the built-in registration and handle its exception. The coordinator
    // still owns readiness and must observe that expiry even if the hook continues running.
    internal async Task WaitForSubscription(Task hook)
    {
        if (_limit.HasValue)
        {
            await Task.WhenAny(hook, _expired.Task).WaitAsync(_connectionLost);
            if (_expired.Task.IsCompletedSuccessfully) throw await _expired.Task;
        }
        await hook.WaitAsync(_connectionLost);
    }

    private StartupPlatformReplyTimeoutException Expire(string step, RequestTimeoutException requestTimeout = null)
    {
        var error = new StartupPlatformReplyTimeoutException(step, _limit.Value, requestTimeout);
        _expired.TrySetResult(error);
        return error;
    }

    private static async Task<T> AsTask<T>(Promise<T> reply) => await reply;
}
