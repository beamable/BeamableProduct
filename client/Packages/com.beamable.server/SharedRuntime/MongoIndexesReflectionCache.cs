using Beamable.Common;
using Beamable.Common.Assistant;
using Beamable.Common.Reflection;
using Beamable.Server;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharedRuntime
{
	public class MongoIndexesReflectionCache : IReflectionSystem
	{
		private static readonly BaseTypeOfInterest ICOLLECTION_ELEMENT_TYPE;
		private static readonly List<BaseTypeOfInterest> BASE_TYPES_OF_INTEREST;

		private static readonly AttributeOfInterest MONGO_INDEX_ATTRIBUTE;
		private static readonly List<AttributeOfInterest> ATTRIBUTES_OF_INTEREST;

		private readonly List<PendingMongoIndexData> _pendingMongoIndexesData = new List<PendingMongoIndexData>();

		public List<BaseTypeOfInterest> BaseTypesOfInterest => BASE_TYPES_OF_INTEREST;
		public List<AttributeOfInterest> AttributesOfInterest => ATTRIBUTES_OF_INTEREST;

		public List<PendingMongoIndexData> PendingMongoIndexesData => _pendingMongoIndexesData;

		internal static MongoIndexesReflectionCache Instance;

		static MongoIndexesReflectionCache()
		{
			ICOLLECTION_ELEMENT_TYPE = new BaseTypeOfInterest(typeof(StorageDocument));
			MONGO_INDEX_ATTRIBUTE =
				new AttributeOfInterest(typeof(MongoIndexAttribute), new Type[] { },
				                        new[] {typeof(StorageDocument)});
			BASE_TYPES_OF_INTEREST = new List<BaseTypeOfInterest> {ICOLLECTION_ELEMENT_TYPE};
			ATTRIBUTES_OF_INTEREST = new List<AttributeOfInterest> {MONGO_INDEX_ATTRIBUTE};
		}

		public void ClearCachedReflectionData()
		{
			_pendingMongoIndexesData.Clear();
		}

		public void OnBaseTypeOfInterestFound(BaseTypeOfInterest baseType, IReadOnlyList<MemberInfo> cachedSubTypes)
		{
			Instance = this;

			if (!BASE_TYPES_OF_INTEREST.Contains(baseType))
				return;

			foreach (MemberInfo info in cachedSubTypes)
			{
				TypeInfo typeInfo = (TypeInfo)info;
				List<Type> interfaces = typeInfo.GetInterfaces().ToList();

				List<Type> properInterfaces = interfaces.FindAll(i => i.IsGenericType && i.GetGenericTypeDefinition() ==
					                                                 typeof(ICollectionElement<>));

				if (properInterfaces.Count == 0)
					continue;

				foreach (Type properInterface in properInterfaces)
				{
					Type databaseType = properInterface.GetGenericArguments()[0];

					var pendingIndexData = new PendingMongoIndexData
					{
						Database = databaseType, Collection = typeInfo, Indexes = new List<MongoIndexDetails>()
					};

					foreach (MemberInfo memberInfo in typeInfo.DeclaredMembers)
					{
						foreach (CustomAttributeData attributeData in memberInfo.CustomAttributes)
						{
							if (attributeData.AttributeType != typeof(MongoIndexAttribute))
								continue;

							var mongoIndexType = (MongoIndexType)attributeData.ConstructorArguments[0].Value;
							var fieldInfo = (FieldInfo)memberInfo;

							var indexDetails = new MongoIndexDetails
							{
								Field = fieldInfo.Name, IndexType = mongoIndexType
							};

							pendingIndexData.Indexes.Add(indexDetails);
						}
					}

					_pendingMongoIndexesData.Add(pendingIndexData);
				}
			}
		}

		public void OnAttributeOfInterestFound(AttributeOfInterest attributeType,
		                                       IReadOnlyList<MemberAttribute> cachedMemberAttributes) { }

		public void OnSetupForCacheGeneration() { }

		public void OnReflectionCacheBuilt(PerBaseTypeCache perBaseTypeCache, PerAttributeCache perAttributeCache) { }

		public void SetStorage(IBeamHintGlobalStorage hintGlobalStorage) { }
	}

	public class PendingMongoIndexData
	{
		public Type Database { get; set; }
		public Type Collection { get; set; }
		public List<MongoIndexDetails> Indexes { get; set; } = new List<MongoIndexDetails>();
	}

	public class MongoIndexDetails
	{
		public MongoIndexType IndexType { get; set; }
		public string Field { get; set; }
	}
}
