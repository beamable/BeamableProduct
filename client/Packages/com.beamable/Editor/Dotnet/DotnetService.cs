using Beamable.Common;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Beamable.Editor.Dotnet
{
	public class DotnetService
	{
		private readonly BeamableDispatcher _dispatcher;

		public DotnetService(BeamableDispatcher dispatcher)
		{
			_dispatcher = dispatcher;
		}

		public string DotnetPath => DotnetUtil.DotnetPath;

		private Process _process;
		protected virtual bool CaptureStandardBuffers => true;
		public bool AutoLogErrors { get; set; } = true;
		private TaskCompletionSource<int> _status, _standardOutComplete;

		protected int _exitCode = -1;

		private void ProcessStandardOut(string message)
		{
			if (message == null) return;
			Debug.Log(message);
		}

		private void ProcessStandardErr(string data)
		{
			if (data == null) return;
			if (!AutoLogErrors) return;
			UnityEngine.Debug.LogError(data);
		}

		public async Promise Run(string command)
		{
			if (string.IsNullOrEmpty(command)) throw new InvalidOperationException("must set command before running");
			try
			{

				using (_process = new System.Diagnostics.Process())
				{
					_process.StartInfo.FileName = DotnetPath;
					_process.StartInfo.Arguments = command;
					// Configure the process using the StartInfo properties.
					_process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
					_process.EnableRaisingEvents = true;
					_process.StartInfo.RedirectStandardInput = true;
					_process.StartInfo.RedirectStandardOutput = CaptureStandardBuffers;
					_process.StartInfo.RedirectStandardError = CaptureStandardBuffers;
					_process.StartInfo.CreateNoWindow = true;
					_process.StartInfo.UseShellExecute = false;

					_status = new TaskCompletionSource<int>();
					_standardOutComplete = new TaskCompletionSource<int>();
					EventHandler eh = (s, e) =>
					{
						Task.Run(async () =>
					   {
						   await Task.Delay(1); // give 1 ms for log messages to eep out
						   if (_dispatcher.IsForceStopped)
						   {
							   KillProc();
							   return;
						   }
						   _dispatcher.Schedule(() =>
						   {
							   _exitCode = _process.ExitCode;
							   _status.TrySetResult(0);
						   });
					   });
					};

					_process.Exited += eh;

					try
					{
						_process.EnableRaisingEvents = true;

						_process.OutputDataReceived += (sender, args) =>
						{
							if (_dispatcher.IsForceStopped)
							{
								KillProc();
								return;
							}
							_dispatcher.Schedule(() =>
							{
								try
								{
									ProcessStandardOut(args.Data);
								}
								catch (Exception ex)
								{
									UnityEngine.Debug.LogException(ex);
								}
							});
						};
						_process.ErrorDataReceived += (sender, args) =>
						{
							if (_dispatcher.IsForceStopped)
							{
								KillProc();
								return;
							}
							_dispatcher.Schedule(() =>
							{
								try
								{
									ProcessStandardErr(args.Data);
								}
								catch (Exception ex)
								{
									UnityEngine.Debug.LogException(ex);
								}
							});
						};

						_process.Start();
						_process.BeginOutputReadLine();
						_process.BeginErrorReadLine();

						var p = new Promise();

						await _status.Task;
						_dispatcher.Schedule(() =>
					   {
						   p.CompleteSuccess();
					   });
						await p;

						if (_exitCode != 0)
						{
							throw new Exception("Cli failed");
						}
					}
					finally
					{
						_process.Exited -= eh;
					}

				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				throw;
			}
		}

		private void KillProc()
		{
			if (_process.HasExited)
			{
				return;
			}

			try
			{
				_process.Kill();
			}
			catch
			{
				UnityEngine.Debug.LogWarning($"Unable to kill dotnet process. This <i>may</i> mean that there are pending dotnet tasks on your machine.");
			}

		}

	}
}