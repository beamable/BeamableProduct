using Beamable.Common;
using Beamable.Common.Dependencies;
using System;
using UnityEngine;

namespace Beamable.Editor
{

	public class EditorRuntimeConfigProviderFallthrough : IRuntimeConfigProvider
	{
		public string Cid { get; private set; }
		public string Pid { get; private set; }

		public EditorRuntimeConfigProviderFallthrough(AccountServerData accountData)
		{
			Cid = accountData.cid.Value;
			Pid = accountData.Account?.realmPid?.Value;
			Debug.Log($"_------_ CID=[{Cid}] PID=[{Pid}]");
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
