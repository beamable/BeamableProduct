using Beamable.Common;
using Beamable.Common.Dependencies;
using System;

namespace Beamable.Editor
{
	public class EditorRuntimeConfigProvider : IRuntimeConfigProvider
	{
		private readonly AccountService _accounts;
		public string Cid => _accounts.Cid;

		public string Pid => _accounts.Account.realmPid;
		// public string Cid => _accounts?.Account?.cid ?? _fallback?.Cid;//;
		// public string Pid => _accounts?.Account?.realmPid ?? _fallback?.Pid;//;
		public EditorRuntimeConfigProvider(AccountService accounts)
		{
			_accounts = accounts;
		}
		
		
	}
	

}
