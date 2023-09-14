using Beamable.Common;
using Beamable.Common.Dependencies;
using System;

namespace Beamable.Editor
{
	public class EditorRuntimeConfigProvider : IRuntimeConfigProvider
	{
		private readonly AccountService _accounts;
		public string Cid => _accounts?.Account?.cid;
		public string Pid => _accounts?.Account?.realmPid;

		public EditorRuntimeConfigProvider(AccountService accounts)
		{
			_accounts = accounts;
		}
		
		
	}
	

}
