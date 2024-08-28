using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using Beamable.Common.Spew;
using Beamable.Editor.Dotnet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

		public BeamCommands(IBeamableRequester requester, IBeamCommandFactory factory)
		{
			_requester = requester;
			_factory = factory;
			defaultBeamArgs = ConstructDefaultArgs();
		}

		public IBeamCommand CreateCustom(string args)
		{
			var command = _factory.Create();
			command.SetCommand("beam " + defaultBeamArgs.Serialize() + " " + args);
			return command;
		}

		public BeamArgs ConstructDefaultArgs()
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
				log = "Verbose",
				skipStandaloneValidation = true,
				dotnetPath = Path.GetFullPath(DotnetUtil.DotnetPath),
				quiet = true,
				noLogFile = true,
				raw = true,
				emitLogStreams = true
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

		public Process process;
		protected virtual bool CaptureStandardBuffers => true;
		public bool AutoLogErrors { get; set; } = false;
		private TaskCompletionSource<int> _status;

		protected int _exitCode = -1;
		private bool _hasExecuted;

		private string _messageBuffer = string.Empty;
		private bool _isMessageInProgress;

		// private Dictionary<string, List<Action<string>>

		private List<ReportDataPointDescription> _points = new List<ReportDataPointDescription>();
		private Action<ReportDataPointDescription> _callbacks = (_) => { };

		private List<ErrorOutput> _errors = new List<ErrorOutput>();

		public BeamCommand(BeamableDispatcher dispatcher, BeamCommandPidCollection collection = null)
		{
			_dispatcher = dispatcher;
			_collection = collection;

			On<ErrorOutput>("error", cb =>
			{
				// accumulate standard errors...
				_errors.Add(cb.data);
			});
		}

		public IBeamCommand OnError(Action<ReportDataPoint<ErrorOutput>> cb)
		{
			return On<ErrorOutput>("error", cb);
		}
		
		public IBeamCommand OnTerminate(Action<ReportDataPoint<EofOutput>> cb)
		{
			return On<EofOutput>("eof", cb);
		}

		public void Cancel()
		{
			try
			{
				if (process.HasExited)
				{
					Debug.Log("Already exited.");
					return;
				}

				Debug.Log("Killing beam process, " + _command);
				process.Kill();
			}
			catch
			{
			}
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

		public static bool CheckForData(ref string buffer, out ReportDataPointDescription serializedData, out string jsonRaw)
		{
			string[] data = buffer.Split(new string[] { Reporting.MESSAGE_DELIMITER }, StringSplitOptions.None);

			if (data.Length > 1)
			{
				try
				{
					serializedData = JsonUtility.FromJson<ReportDataPointDescription>(data[0]);
					var remainingData = data.Skip(1).ToArray();

					buffer = string.Join(Reporting.MESSAGE_DELIMITER, remainingData);
					jsonRaw = data[0];
					return true;
				}
				catch (Exception e) //this case we have full data but there is some error in the json
				{
					Debug.LogError(e.Message);
					serializedData = null;
					jsonRaw = string.Empty;
					return false;
				}
			}

			if (!buffer.Contains(Reporting.MESSAGE_DELIMITER))
			{
				try
				{
					serializedData = JsonUtility.FromJson<ReportDataPointDescription>(data[0]);
					buffer = string.Empty;
					jsonRaw = data[0];
					return true;
				}
				catch
				{
					// in this case, data is just incomplete, so we ignore
				}
			}
			else
			{
				try
				{
					serializedData = JsonUtility.FromJson<ReportDataPointDescription>(data[0]);
					buffer = string.Empty;
					jsonRaw = data[0];
					return true;
				}
				catch (Exception e) //in this case json is wrong
				{
					Debug.LogError(e.Message);
				}
			}

			serializedData = null;
			jsonRaw = string.Empty;
			return false;
		}

		private void ProcessStandardOut(string message)
		{
			if (string.IsNullOrEmpty(message)) return;

			_messageBuffer += message;

			if (CheckForData(ref _messageBuffer, out ReportDataPointDescription data, out string jsonRaw))
			{
				data.json = jsonRaw;
				_points.Add(data);
				_callbacks?.Invoke(data);
			}
		}

		private void ProcessStandardErr(string data)
		{
			if (string.IsNullOrWhiteSpace(data)) return;
			if (!AutoLogErrors) return;
			Debug.LogError(data);
		}

		public static string GetCommandPrefix()
		{
			var beamCli = BeamCliUtil.CLI;

#if UNITY_EDITOR_WIN
			beamCli = $"\"{beamCli}\"";
#endif

			if (!beamCli.Contains(".dll"))
			{
				beamCli = $"tool run {beamCli}";
			}

			return beamCli;
		}

		public void SetCommand(string command)
		{
			var prefix = GetCommandPrefix();

			Command = prefix + command.Substring("beam".Length);
		}

		public async Promise Run()
		{
			if (string.IsNullOrEmpty(Command)) throw new InvalidOperationException("must set command before running");
			_hasExecuted = true;

			
			
			using (process = new System.Diagnostics.Process())
			{
				process.StartInfo.FileName = DotnetUtil.DotnetPath;
				process.StartInfo.Arguments = _command;
				// Configure the process using the StartInfo properties.
				process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
				process.EnableRaisingEvents = true;
				process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.RedirectStandardOutput = CaptureStandardBuffers;
				process.StartInfo.RedirectStandardError = CaptureStandardBuffers;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;

				// prevent the beam CLI from saving any log information to file.
				process.StartInfo.Environment.Add("BEAM_CLI_NO_FILE_LOG", "1");

				process.StartInfo.EnvironmentVariables[Constants.EnvironmentVariables.BEAM_PATH] = GetCommandPrefix();
				process.StartInfo.EnvironmentVariables[Constants.EnvironmentVariables.BEAM_DOTNET_PATH] = Path.GetFullPath(DotnetUtil.DotnetPath);
				process.StartInfo.EnvironmentVariables[Constants.EnvironmentVariables.BEAM_DOTNET_MSBUILD_PATH] =
					Path.GetFullPath(DotnetUtil.DotnetMSBuildPath);
				
				_status = new TaskCompletionSource<int>();
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
							if (_exitCode >= 0) return;
							// there still may pending log lines, so we need to make sure they get processed before claiming the process is complete
							_exitCode = process.ExitCode;
							_status.TrySetResult(0);
						});

					});
				};

				process.Exited += eh;

				var earlyExitTask = new TaskCompletionSource<int>();
				OnTerminate(_ =>
				{
					CliLogger.Log("Early EOF exit");
					_exitCode = 0;
					earlyExitTask.SetResult(1);
				});
				
				var pid = 0;
				try
				{
					process.EnableRaisingEvents = true;

					process.OutputDataReceived += (sender, args) =>
					{
						if (_dispatcher.IsForceStopped)
						{
							KillProc();
							return;
						}

						_dispatcher.Schedule(() =>
						{
							CliLogger.Log("stdout", args.Data, System.Environment.NewLine + System.Environment.NewLine,
							              _command);
						});
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
					process.ErrorDataReceived += (sender, args) =>
					{
						if (_dispatcher.IsForceStopped)
						{
							KillProc();
							return;
						}

						_dispatcher.Schedule(() =>
						{
							CliLogger.Log("stderr", args.Data, System.Environment.NewLine + System.Environment.NewLine,
							              _command);
						});
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

					CliLogger.Log("starting", _command);
					process.Start();
					pid = process.Id;
					_collection?.Add(pid);
					// _started = true;
					process.BeginOutputReadLine();
					process.BeginErrorReadLine();

					await Task.WhenAny(_status.Task, earlyExitTask.Task);

					IEnumerator Defer()
					{
						yield return null; // delay a single frame, because the stdout/stderr callbacks may not have fired yet.
						
						if (_exitCode != 0)
						{
							CliLogger.Log("failed", _command, $"errors-count=[{_errors.Count}]");

							foreach (var err in _errors)
							{
								BeamEditorContext.Default.Dispatcher.Schedule(() => Debug.LogError(err.message));
							}
							throw new CliInvocationException(_command, _errors);
						}
						else
						{
							CliLogger.Log("done", _command );
						}
					}
					_dispatcher.Run("beam-cli-defer", Defer());
				}
				finally
				{
					process.Exited -= eh;
					_collection?.Remove(pid);
				}
			}


		}

		private void KillProc()
		{
			if (process.HasExited)
			{
				return;
			}

			try
			{
				process.Kill();
			}
			catch
			{
				Debug.LogWarning(
					$"Unable to kill beamCLI process. This <i>may</i> mean that there are pending beamCLI tasks on your machine. \n command=[{_command}]");
			}
		}
	}
}
