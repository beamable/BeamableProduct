﻿using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.Model
{
	public abstract class ServiceBuilderBase : IBeamableBuilder
	{
		public IDescriptor Descriptor { get; internal set; }
		public Action<bool> OnIsRunningChanged { get; set; }
		public Action<int, int> OnBuildingProgress { get; set; }
		public Action<int, int> OnStartingProgress { get; set; }
		public Action<bool> OnBuildingFinished { get; set; }
		public Action<bool> OnStartingFinished { get; set; }
		public bool IsRunning
		{
			get => _isRunning;
			set
			{
				if (value == _isRunning) return;
				_isRunning = value;
				// XXX: If OnIsRunningChanged is mutated at before delayCall triggers, non-deterministic behaviour could occur
				EditorApplication.delayCall += () => OnIsRunningChanged?.Invoke(value);
			}
		}

		protected DockerCommand _logProcess;
		protected RunImageCommand _runProcess;
		protected bool _isRunning;
		private bool _isStopping;

		protected void CaptureLogs()
		{
			_logProcess?.Kill();
			_logProcess = new FollowLogCommand(Descriptor);
			_logProcess.Start();
		}

		public async Task CheckIfIsRunning()
		{
			var checkProcess = new CheckImageReturnableCommand(Descriptor)
			{
				WriteLogToUnity = false,
				WriteCommandToUnity = false
			};

			_isRunning = await checkProcess.Start(null);
		}

		protected abstract Task<RunImageCommand> PrepareRunCommand();

		public async Task TryToStart()
		{
			// if the service is already running; don't do anything.
			if (IsRunning) return;
			if (_runProcess != null) return;

			_runProcess = await PrepareRunCommand();
			_runProcess.OnStandardOut += message => MicroserviceLogHelper.HandleRunCommandOutput(this, message);
			_runProcess.OnStandardErr += message => MicroserviceLogHelper.HandleRunCommandOutput(this, message);

			// TODO: Send messages to /admin/HealthCheck to see if the service is ready to accept traffic.

			_runProcess.OnExit += i =>
			{
				IsRunning = false;
				_runProcess = null;
			};
			_runProcess?.Start();
		}

		public virtual async void Init(IDescriptor descriptor)
		{
			Descriptor = descriptor;

			_isRunning = false;
			await CheckIfIsRunning();
			if (IsRunning)
			{
				CaptureLogs();
			}
		}

		public async Task TryToStop()
		{
			if (!IsRunning) return;
			if (_isStopping) return;

			_isStopping = true;
			try
			{
				var stopProcess = new StopImageReturnableCommand(Descriptor);
				await stopProcess.Start(null);
				IsRunning = false;
			}
			finally
			{
				_isStopping = false;
			}
		}

		public async Task TryToRestart()
		{
			if (!IsRunning) return;

			await TryToStop();
			await TryToStart();
		}
	}
}
