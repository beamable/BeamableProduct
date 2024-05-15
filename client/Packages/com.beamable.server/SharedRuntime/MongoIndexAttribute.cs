// this file was copied from nuget package Beamable.Server.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Server.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

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
