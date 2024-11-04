using Beamable.Common;
using Beamable.Common.Reflection;
using Beamable.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static Beamable.Common.Constants.MenuItems.Assets.Orders;

#pragma warning disable CS0067 // Event is never used

namespace Beamable.Server.Editor
{
#if BEAMABLE_DEVELOPER
	[CreateAssetMenu(fileName = "MicroserviceReflectionCache", menuName = "Beamable/Reflection/Microservices Cache",
	                 order = MENU_ITEM_PATH_ASSETS_BEAMABLE_ORDER_2)]
#endif
	public class MicroserviceReflectionCache : ReflectionSystemObject
	{
		[NonSerialized] public Registry Cache;

		public override IReflectionSystem System => Cache;

		public override IReflectionTypeProvider TypeProvider => Cache;

		public override Type SystemType => typeof(Registry);

		private MicroserviceReflectionCache()
		{
			Cache = new Registry();
		}

		public class Registry : IReflectionSystem
		{
			private static readonly BaseTypeOfInterest MICROSERVICE_BASE_TYPE;
			private static readonly BaseTypeOfInterest MICROSERVICE_CLIENT_BASE_TYPE;
			private static readonly BaseTypeOfInterest MONGO_STORAGE_OBJECT_BASE_TYPE;
			private static readonly List<BaseTypeOfInterest> BASE_TYPES_OF_INTEREST;

			private static readonly AttributeOfInterest MICROSERVICE_ATTRIBUTE;
			private static readonly AttributeOfInterest STORAGE_OBJECT_ATTRIBUTE;
			private static readonly List<AttributeOfInterest> ATTRIBUTES_OF_INTEREST;

			static Registry()
			{
				MICROSERVICE_BASE_TYPE = new BaseTypeOfInterest(typeof(Microservice));
				MICROSERVICE_CLIENT_BASE_TYPE = new BaseTypeOfInterest(typeof(MicroserviceClient));
				MICROSERVICE_ATTRIBUTE =
					new AttributeOfInterest(typeof(MicroserviceAttribute), new Type[] { },
											new[] { typeof(Microservice) });

				MONGO_STORAGE_OBJECT_BASE_TYPE = new BaseTypeOfInterest(typeof(MongoStorageObject));
				STORAGE_OBJECT_ATTRIBUTE =
					new AttributeOfInterest(typeof(StorageObjectAttribute), new Type[] { },
											new[] { typeof(StorageObject) });

				BASE_TYPES_OF_INTEREST =
					new List<BaseTypeOfInterest>() { MICROSERVICE_BASE_TYPE, MONGO_STORAGE_OBJECT_BASE_TYPE, MICROSERVICE_CLIENT_BASE_TYPE };
				ATTRIBUTES_OF_INTEREST =
					new List<AttributeOfInterest>() { MICROSERVICE_ATTRIBUTE, STORAGE_OBJECT_ATTRIBUTE };
			}

			public List<BaseTypeOfInterest> BaseTypesOfInterest => BASE_TYPES_OF_INTEREST;
			public List<AttributeOfInterest> AttributesOfInterest => ATTRIBUTES_OF_INTEREST;

			public readonly List<StorageObjectDescriptor> StorageDescriptors = new List<StorageObjectDescriptor>();
			public readonly List<MicroserviceDescriptor> Descriptors = new List<MicroserviceDescriptor>();
			public readonly List<IDescriptor> AllDescriptors = new List<IDescriptor>();

			public readonly List<MicroserviceClientInfo> ClientInfos = new List<MicroserviceClientInfo>();

			public void ClearCachedReflectionData()
			{
				Descriptors.Clear();
				StorageDescriptors.Clear();
				AllDescriptors.Clear();
			}

			public void OnSetupForCacheGeneration()
			{
				// Since we don't require any setup prior to generating the cache, we can skip it.
			}

			public void OnReflectionCacheBuilt(PerBaseTypeCache perBaseTypeCache,
											   PerAttributeCache perAttributeCache)
			{
				// TODO: Display BeamHint of validation type for microservices declared in ignored assemblies.
			}

			public void OnBaseTypeOfInterestFound(BaseTypeOfInterest baseType, IReadOnlyList<MemberInfo> cachedSubTypes)
			{
				if (baseType.Equals(MICROSERVICE_BASE_TYPE))
					ParseMicroserviceSubTypes(cachedSubTypes);

				if (baseType.Equals(MONGO_STORAGE_OBJECT_BASE_TYPE))
					ParseStorageObjectSubTypes(cachedSubTypes);

				if (baseType.Equals(MICROSERVICE_CLIENT_BASE_TYPE))
					ParseMicroserviceClientSubTypes(cachedSubTypes);

				void ParseMicroserviceClientSubTypes(IReadOnlyList<MemberInfo> cachedMicroserviceClientSubtypes)
				{
					foreach (var member in cachedMicroserviceClientSubtypes)
					{
						switch (member)
						{
							case Type memberType:
								try
								{
									ClientInfos.Add(CreateClientInfo(memberType));

								}
								catch (InvalidOperationException ex)
								{
									Debug.LogWarning($"Unable to create {nameof(MicroserviceClientInfo)} for {memberType}. Reason=[{ex.Message}]");
								}

								break;
							default:
								Debug.LogWarning($"Unknown member type for Microservice client in reflection cache. member=[{member}] ");
								break;
						}
					}
				}

				void ParseMicroserviceSubTypes(IReadOnlyList<MemberInfo> cachedMicroserviceSubtypes)
				{
					var validationResults = cachedMicroserviceSubtypes
						.GetAndValidateAttributeExistence(MICROSERVICE_ATTRIBUTE,
														  info =>
														  {
															  var message =
																  $"Microservice sub-class [{info.Name}] does not have the [{nameof(MicroserviceAttribute)}].";
															  return new AttributeValidationResult(
																  null, info,
																  ReflectionCache.ValidationResultType.Error, message);
														  });

					// Get all Microservice Attribute usage errors found
					validationResults.SplitValidationResults(out _, out _, out var microserviceAttrErrors);

					// Register a hint with all its validation errors as the context object
					// TODO: [AssistantRemoval] ID_MICROSERVICE_ATTRIBUTE_MISSING --- this can be a Static Analyzer.
				}

				void ParseStorageObjectSubTypes(IReadOnlyList<MemberInfo> cachedStorageObjectSubTypes)
				{
					var validationResults = cachedStorageObjectSubTypes
						.GetAndValidateAttributeExistence(STORAGE_OBJECT_ATTRIBUTE,
														  info =>
														  {
															  var message =
																  $"{nameof(StorageObject)} sub-class [{info.Name}] does not have the [{nameof(StorageObjectAttribute)}].";
															  return new AttributeValidationResult(
																  null, info,
																  ReflectionCache.ValidationResultType.Error, message);
														  });

					// Get all Microservice Attribute usage errors found
					validationResults.SplitValidationResults(out _, out _, out var storageObjAttrErrors);

					// Register a hint with all its validation errors as the context object
					// TODO: [AssistantRemoval] ID_STORAGE_OBJECT_ATTRIBUTE_MISSING --- this can be a Static Analyzer.
				}
			}

			public void OnAttributeOfInterestFound(AttributeOfInterest attributeType,
												   IReadOnlyList<MemberAttribute> cachedMemberAttributes)
			{
				if (attributeType.Equals(MICROSERVICE_ATTRIBUTE))
					ParseMicroserviceAttributes(cachedMemberAttributes);

				if (attributeType.Equals(STORAGE_OBJECT_ATTRIBUTE))
					ParseStorageObjectAttributes(cachedMemberAttributes);

				void ParseMicroserviceAttributes(IReadOnlyList<MemberAttribute> cachedMicroserviceAttributes)
				{
					// Searches for all unique name collisions.
					var uniqueNameValidationResults = cachedMicroserviceAttributes
						.GetAndValidateUniqueNamingAttributes<MicroserviceAttribute>();

					// Registers a hint with all name collisions found.
					// TODO: [AssistantRemoval] ID_MICROSERVICE_NAME_COLLISION --- this can be a Static Analyzer.

					// Get all ClientCallables
					var clientCallableValidationResults = cachedMicroserviceAttributes
														  .SelectMany(
															  pair => pair.InfoAs<Type>()
																		  .GetMethods(
																			  BindingFlags.Public |
																			  BindingFlags.Instance))
														  .GetOptionalAttributeInMembers<ClientCallableAttribute>();

					// Handle invalid signatures and warnings
					clientCallableValidationResults.SplitValidationResults(out var clientCallablesValid,
																		   out var clientCallableWarnings,
																		   out var clientCallableErrors);

					// TODO: [AssistantRemoval] ID_CLIENT_CALLABLE_ASYNC_VOID --- this can be a Static Analyzer.
					// TODO: [AssistantRemoval] ID_CLIENT_CALLABLE_UNSUPPORTED_PARAMETERS --- this can be a Static Analyzer.

					// Builds a lookup of DeclaringType => MemberAttribute.
					var validClientCallablesLookup = clientCallablesValid
													 .Concat(clientCallableWarnings)
													 .Concat(clientCallableErrors)
													 .Select(result => result.Pair)
													 .CreateMemberAttributeOwnerLookupTable();

					// Register all configured microservices
					for (int k = 0; k < uniqueNameValidationResults.PerAttributeNameValidations.Count; k++)
					{
						var serviceAttribute = uniqueNameValidationResults.PerAttributeNameValidations[k].Pair
																		  .AttrAs<MicroserviceAttribute>();
						var type = uniqueNameValidationResults.PerAttributeNameValidations[k].Pair.InfoAs<Type>();

						// TODO: XXX this is a hacky way to ignore the default microservice...
						if (serviceAttribute.MicroserviceName.ToLower().Equals("xxxx")) continue;

						if (!File.Exists(serviceAttribute.GetSourcePath()))
							continue;

						// Create descriptor
						var hasWarning = uniqueNameValidationResults.PerAttributeNameValidations[k].Type ==
										 ReflectionCache.ValidationResultType.Warning;
						var hasError = uniqueNameValidationResults.PerAttributeNameValidations[k].Type ==
									   ReflectionCache.ValidationResultType.Error;
						var descriptor = new MicroserviceDescriptor
						{
							Name = serviceAttribute.MicroserviceName,
							Type = type,
							CustomClientPath = serviceAttribute.CustomAutoGeneratedClientPath,
							AttributePath = serviceAttribute.GetSourcePath(),
							HasValidationError = hasError,
							HasValidationWarning = hasWarning,
						};

						// Initialize the ClientCallableDescriptors if the type has any.
						if (validClientCallablesLookup.TryGetValue(type, out var clientCallables))
						{
							// Generates descriptors for each of the individual client callables.
							descriptor.Methods = clientCallables.Select(delegate (MemberAttribute pair)
							{
								var clientCallableAttribute = pair.AttrAs<ClientCallableAttribute>();
								var clientCallableMethod = pair.InfoAs<MethodInfo>();

								var callableName = pair.GetOptionalNameOrMemberName<ClientCallableAttribute>();
								var callableScopes = clientCallableAttribute.RequiredScopes;

								var parameters = clientCallableMethod
												 .GetParameters()
												 .Select((param, i) =>
												 {
													 var paramAttr = param.GetCustomAttribute<ParameterAttribute>();
													 var paramName =
														 string.IsNullOrEmpty(paramAttr?.ParameterNameOverride)
															 ? param.Name
															 : paramAttr.ParameterNameOverride;
													 return new ClientCallableParameterDescriptor
													 {
														 Name = paramName,
														 Index = i,
														 Type = param.ParameterType
													 };
												 }).ToArray();

								return new ClientCallableDescriptor()
								{
									Path = callableName,
									Scopes = callableScopes,
									Parameters = parameters,
								};
							}).ToList();
						}
						else // If no client callables were found in the C#MS, initialize an empty list.
						{
							descriptor.Methods = new List<ClientCallableDescriptor>();
						}

						// Check if MS is used for external identity federation
						var interfaces = descriptor.Type.GetInterfaces();



						foreach (var it in interfaces)
						{
							// Skip non-generic types while we look for IFederation-derived implementations
							if (!it.IsGenericType)
								continue;

							// Make sure we found an IFederation interface
							if (!it.GetGenericTypeDefinition().GetInterfaces().Contains(typeof(IFederation)))
								continue;

							// Get the cleaned-up type name (IFederatedGameServer`1 => IFederatedGameServer) 
							var typeName = it.GetGenericTypeDefinition().Name;
							typeName = typeName.Substring(0, typeName.IndexOf("`", StringComparison.Ordinal));

							// Get the IFederationId 
							var federatedType = it.GetGenericArguments()[0];
							if (Activator.CreateInstance(federatedType) is IFederationId identity)
							{
								descriptor.FederatedNamespaces.Add(identity.UniqueName);
								descriptor.FederationComponents.Add(new FederationComponent
								{
									identity = identity,
									interfaceType = it,
									typeName = typeName
								});
							}
						}

						Descriptors.Add(descriptor);
						AllDescriptors.Add(descriptor);
					}
				}

				void ParseStorageObjectAttributes(IReadOnlyList<MemberAttribute> cachedStorageObjectAttributes)
				{
					// Searches for all unique name collisions.
					var uniqueNameValidationResults = cachedStorageObjectAttributes
						.GetAndValidateUniqueNamingAttributes<StorageObjectAttribute>();

					// Registers a hint with all name collisions found.
					// TODO: [AssistantRemoval] ID_STORAGE_OBJECT_NAME_COLLISION --- this can be a Static Analyzer.

					// Register all configured storage object
					foreach (var storageObjectValResults in uniqueNameValidationResults.PerAttributeNameValidations)
					{
						var serviceAttribute = storageObjectValResults.Pair.AttrAs<StorageObjectAttribute>();
						var type = storageObjectValResults.Pair.InfoAs<Type>();

						// TODO: XXX this is a hacky way to ignore the default microservice...
						if (serviceAttribute.StorageName.ToLower().Equals("xxxx")) continue;

						if (!File.Exists(serviceAttribute.SourcePath))
							continue;

						// Create descriptor
						var hasWarning = storageObjectValResults.Type == ReflectionCache.ValidationResultType.Warning;
						var hasError = storageObjectValResults.Type == ReflectionCache.ValidationResultType.Error;
						var descriptor = new StorageObjectDescriptor
						{
							Name = serviceAttribute.StorageName,
							Type = type,
							AttributePath = serviceAttribute.SourcePath,
							HasValidationWarning = hasWarning,
							HasValidationError = hasError,
						};

						StorageDescriptors.Add(descriptor);
						AllDescriptors.Add(descriptor);
					}
				}
			}

			private static MicroserviceClientInfo CreateClientInfo(Type clientType)
			{

				var clientCons = clientType.GetConstructor(new Type[] { typeof(BeamContext) });
				var clientInstance = (MicroserviceClient)clientCons?.Invoke(new object[] { null });

				var info = new MicroserviceClientInfo();

				if (clientInstance is IHaveServiceName serviceName)
				{
					info.ServiceName = serviceName.ServiceName;
				}
				else
				{
					throw new InvalidOperationException(
						$"the autogenerated microservice does not have {nameof(IHaveServiceName)} implemented");
				}

				var interfaces = clientType.GetInterfaces();
				foreach (var it in interfaces)
				{
					// Skip non-generic types while we look for IFederation-derived implementations
					if (!it.IsGenericType)
						continue;

					// Make sure we found an IFederation interface
					if (!it.GetGenericTypeDefinition().GetInterfaces().Contains(typeof(IFederation)))
						continue;

					// Get the cleaned-up type name (IFederatedGameServer`1 => IFederatedGameServer) 
					var typeName = it.GetGenericTypeDefinition().Name;
					typeName = typeName.Substring(0, typeName.IndexOf("`", StringComparison.Ordinal));

					// Get the IFederationId 
					var federatedType = it.GetGenericArguments()[0];
					if (Activator.CreateInstance(federatedType) is IFederationId identity)
					{
						info.FederationComponents.Add(new FederationComponent
						{
							identity = identity,
							interfaceType = it,
							typeName = typeName
						});
					}
				}

				return info;
			}

		}
	}
}
