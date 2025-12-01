// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

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
