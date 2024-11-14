// this file was copied from nuget package Beamable.Common@3.0.0-PREVIEW.RC6
// https://www.nuget.org/packages/Beamable.Common/3.0.0-PREVIEW.RC6

using System;
using System.Collections.Generic;

namespace Beamable.Common.Content.Validation
{
	public class AggregateContentValidationException : AggregateException
	{
		public AggregateContentValidationException(IEnumerable<ContentValidationException> exs) : base(exs) { }
	}
}
