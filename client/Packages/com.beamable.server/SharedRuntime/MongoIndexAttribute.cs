using System;

namespace Beamable.Mongo
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class MongoIndexAttribute : Attribute
	{
		private readonly MongoIndexesExtension.IndexType _indexType;
		private readonly string _indexName;
		
		public MongoIndexAttribute(MongoIndexesExtension.IndexType indexType, string indexName)
		{
			_indexType = indexType;
			_indexName = indexName;
		}
	}
}
