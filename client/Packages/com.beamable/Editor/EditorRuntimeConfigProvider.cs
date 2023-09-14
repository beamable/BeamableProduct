using Beamable.Common;
using Beamable.Common.Dependencies;
using System;

namespace Beamable.Editor
{
	public class EditorRuntimeConfigProvider : IRuntimeConfigProvider
	{
		private readonly AccountService _accounts;
		public string Cid { get; }
		public string Pid { get; }

		public EditorRuntimeConfigProvider(AccountService accounts)
		{
			_accounts = accounts;

			if (_accounts.Account == null)
			{
				throw new InvalidOperationException("Beamable cannot initialize without a selected realm. Use toolbox to select a realm.");
			}
			
			Cid = _accounts.Account.cid;
			Pid = _accounts.Account.realmPid;
		}
		
		
	}
	

}
