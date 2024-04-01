using Beamable.Common;
using Beamable.Editor.BeamCli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Beamable.Editor.Dotnet
{
	public class DotnetService
	{
		private readonly BeamableDispatcher _dispatcher;
		private List<string> _resultBuffer;

		public DotnetService(BeamableDispatcher dispatcher)
		{
			_dispatcher = dispatcher;
			_resultBuffer = new List<string>();
		}

		public string DotnetPath => Path.GetFullPath(DotnetUtil.DotnetPath);

		private Process _process;
		protected virtual bool CaptureStandardBuffers => true;
		public bool AutoLogErrors { get; set; } = true;
		private TaskCompletionSource<int> _status, _standardOutComplete;

		protected int _exitCode = -1;

		private bool _purposelyBeingExited;

		private void ProcessStandardOut(string message)
		{
			if (message == null) return;
			_resultBuffer.Add(message);
			Debug.Log(message);
		}

		private void ProcessStandardErr(string data)
		{
			if (data == null) return;
			if (!AutoLogErrors) return;
			UnityEngine.Debug.LogError(data);
		}

		public void SetPurposelyExit()
		{
			_purposelyBeingExited = true;
		}

		public async Promise<List<string>> Run(string command)
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
					_process.StartInfo.Environment.Add("DOTNET_CLI_UI_LANGUAGE", "en");
					_process.StartInfo.Environment.Add("BEAM_PATH", BeamCliUtil.CLI_PATH.Replace(".dll", ""));

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

						_resultBuffer.Clear();
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

						if (_exitCode != 0 && !_purposelyBeingExited)
						{
							_purposelyBeingExited = false;
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

			return _resultBuffer;
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
