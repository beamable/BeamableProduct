using Beamable.Common;
using Beamable.Common.Assistant;
using Beamable.Common.Reflection;
using Beamable.Mongo;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Beamable.Server
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

							var mongoIndexType =
								(MongoIndexesExtension.IndexType)attributeData.ConstructorArguments[0].Value;
							var mongoIndexName = (string)attributeData.ConstructorArguments[1].Value;
							var fieldInfo = (FieldInfo)memberInfo;

							var indexDetails = new MongoIndexDetails
							{
								Field = fieldInfo.Name, IndexType = mongoIndexType, IndexName = mongoIndexName
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

		public void SetupStorage(IStorageObjectConnectionProvider connectionProvider)
		{
			foreach (PendingMongoIndexData data in _pendingMongoIndexesData)
			{
				BeamableLogger.Log($"Working on: {data.Database}");

				MethodInfo getCollectionMethod =
					typeof(IStorageObjectConnectionProvider).GetMethods()
					                                        .Where(method =>
						                                               method.Name ==
						                                               nameof(IStorageObjectConnectionProvider
							                                                      .GetCollection) &&
						                                               method.GetParameters().Length == 0)
					                                        .FirstOrDefault(x => x.IsGenericMethod);

				Type mongoCollectionType = typeof(IMongoCollection<>);
				Type mongoCollectionGenericType = mongoCollectionType.MakeGenericType(data.Collection);
				Type promiseGenericType = typeof(Promise<>).MakeGenericType(mongoCollectionGenericType);

				MethodInfo getResultMethod = promiseGenericType.GetMethod(nameof(Promise.GetResult));

				IEnumerable<Type> enumerable = typeof(MongoIndexesExtension).Assembly.GetTypes();
				Type mongoDbExtensionsType = enumerable.First(t => t.Name == nameof(MongoIndexesExtension));

				MethodInfo methodInfo =
					mongoDbExtensionsType.GetMethod(nameof(MongoIndexesExtension.CreateSingleIndex));
				MethodInfo createSingleIndexMethodGeneric = methodInfo?.MakeGenericMethod(data.Collection);

				try
				{
					MethodInfo getCollectionMethodGeneric =
						getCollectionMethod?.MakeGenericMethod(data.Database, data.Collection);

					object collectionGenericObject = getCollectionMethodGeneric?.Invoke(connectionProvider, null);
					object convertedPromise = Convert.ChangeType(collectionGenericObject, promiseGenericType);
					object extractedCollectionObject = getResultMethod?.Invoke(convertedPromise, null);

					foreach (MongoIndexDetails details in data.Indexes)
					{
						BeamableLogger.Log($"Index data: {details.IndexType}-{details.Field}-{details.IndexName}");

						createSingleIndexMethodGeneric?.Invoke(extractedCollectionObject,
						                                       new[]
						                                       {
							                                       extractedCollectionObject, details.IndexType,
							                                       details.Field, details.IndexName
						                                       });
					}
				}
				catch (Exception e)
				{
					BeamableLogger.LogException(e);
					throw;
				}
			}
		}
	}

	public class PendingMongoIndexData
	{
		public Type Database { get; set; }
		public Type Collection { get; set; }
		public List<MongoIndexDetails> Indexes { get; set; } = new List<MongoIndexDetails>();
	}

	public class MongoIndexDetails
	{
		public MongoIndexesExtension.IndexType IndexType { get; set; }
		public string IndexName { get; set; }
		public string Field { get; set; }
	}
}
