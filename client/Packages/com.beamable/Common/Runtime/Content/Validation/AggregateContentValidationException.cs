// this file was copied from nuget package Beamable.Common@4.3.6-PREVIEW.RC1
// https://www.nuget.org/packages/Beamable.Common/4.3.6-PREVIEW.RC1

using System;
using System.Collections.Generic;

namespace Beamable.Common.Content.Validation
{
	public class AggregateContentValidationException : AggregateException
	{
		public AggregateContentValidationException(IEnumerable<ContentValidationException> exs) : base(exs) { }
	}
}
