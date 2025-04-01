// this file was copied from nuget package Beamable.Common@4.2.0-PREVIEW.RC5
// https://www.nuget.org/packages/Beamable.Common/4.2.0-PREVIEW.RC5

using System;
using System.IO;
using System.Text.RegularExpressions;
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

		/// <summary>
		/// Validates whether the input is a valid PID value.
		/// </summary>
		/// <param name="input">The PID to validate.</param>
		/// <returns>Boolean indicating whether the input is valid.</returns>
		public static bool IsValidPid(string input) => Regex.IsMatch(input, @"^DE_\d+$");
	}
}
