
using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using Beamable.Editor.Dotnet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Beamable.Editor.BeamCli.Commands
{
	public partial class BeamCommands : IBeamableDisposable
	{
		private readonly IBeamableRequester _requester;
		private IBeamCommandFactory _factory;
		public BeamArgs defaultBeamArgs;

		public BeamCommands(IBeamableRequester requester, BeamCommandFactory factory)
		{
			_requester = requester;
			_factory = factory;
			defaultBeamArgs = ConstructDefaultArgs();
		}

		private BeamArgs ConstructDefaultArgs()
		{
			string cid = null;
			string pid = null;
			try
			{
				cid = _requester.Cid;
				pid = _requester.Pid;
			}
			catch
			{
				// if there is no cid or pid, oh well.
			}
			var beamArgs = new BeamArgs
			{
				cid = cid,
				pid = pid,
				host = BeamableEnvironment.ApiUrl,
				refreshToken = _requester?.AccessToken?.RefreshToken,
				log = "Information",
				reporterUseFatal = true,
				skipStandaloneValidation = true,
				dotnetPath = DotnetUtil.DotnetPath
			};
			return beamArgs;
		}

		public BeamCommands SetBeamArgs(BeamArgs args)
		{
			defaultBeamArgs = args;
			return this;
		}

		public Promise OnDispose()
		{
			_factory.ClearAll();
			return Promise.Success;
		}
	}
}

namespace Beamable.Editor.BeamCli
{

	public class BeamCommandFactory : IBeamCommandFactory
	{
		private readonly BeamableDispatcher _dispatcher;

		[SerializeField]
		public BeamCommandPidCollection PidCollection = new BeamCommandPidCollection();

		public BeamCommandFactory(BeamableDispatcher dispatcher)
		{
			_dispatcher = dispatcher;

			AssemblyReloadEvents.beforeAssemblyReload -= ClearAll;
			AssemblyReloadEvents.afterAssemblyReload -= ClearAll;
			AssemblyReloadEvents.beforeAssemblyReload += ClearAll;
			AssemblyReloadEvents.afterAssemblyReload += ClearAll;
			EditorApplication.wantsToQuit += () =>
			{
				ClearAll();
				return true;
			};
		}

		public IBeamCommand Create()
		{
			var command = new BeamCommand(_dispatcher, PidCollection);
			return command;
		}

		public void ClearAll()
		{
			PidCollection.ClearAll();
		}
	}


	[Serializable]
	public class BeamCommandPidCollection
	{
		public List<int> pids = new List<int>();
		public void Add(int pid)
		{
			pids.Add(pid);
		}

		public void Remove(int pid)
		{
			pids.Remove(pid);
		}

		public void ClearAll()
		{
			foreach (var pid in pids)
			{
				try
				{
					Process.GetProcessById(pid)?.Kill();
				}
				catch
				{
					// unable to kill process
				}
			}
			pids.Clear();
		}
	}

	public class BeamCommand : IBeamCommand
	{
		private readonly BeamableDispatcher _dispatcher;
		private readonly BeamCommandPidCollection _collection;
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

		private string _messageBuffer = string.Empty;
		private bool _isMessageInProgress;

		// private Dictionary<string, List<Action<string>>

		private List<ReportDataPointDescription> _points = new List<ReportDataPointDescription>();
		private Action<ReportDataPointDescription> _callbacks = (_) => { };
		public BeamCommand(BeamableDispatcher dispatcher, BeamCommandPidCollection collection = null)
		{
			_dispatcher = dispatcher;
			_collection = collection;
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

			_messageBuffer += message;
			if (!_isMessageInProgress)
			{
				var startIndex = _messageBuffer.IndexOf(Reporting.PATTERN_START, StringComparison.Ordinal);
				if (startIndex >= 0)
				{
					_isMessageInProgress = true;
					_messageBuffer = _messageBuffer.Substring(startIndex + Reporting.PATTERN_START.Length);
				}
			}
			
			if (_isMessageInProgress)
			{
				var endPatternIndex = _messageBuffer.IndexOf(Reporting.PATTERN_END, StringComparison.Ordinal);
				if (endPatternIndex >= 0)
				{
					_isMessageInProgress = false;
					var found = _messageBuffer.Substring(0, endPatternIndex);
					_messageBuffer = _messageBuffer.Substring(endPatternIndex + Reporting.PATTERN_END.Length);
					// Debug.LogWarning(found);
					try
					{
						var pt = JsonUtility.FromJson<ReportDataPointDescription>(found);
						if (pt != null)
						{
							pt.json = found;
							_points.Add(pt);
							_callbacks?.Invoke(pt);
						}
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}
				}
			}
		}

		private void ProcessStandardErr(string data)
		{
			if (string.IsNullOrWhiteSpace(data)) return;
			if (!AutoLogErrors) return;
			Debug.LogError(data);
		}

		public void SetCommand(string command)
		{
			var beamLocation = BeamCliUtil.CLI_PATH;

#if UNITY_EDITOR_WIN
			beamLocation = $"\"{Path.GetFullPath(beamLocation)}\"";
#endif

			Command = beamLocation + command.Substring("beam".Length);
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

					var pid = 0;
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
									Debug.LogException(ex);
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
									Debug.LogException(ex);
								}
							});
						};

						_process.Start();
						pid = _process.Id;
						_collection?.Add(pid);
						// _started = true;
						_process.BeginOutputReadLine();
						_process.BeginErrorReadLine();

						await _status.Task;

						if (_exitCode != 0)
						{
							throw new Exception($"Cli failed: {_command}");
						}
					}
					finally
					{
						_process.Exited -= eh;
						_collection?.Remove(pid);
					}

				}
			}
			catch (Exception)
			{
				// Debug.LogException(e);
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
				Debug.LogWarning($"Unable to kill beamCLI process. This <i>may</i> mean that there are pending beamCLI tasks on your machine. \n command=[{_command}]");
			}

		}


	}
}
