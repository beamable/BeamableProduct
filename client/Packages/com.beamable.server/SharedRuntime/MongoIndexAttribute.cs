using System;

namespace MongoDB.Bson
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class MongoIndexAttribute : Attribute
	{
		private readonly MongoIndexType _indexType;

		public MongoIndexType IndexType => _indexType;

		public MongoIndexAttribute(MongoIndexType indexType) => _indexType = indexType;
	}
}
