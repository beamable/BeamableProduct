// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using System;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.Common.BeamCli
{

	public class CliInvocationException : Exception
	{
		public List<ErrorOutput> Errors { get; private set; }
		public string Command { get; private set; }
		public CliInvocationException(string command, List<ErrorOutput> errors)
			: base($"CLI failed. command=[{command}] error-types=[{string.Join(",", errors.Select(x => x.typeName))}] error-messages=[{string.Join(",", errors.Select(x => x.message))}] error-stacks=[{string.Join(",", errors.Select(x => x.stackTrace))}]")
		{
			Command = command;
			Errors = errors;
		}
	}

	[Serializable]
	public class ErrorOutput
	{
		public string message;
		public string invocation;
		public int exitCode;
		public string typeName;
		public string fullTypeName;
		public string stackTrace;
	}

	[Serializable]
	public class EofOutput
	{
		// nothing. 
	}
}
