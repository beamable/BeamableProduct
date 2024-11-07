// this file was copied from nuget package Beamable.Common@3.0.0-PREVIEW.RC4
// https://www.nuget.org/packages/Beamable.Common/3.0.0-PREVIEW.RC4

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
		public IRuntimeConfigProvider Fallback { get; set; }

		/// <inheritdoc cref="IRuntimeConfigProvider.Cid"/>
		public string Cid => Fallback.Cid;

		/// <inheritdoc cref="IRuntimeConfigProvider.Pid"/>
		public string Pid
		{
			get => _pid ?? Fallback.Pid;
			set => _pid = value;
		}

		private string _pid;

		public DefaultRuntimeConfigProvider(IRuntimeConfigProvider fallback)
		{
			Fallback = fallback;
		}

	}
}
