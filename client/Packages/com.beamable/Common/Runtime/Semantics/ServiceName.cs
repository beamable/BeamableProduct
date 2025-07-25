// this file was copied from nuget package Beamable.Common@5.1.0
// https://www.nuget.org/packages/Beamable.Common/5.1.0

using Beamable.Common.BeamCli;
using System;
using System.Text.RegularExpressions;

namespace Beamable.Common.Semantics
{
	[CliContractType]
	public struct ServiceName
	{
		public string Value { get; }
		public ServiceName(string value)
		{
			// if we do not set the value skip checks
			if (string.IsNullOrEmpty(value))
			{
				Value = string.Empty;
				return;
			}
			string pattern = @"^[A-Za-z][A-Za-z0-9_-]*$";
			bool isMatch = Regex.IsMatch(value, pattern);
			if (!isMatch)
			{
				throw new Exception($"Invalid {nameof(ServiceName)}. Input=[{value}] is invalid. Must be alpha numeric. Dashes and underscores are allowed. Must start with an alpha character.");
			}

			Value = value;
		}

		public static implicit operator string(ServiceName d) => d.Value;

		public override string ToString()
		{
			return Value;
		}
	}
}
