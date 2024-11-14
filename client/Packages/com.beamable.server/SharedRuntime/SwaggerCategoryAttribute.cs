// this file was copied from nuget package Beamable.Server.Common@3.0.0-PREVIEW.RC6
// https://www.nuget.org/packages/Beamable.Server.Common/3.0.0-PREVIEW.RC6

﻿using System;

namespace Beamable.Server
{
	[AttributeUsage(AttributeTargets.Method)]
	public class SwaggerCategoryAttribute : Attribute
	{
		public string CategoryName { get; }

		public SwaggerCategoryAttribute(string categoryName)
		{
			CategoryName = string.IsNullOrWhiteSpace(categoryName) ? "Uncategorized" : categoryName;
		}
	}
}
