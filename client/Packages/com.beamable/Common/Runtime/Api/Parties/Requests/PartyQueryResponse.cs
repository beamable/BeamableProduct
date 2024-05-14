// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

﻿using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Parties
{
	/// <summary>
	/// Response payload including a list of <see cref="Party"/>.
	/// </summary>
	[Serializable]
	public class PartyQueryResponse
	{
		/// <summary>
		/// List of <see cref="Party"/> representing the results of the query.
		/// </summary>
		public List<Party> results;
	}
}
