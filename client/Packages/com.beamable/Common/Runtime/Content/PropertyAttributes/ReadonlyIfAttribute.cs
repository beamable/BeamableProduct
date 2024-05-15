// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

﻿using UnityEngine;

namespace Beamable.Common.Content
{
	public class ReadonlyIfAttribute : PropertyAttribute
	{
		public string conditionPath;
		public bool negate;
		public SpecialDrawer specialDrawer;

		public ReadonlyIfAttribute(string conditionPath)
		{
			this.conditionPath = conditionPath;
		}

		public enum SpecialDrawer
		{
			None,
			DelayedString
		}
	}
}
