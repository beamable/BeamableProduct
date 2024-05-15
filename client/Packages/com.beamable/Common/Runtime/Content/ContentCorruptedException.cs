// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

﻿using System;

namespace Beamable.Common.Content
{
	public class ContentCorruptedException : Exception
	{
		public ContentCorruptedException(string message) : base(message)
		{

		}
	}
}

