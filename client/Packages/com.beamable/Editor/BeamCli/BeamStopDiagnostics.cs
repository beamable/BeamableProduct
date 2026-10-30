using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Beamable.Editor.BeamCli
{
    // Investigation only. Observes state; does not cancel, complete, or pump anything.
    internal sealed class BeamStopDiagnostics : IDisposable
    {
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly SynchronizationContext _context = SynchronizationContext.Current;
        private readonly int _mainThread = Thread.CurrentThread.ManagedThreadId;
        private readonly ConcurrentQueue<string> _lines = new ConcurrentQueue<string>();
        private int _writing;
        private readonly string _path;
        private readonly Timer _timer;
        private volatile string _phase = "entry";
        private volatile Task _http;
        private CancellationToken _timeoutToken;
        private volatile bool _hasToken;
        private int _disposed;

        internal static BeamStopDiagnostics Begin(string commandId)
        {
            try
            {
                var marker = Path.Combine(Directory.GetCurrentDirectory(), "beam-stop-probe-mode.txt");
                if (!File.Exists(marker)) return null;
                var mode = File.ReadAllText(marker).Trim();
                return new BeamStopDiagnostics(commandId, mode);
            }
            catch { return null; }
        }

        private BeamStopDiagnostics(string commandId, string mode)
        {
            var directory = Path.Combine(Directory.GetCurrentDirectory(), "diagnostic-output", mode);
            Directory.CreateDirectory(directory);
            _path = Path.Combine(directory, "stop-" + commandId + "-" + Guid.NewGuid().ToString("N") + ".log");
            Write("begin httpAssembly=" + typeof(HttpClient).Assembly.Location + " runtime=" + System.Environment.Version
                  + " capturedContext=" + ContextName(_context) + " mainThread=" + _mainThread);
            _timer = new Timer(_ => Flush(), null, 250, 250);
        }

        internal void AttachTimeout(CancellationToken token)
        {
            // Poll the actual token rather than adding a cancellation callback that could change callback order.
            _timeoutToken = token;
            _hasToken = true;
            Mark("deadline.scheduled");
        }

        internal void AttachHttp(Task task)
        {
            _http = task;
            Mark("ack.await");
        }

        internal void Mark(string phase)
        {
            _phase = phase;
            Snapshot();
        }

        private void Snapshot()
        {
            try
            {
                ThreadPool.GetAvailableThreads(out var workers, out var io);
                var queue = _context?.GetType().GetField("m_AsyncWorkQueue", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(_context) as ICollection;
                Write("phase=" + _phase + " cancelled=" + (_hasToken ? _timeoutToken.IsCancellationRequested.ToString() : "unset")
                      + " http=" + (_http == null ? "unset" : _http.Status.ToString())
                      + " capturedQueue=" + (queue == null ? "unknown" : queue.Count.ToString())
                      + " workers=" + workers + " io=" + io);
            }
            catch (Exception error) { Write("observer.error=" + error.GetType().Name); }
        }

        private void Write(string message)
        {
            try
            {
                var line = DateTime.UtcNow.ToString("O") + " elapsedMs=" + _clock.ElapsedMilliseconds
                           + " thread=" + Thread.CurrentThread.ManagedThreadId
                           + " context=" + ContextName(SynchronizationContext.Current) + " " + message + System.Environment.NewLine;
                _lines.Enqueue(line);
            }
            catch { /* A diagnostic write must never change command success/failure. */ }
        }

        private void Flush()
        {
            if (Interlocked.Exchange(ref _writing, 1) != 0) return;
            try
            {
                if (Volatile.Read(ref _disposed) == 0) Snapshot();
                while (_lines.TryDequeue(out var line)) File.AppendAllText(_path, line);
                if (Volatile.Read(ref _disposed) != 0) _timer?.Dispose();
            }
            catch { /* Diagnostic I/O never blocks the observed continuation. */ }
            finally { Volatile.Write(ref _writing, 0); }
        }

        private static string ContextName(SynchronizationContext context) => context == null ? "null"
            : context.GetType().FullName + "#" + RuntimeHelpers.GetHashCode(context);

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            Mark("stop.scope.exit");
            // The next observer tick flushes the final event and disposes its timer.
        }
    }
}
