// this file was copied from nuget package Beamable.Common@5.1.0-PREVIEW.RC1
// https://www.nuget.org/packages/Beamable.Common/5.1.0-PREVIEW.RC1

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
