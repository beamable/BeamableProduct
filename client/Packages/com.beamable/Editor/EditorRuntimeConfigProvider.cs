using Beamable.Common;
using Beamable.Common.Dependencies;
using System;

namespace Beamable.Editor
{
	public class EditorRuntimeConfigProvider : IRuntimeConfigProvider
	{
		private readonly AccountService _accounts;
		// public string Cid => _accounts.Cid;
		// public string Pid => _accounts.Account.realmPid;

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
		
		// public string Cid => _accounts?.Account?.cid ?? _fallback?.Cid;//;
		// public string Pid => _accounts?.Account?.realmPid ?? _fallback?.Pid;//;
		public EditorRuntimeConfigProvider(AccountService accounts)
		{
			_accounts = accounts;
		}
		
		
	}
	

}
