using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using Beamable.Editor.BeamCli;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beamable.Editor.Tests
{
    public class Issue4866CancellationReproTests
    {
        // Exercises production Cancel against the Windows cmd -> child topology used by Run.
        // The child is a harmless controlled PowerShell file writer, not dotnet/Beam CLI.
        [UnityTest]
        public IEnumerator CancelKillsShellButControlledChildCanStillWrite()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor) Assert.Ignore("Windows shell topology only.");
            string root = Path.Combine(Path.GetTempPath(), "beam4866-cancel-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string ready = Path.Combine(root, "ready");
            string release = Path.Combine(root, "release");
            string wrote = Path.Combine(root, "wrote");
            string escaped = root.Replace("'", "''");
            string script = "$r='" + escaped + "'; [IO.File]::WriteAllText((Join-Path $r 'ready'), [string]$PID); " +
                "$deadline=[DateTime]::UtcNow.AddSeconds(20); while(-not [IO.File]::Exists((Join-Path $r 'release')) -and [DateTime]::UtcNow -lt $deadline){Start-Sleep -Milliseconds 25}; " +
                "if([IO.File]::Exists((Join-Path $r 'release'))){[IO.File]::WriteAllText((Join-Path $r 'wrote'),'child wrote after cancellation')}";
            string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            Process shell = null;
            Process child = null;
            try
            {
                shell = Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/d /s /c powershell.exe -NoLogo -NoProfile -NonInteractive -EncodedCommand " + encoded,
                    UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
                });
                var clock = Stopwatch.StartNew();
                while (!File.Exists(ready) && clock.Elapsed.TotalSeconds < 10) yield return null;
                Assert.That(File.Exists(ready), Is.True, "Controlled child did not start.");
                child = Process.GetProcessById(int.Parse(File.ReadAllText(ready)));
                // Open and retain this owned child's process handle before cancellation/cleanup.
                Assert.That(child.Handle, Is.Not.EqualTo(IntPtr.Zero));
                Assert.That(child.ProcessName, Does.StartWith("powershell").IgnoreCase);
                Assert.That(shell.HasExited, Is.False);
                Assert.That(File.Exists(wrote), Is.False);
                new BeamCommand(null) { process = shell }.Cancel();
                clock.Restart();
                while (!shell.HasExited && clock.Elapsed.TotalSeconds < 5) yield return null;
                Assert.That(shell.HasExited, Is.True, "Production Cancel did not stop its shell.");
                Assert.That(child.HasExited, Is.False, "Hypothesis disproven: child already terminated.");
                File.WriteAllText(release, "release test-owned writer");
                clock.Restart();
                while (!File.Exists(wrote) && clock.Elapsed.TotalSeconds < 5) yield return null;
                Assert.That(File.Exists(wrote), Is.True, "Hypothesis disproven: child did not write after shell cancellation.");
                TestContext.WriteLine("CONFIRMED production BeamCommand.Cancel killed its cmd parent but an already-started controlled child wrote afterward. This tests shell cancellation, not actual CLI/network operations.");
            }
            finally
            {
                if (child != null) { try { if (!child.HasExited) { child.Kill(); child.WaitForExit(3000); } } finally { child.Dispose(); } }
                if (shell != null) { try { if (!shell.HasExited) { shell.Kill(); shell.WaitForExit(3000); } } finally { shell.Dispose(); } }
                foreach (var path in new[] { ready, release, wrote }) if (File.Exists(path)) File.Delete(path);
                if (Directory.Exists(root)) Directory.Delete(root);
            }
        }
    }
}


