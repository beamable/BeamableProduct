using Beamable.Common;
using Beamable.Common.Dependencies;
using System;

namespace Beamable.Editor
{

	public class EditorRuntimeConfigProviderFallthrough : IRuntimeConfigProvider
	{
		public string Cid { get; private set; }
		public string Pid { get; private set; }

		public EditorRuntimeConfigProviderFallthrough(IDependencyProvider provider, BeamEditorContext editorCtx, ServiceDescriptor runtimeProviderDescriptor)
		{
			// var original = (IRuntimeConfigProvider) runtimeProviderDescriptor.Factory.Invoke(provider);
			var accountService = editorCtx.ServiceScope.GetService<AccountService>();
			var editorProvider = new EditorRuntimeConfigProvider(accountService);
			if (accountService != null && (accountService.Cid?.HasValue ?? false))
			{
				Cid = editorProvider.Cid;
				Pid = editorProvider.Pid;
			}
			else
			{
				// Cid = original.Cid;
				// Pid = original.Pid;
			}
		}
	}
	
	public class EditorRuntimeConfigProvider : IRuntimeConfigProvider
	{
		private readonly AccountService _accounts;
		public string Cid
		{
			get
			{
				var cid = _accounts?.Cid?.Value;
				if (string.IsNullOrEmpty(cid) && this != Beam.RuntimeConfigProvider.Fallback)
				{
					cid = Beam.RuntimeConfigProvider.Cid;
				}

				return cid;
			}
		}

		public string Pid
		{
			get
			{
				var pid = _accounts?.Account?.realmPid?.Value;
				if (string.IsNullOrEmpty(pid) && this != Beam.RuntimeConfigProvider.Fallback)
				{
					pid = Beam.RuntimeConfigProvider.Pid;
				}

				return pid;
			}
		}

		public EditorRuntimeConfigProvider(AccountService accounts)
		{
			_accounts = accounts;
		}
	}


}
