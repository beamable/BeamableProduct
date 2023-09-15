using System;

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
