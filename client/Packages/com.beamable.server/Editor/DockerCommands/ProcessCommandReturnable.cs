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


		protected Promise<T> Promise { get; private set; }

		protected string StandardOutBuffer { get; private set; }
		protected string StandardErrorBuffer { get; private set; }

		protected bool _finished;

		public override void Start()
		{
			StartAsync();
		}

		public virtual Promise<T> StartAsync()
		{
			if (DockerRequired && DockerNotInstalled)
			{
				return Promise<T>.Failed(new DockerNotInstalledException());
			}

			Promise = new Promise<T>();
			base.Start();

			return Promise;
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

			BeamEditorContext.Default.Dispatcher.Schedule(Callback);
			_finished = true;
		}
	}

}
