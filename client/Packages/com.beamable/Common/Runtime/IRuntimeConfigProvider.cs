using System;
using System.IO;
using UnityEngine;

namespace Beamable.Common
{
	/// <summary>
	/// A <see cref="IRuntimeConfigProvider"/> contains the CID and PID being used to connect to Beamable
	/// </summary>
	public interface IRuntimeConfigProvider
	{
		/// <summary>
		/// The CID is the customer id, or organization id. 
		/// </summary>
		string Cid { get; }
		
		/// <summary>
		/// The PID is the project id, or realm id.
		/// </summary>
		string Pid { get; }
	}

	public class DefaultRuntimeConfigProvider : IRuntimeConfigProvider
	{
		private readonly IRuntimeConfigProvider _fallback;

		/// <inheritdoc cref="IRuntimeConfigProvider.Cid"/>
		public string Cid => _cid ?? _fallback.Cid;

		/// <inheritdoc cref="IRuntimeConfigProvider.Pid"/>
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
