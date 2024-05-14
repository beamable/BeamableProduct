// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

using System;
using System.Collections.Generic;

namespace Beamable.Common.Content.Validation
{
	public class AggregateContentValidationException : AggregateException
	{
		public AggregateContentValidationException(IEnumerable<ContentValidationException> exs) : base(exs) { }
	}
}
