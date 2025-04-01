// this file was copied from nuget package Beamable.Server.Common@4.2.0-PREVIEW.RC4
// https://www.nuget.org/packages/Beamable.Server.Common/4.2.0-PREVIEW.RC4

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
