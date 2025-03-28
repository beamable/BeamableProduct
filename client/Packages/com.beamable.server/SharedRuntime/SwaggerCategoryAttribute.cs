// this file was copied from nuget package Beamable.Server.Common@4.1.5
// https://www.nuget.org/packages/Beamable.Server.Common/4.1.5

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
