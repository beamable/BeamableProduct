
// using System.Diagnostics;

using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Editor;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace Beamable.Editor.BeamCli.Commands
{
	public partial class BeamCommands
	{
		private IBeamCommandFactory _factory;

		public BeamCommands(BeamableDispatcher dispatcher)
		{
			_factory = new BeamCommandFactory(dispatcher);
		}
	}
}

namespace Beamable.Editor.BeamCli
{


	
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
		private TaskCompletionSource<int> _status, _standardOutComplete;

		// private bool _hasExited;
		protected int _exitCode = -1;
		private bool _hasExecuted;

		public BeamCommand(BeamableDispatcher dispatcher)
		{
			_dispatcher = dispatcher;
		}
		
		private void ProcessStandardOut(string data)
		{
			Debug.Log(data);
		}

		private void ProcessStandardErr(string data)
		{
			Debug.Log(data);
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
						Task.Run( async () =>
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

						// before starting anything, make sure the beam context has initialized, so that the dispatcher can be accessed later.
						// await BeamEditorContext.Default.InitializePromise;
						// await MicroserviceEditor.WaitForInit();

						_process.Start();
						// _started = true;
						_process.BeginOutputReadLine();
						_process.BeginErrorReadLine();

						await _status.Task;
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
			}
		}
		
		
	}
}
