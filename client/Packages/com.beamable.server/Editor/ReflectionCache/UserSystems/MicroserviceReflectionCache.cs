using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.BeamCli;
using Beamable.Common.Reflection;
using Beamable.Common.Runtime;
using Beamable.Editor.Environment;
using Beamable.Editor.Microservice.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Reflection;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Server.Editor.UI;
using Beamable.Server.Editor.UI.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static Beamable.Common.Constants.Features.Docker;
using static Beamable.Common.Constants.Features.Services;
using static Beamable.Common.Constants.MenuItems.Assets.Orders;
using LogMessage = Beamable.Editor.UI.Model.LogMessage;
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

			private Dictionary<string, MicroserviceBuilder> _serviceToBuilder =
				new Dictionary<string, MicroserviceBuilder>();

			private Dictionary<string, MongoStorageBuilder> _storageToBuilder =
				new Dictionary<string, MongoStorageBuilder>();

			public readonly List<StorageObjectDescriptor> StorageDescriptors = new List<StorageObjectDescriptor>();
			public readonly List<MicroserviceDescriptor> Descriptors = new List<MicroserviceDescriptor>();
			public readonly List<IDescriptor> AllDescriptors = new List<IDescriptor>();

			public readonly List<MicroserviceClientInfo> ClientInfos = new List<MicroserviceClientInfo>();

			public void ClearCachedReflectionData()
			{
				_serviceToBuilder.Clear();
				_storageToBuilder.Clear();

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

			#region Service Deployment

			public const string SERVICE_PUBLISHED_KEY = "service_published_{0}";

			public event Action<ManifestModel, int> OnBeforeDeploy;
			public event Action<ManifestModel, int> OnDeploySuccess;
			public event Action<ManifestModel, string> OnDeployFailed;
			public event Action<IDescriptor> OnServiceDeployProgress;

			public void MicroserviceCreated(string serviceName)
			{
				var key = string.Format(SERVICE_PUBLISHED_KEY, serviceName);
				EditorPrefs.SetBool(key, false);
			}

			public Promise<ManifestModel> GenerateUploadModel()
			{
				// first, get the server manifest
				var de = BeamEditorContext.Default;
				var client = de.GetMicroserviceManager();
				return client.GetCurrentManifest().Map(manifest =>
				{
					var allServices = new HashSet<string>();

					// make sure all server-side things are represented
					foreach (var serverSideService in manifest.manifest.Select(s => s.serviceName))
					{
						allServices.Add(serverSideService);
					}

					// add in anything locally...
					foreach (var descriptor in Descriptors)
					{
						allServices.Add(descriptor.Name);
					}

					// get enablement for each service...
					var entries = allServices.Select(name =>
					{
						var configEntry =
							MicroserviceConfiguration.Instance
													 .GetEntry(
														 name); //config.FirstOrDefault(s => s.ServiceName == name);
						var descriptor = Descriptors.FirstOrDefault(d => d.Name == configEntry.ServiceName);
						var remote = manifest.manifest.FirstOrDefault(s => string.Equals(s.serviceName, name));
						var serviceDependencies = new List<ServiceDependency>();
						if (descriptor != null)
						{
							foreach (var storage in descriptor.GetStorageReferences())
							{
								serviceDependencies.Add(
									new ServiceDependency { id = storage.Name, storageType = "storage" });
							}
						}
						else if (remote != null)
						{
							// this is a remote service, and we should keep its references intact...
							serviceDependencies.AddRange(remote.dependencies);
						}

						return new ManifestEntryModel
						{
							Comment = "",
							Name = name,
							Enabled = configEntry?.Enabled ?? true,
							Archived = configEntry?.Archived ?? false,
							TemplateId = configEntry?.TemplateId ?? "small",
							Dependencies = serviceDependencies
						};
					}).ToList();

					var allStorages = new HashSet<string>();

					foreach (var serverSideStorage in manifest.storageReference.Select(s => s.id))
					{
						allStorages.Add(serverSideStorage);
					}

					foreach (var storageDescriptor in StorageDescriptors)
					{
						allStorages.Add(storageDescriptor.Name);
					}

					var storageEntries = allStorages.Select(name =>
					{
						var configEntry = MicroserviceConfiguration.Instance.GetStorageEntry(name);
						return new StorageEntryModel
						{
							Name = name,
							Type = configEntry?.StorageType ?? "mongov1",
							Enabled = configEntry?.Enabled ?? true,
							Archived = configEntry?.Archived ?? false,
							TemplateId = configEntry?.TemplateId ?? "small",
						};
					}).ToList();

					return new ManifestModel
					{
						ServerManifest = manifest.manifest.ToDictionary(e => e.serviceName),
						Comment = "",
						Services = entries.ToDictionary(e => e.Name),
						Storages = storageEntries.ToDictionary(s => s.Name)
					};
				});
			}


			#endregion

			#region Running Services

			public async Promise<Dictionary<string, string>> GetConnectionStringEnvironmentVariables(
				MicroserviceDescriptor service)
			{
				var env = new Dictionary<string, string>();
				foreach (var reference in service.GetStorageReferences())
				{
					var key = $"STORAGE_CONNSTR_{reference.Name}";
					env[key] = await GetConnectionString(reference);
				}

				return env;
			}

			public async Promise<string> GetConnectionString(StorageObjectDescriptor storage)
			{
				var storageCheck = new CheckImageReturnableCommand(storage);
				var isStorageRunning = await storageCheck.StartAsync();
				if (isStorageRunning)
				{
					var config = MicroserviceConfiguration.Instance.GetStorageEntry(storage.Name);
					return
						$"mongodb://{config.LocalInitUser}:{config.LocalInitPass}@host.docker.internal:{config.LocalDataPort}";
				}
				else
				{
					return "";
				}
			}

			#endregion

			#region Service Builders

			public MicroserviceBuilder GetServiceBuilder(MicroserviceDescriptor descriptor)
			{
				var key = descriptor.Name;
				if (!_serviceToBuilder.ContainsKey(key))
				{
					var builder = new MicroserviceBuilder();
					builder.Init(descriptor);
					_serviceToBuilder.Add(key, builder);
				}

				return _serviceToBuilder[key];
			}

			public MongoStorageBuilder GetStorageBuilder(StorageObjectDescriptor descriptor)
			{
				var key = descriptor.Name;

				if (_storageToBuilder.ContainsKey(key))
					return _storageToBuilder[key];

				var builder = new MongoStorageBuilder();
				builder.Init(descriptor);
				_storageToBuilder.Add(key, builder);
				return _storageToBuilder[key];
			}

			#endregion
		}
	}
}
