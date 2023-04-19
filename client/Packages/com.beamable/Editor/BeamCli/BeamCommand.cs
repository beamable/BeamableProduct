
// using System.Diagnostics;

using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.BeamCli;
using Beamable.Editor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Beamable.Editor.BeamCli.Commands
{
	public partial class BeamCommands
	{
		private readonly IBeamableRequester _requester;
		private IBeamCommandFactory _factory;
		public BeamArgs defaultBeamArgs;

		public BeamCommands(IBeamableRequester requester, BeamableDispatcher dispatcher)
		{
			_requester = requester;
			_factory = new BeamCommandFactory(dispatcher);
			defaultBeamArgs = ConstructDefaultArgs();
		}

		public BeamArgs ConstructDefaultArgs()
		{
			var beamArgs = new BeamArgs
			{
				cid = _requester.Cid,
				pid = _requester.Pid,
				host = BeamableEnvironment.ApiUrl,
				refreshToken = _requester.AccessToken.RefreshToken,
				log = "Information",
				reporterUseFatal = true
			};
			return beamArgs;
		}

		public BeamCommands SetBeamArgs(BeamArgs args)
		{
			defaultBeamArgs = args;
			return this;
		}
	}

}

namespace Beamable.Editor.BeamCli
{
	public static class BeamCommandExtensions
	{
		// public static void Handle<TChannel, TData>(this IBeamCommandResultStream<TChannel, TData> self, Action<ReportDataPoint<TData>> cb)
		// 	where TChannel : IResultChannel, new()
		// {
		// 	var channel = new TChannel(); // TODO: cache
		// 	// self.On(channel.ChannelName, cb);
		// }
	}

	public class BeamCommandFactory : IBeamCommandFactory
	{
		private readonly BeamableDispatcher _dispatcher;

		public BeamCommandFactory(BeamableDispatcher dispatcher)
		{
			_dispatcher = dispatcher;
		}

		public IBeamCommand Create() => new BeamCommand(_dispatcher);
	}

	public class BeamCommand : IBeamCommand
	{
		private readonly BeamableDispatcher _dispatcher;


		private string _command;

		public string Command
		{
			get => _command;
			set
			{
				if (_hasExecuted) throw new InvalidOperationException("cannot set command after running");
				_command = value;
			}
		}

		private Process _process;
		protected virtual bool CaptureStandardBuffers => true;
		public bool AutoLogErrors { get; set; } = true;
		private TaskCompletionSource<int> _status, _standardOutComplete;

		// private bool _hasExited;
		protected int _exitCode = -1;
		private bool _hasExecuted;

		private string messageBuffer = "";
		private bool isMessageInProgress;

		// private Dictionary<string, List<Action<string>>

		private List<ReportDataPointDescription> _points = new List<ReportDataPointDescription>();
		private Action<ReportDataPointDescription> _callbacks = (_) => { };
		public BeamCommand(BeamableDispatcher dispatcher)
		{
			_dispatcher = dispatcher;
		}

		public IBeamCommand On<T>(string type, Action<ReportDataPoint<T>> cb)
		{
			On(desc =>
			{
				if (desc.type != type) return;
				var pt = JsonUtility.FromJson<ReportDataPoint<T>>(desc.json);
				cb?.Invoke(pt);
			});

			return this;
		}

		public IBeamCommand On(Action<ReportDataPointDescription> cb)
		{
			_callbacks += cb;
			return this;
		}

		private void ProcessStandardOut(string message)
		{
			if (message == null) return;

			messageBuffer += message;

			// Debug.LogWarning(message);
			if (!isMessageInProgress)
			{
				var startIndex = messageBuffer.IndexOf(Reporting.PATTERN_START, StringComparison.Ordinal);
				if (startIndex >= 0)
				{
					isMessageInProgress = true;
					messageBuffer = messageBuffer.Substring(startIndex + Reporting.PATTERN_START.Length);
				}
			}
			else if (isMessageInProgress)
			{
				var startIndex = messageBuffer.IndexOf(Reporting.PATTERN_END, StringComparison.Ordinal);
				if (startIndex >= 0)
				{
					isMessageInProgress = false;
					var found = messageBuffer.Substring(0, startIndex);
					messageBuffer = messageBuffer.Substring(startIndex + Reporting.PATTERN_END.Length);
					// Debug.LogWarning(found);

					var pt = JsonUtility.FromJson<ReportDataPointDescription>(found);
					if (pt != null)
					{
						pt.json = found;
						_points.Add(pt);
						_callbacks?.Invoke(pt);
					}
				}
			}
		}

		private void ProcessStandardErr(string data)
		{
			if (data == null) return;
			if (!AutoLogErrors) return;
			Debug.LogError(data);
		}

		public void SetCommand(string command)
		{
			Command = command;
		}

		public async Promise Run()
		{
			if (string.IsNullOrEmpty(Command)) throw new InvalidOperationException("must set command before running");
			_hasExecuted = true;
			try
			{

				using (_process = new System.Diagnostics.Process())
				{
#if UNITY_EDITOR && !UNITY_EDITOR_WIN
               _process.StartInfo.FileName = "sh";
               _process.StartInfo.Arguments = $"-c '{Command}'";
#else
					_process.StartInfo.FileName = "cmd.exe";
					_process.StartInfo.Arguments = $"/C {_command}"; //  "/C " + _command + " > " + commandoutputfile + "'"; // TODO: I haven't tested this since refactor.
#endif
					// Configure the process using the StartInfo properties.
					_process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
					_process.EnableRaisingEvents = true;
					_process.StartInfo.RedirectStandardInput = true;
					_process.StartInfo.RedirectStandardOutput = CaptureStandardBuffers;
					_process.StartInfo.RedirectStandardError = CaptureStandardBuffers;
					_process.StartInfo.CreateNoWindow = true;
					_process.StartInfo.UseShellExecute = false;
					// ModifyStartInfo(_process.StartInfo);

					_status = new TaskCompletionSource<int>();
					_standardOutComplete = new TaskCompletionSource<int>();
					EventHandler eh = (s, e) =>
					{
						Task.Run(async () =>
					   {
						   await Task.Delay(1); // give 1 ms for log messages to eep out
						   _dispatcher.Schedule(() =>
						  {
							  // there still may pending log lines, so we need to make sure they get processed before claiming the process is complete
							  // _hasExited = true;
							  _exitCode = _process.ExitCode;

							  // OnExit?.Invoke(_process.ExitCode);
							  // HandleOnExit();

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
							_dispatcher.Schedule(() =>
							{
								try
								{
									ProcessStandardOut(args.Data);
								}
								catch (Exception ex)
								{
									Debug.LogException(ex);
								}
							});
						};
						_process.ErrorDataReceived += (sender, args) =>
						{
							_dispatcher.Schedule(() =>
							{
								try
								{
									ProcessStandardErr(args.Data);
								}
								catch (Exception ex)
								{
									Debug.LogException(ex);
								}
							});
						};

						_process.Start();
						// _started = true;
						_process.BeginOutputReadLine();
						_process.BeginErrorReadLine();

						await _status.Task;

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
			catch (Exception)
			{
				// Debug.LogException(e);
				throw;
			}
		}


	}
}
