// this file was copied from nuget package Beamable.Server.Common@4.3.6-PREVIEW.RC1
// https://www.nuget.org/packages/Beamable.Server.Common/4.3.6-PREVIEW.RC1

﻿using System;

namespace Beamable.Mongo
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class MongoIndexAttribute : Attribute
	{
		public readonly MongoIndexesExtension.IndexType IndexType;
		public readonly string IndexName;

		public MongoIndexAttribute(MongoIndexesExtension.IndexType indexType, string indexName)
		{
			IndexType = indexType;
			IndexName = indexName;
		}
	}
}
