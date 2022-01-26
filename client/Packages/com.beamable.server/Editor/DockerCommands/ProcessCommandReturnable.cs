using Beamable.Common;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace Beamable.Server.Editor.DockerCommands
{
	public abstract class DockerCommandReturnable<T> : DockerCommand
	{
		public Action<string> OnStandardOut;
		public Action<string> OnStandardErr;

		private CommandRunnerWindow _context;
		protected Promise<T> Promise { get; private set; }

		protected string StandardOutBuffer { get; private set; }
		protected string StandardErrorBuffer { get; private set; }

		protected bool _finished;

		public override void Start()
		{
			Start(null);
		}

		public Promise<T> Start(CommandRunnerWindow context)
		{
			if (DockerRequired && DockerNotInstalled)
			{
				return Promise<T>.Failed(new DockerNotInstalledException());
			}
			_context = context;
			Promise = new Promise<T>();
			base.Start();

			ForceContextUpdateOnFinish();
			return Promise;
		}

		private void ForceContextUpdateOnFinish()
		{
			if (_context == null) return;

			void Check()
			{
				if (!_finished) return;
				try
				{
					if (_context != null)
					{
						_context.ForceProcess();
						EditorUtility.SetDirty(_context);
						_context.Repaint();
					}
				}
				finally
				{
					EditorApplication.update -= Check;
				}
			}

			EditorApplication.update += Check;
		}

		protected abstract void Resolve();

		protected override void HandleStandardOut(string data)
		{
			base.HandleStandardOut(data);
			if (data != null)
			{
				StandardOutBuffer += data;
				OnStandardOut?.Invoke(data);
			}
		}

		protected override void HandleStandardErr(string data)
		{
			base.HandleStandardErr(data);
			if (data != null)
			{
				StandardErrorBuffer += data;
				OnStandardErr?.Invoke(data);
			}
		}

		protected override void HandleOnExit()
		{
			void Callback()
			{
				base.HandleOnExit();
				Resolve();
			}

			if (_context == null)
			{
				EditorApplication.delayCall += Callback;
			}
			else
			{
				_context.RunOnMainThread(Callback);
			}

			_finished = true;
		}
	}

	public class CommandRunnerWindow : EditorWindow
	{
		static CommandRunnerWindow _instance;

		static volatile bool _queued = false;
		static List<Action> _backlog = new List<Action>(8);
		static List<Action> _actions = new List<Action>(8);

		private void Update()
		{
			// this is running on the main thread...
			if (_queued)
			{
				ForceProcess();
			}
		}

		public void ForceProcess()
		{
			lock (_backlog)
			{
				var tmp = _actions;
				_actions = _backlog;
				_backlog = tmp;
				_queued = false;
			}

			foreach (var action in _actions)
				action();

			_actions.Clear();
		}

		public void RunOnMainThread(Action action)
		{
			lock (_backlog)
			{
				_backlog.Add(action);
				_queued = true;
			}
		}
	}

}
