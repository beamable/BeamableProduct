using Beamable.Microservices;
using System;

namespace MongoDB.Bson
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class MongoIndexAttribute : Attribute
	{
		private readonly MongoDbExtensions.IndexType _indexType;
		private readonly string _indexName;
		
		public MongoIndexAttribute(MongoDbExtensions.IndexType indexType, string indexName = "")
		{
			_indexType = indexType;
			_indexName = indexName;
		}
	}
}
