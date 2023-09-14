using System;
using System.IO;
using UnityEngine;

namespace Beamable.Common
{
	public interface IRuntimeConfigProvider
	{
		string Cid { get; }
		string Pid { get; }
	}

	public class DefaultRuntimeConfigProvider : IRuntimeConfigProvider
	{
		private readonly IRuntimeConfigProvider _fallback;

		public string Cid
		{
			get => _cid ?? _fallback.Cid;
			set => _cid = value;
		}

		public string Pid
		{
			get => _pid ?? _fallback.Pid;
			set => _pid = value;
		}

		private string _cid, _pid;

		public DefaultRuntimeConfigProvider(IRuntimeConfigProvider fallback)
		{
			_fallback = fallback;
		}
	}
}
