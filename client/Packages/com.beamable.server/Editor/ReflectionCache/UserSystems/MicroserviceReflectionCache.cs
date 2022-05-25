using Beamable.Common;
using Beamable.Common.Assistant;
using Beamable.Common.Reflection;
using Beamable.Editor;
using Beamable.Editor.Microservice.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Reflection;
using Beamable.Server.Editor.CodeGen;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Server.Editor.UI;
using Beamable.Server.Editor.UI.Components;
using Beamable.Server.Editor.Uploader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using static Beamable.Common.Constants.Features.Services;
using static Beamable.Common.Constants.MenuItems.Assets.Orders;
using LogMessage = Beamable.Editor.UI.Model.LogMessage;

namespace Beamable.Server.Editor
{
#if BEAMABLE_DEVELOPER
	[CreateAssetMenu(fileName = "MicroserviceReflectionCache", menuName = "Beamable/Reflection/Microservices Cache", order = MENU_ITEM_PATH_ASSETS_BEAMABLE_ORDER_2)]
#endif
	public class MicroserviceReflectionCache : ReflectionSystemObject
	{
		[NonSerialized]
		public Registry Cache;

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
			private static readonly BaseTypeOfInterest MONGO_STORAGE_OBJECT_BASE_TYPE;
			private static readonly List<BaseTypeOfInterest> BASE_TYPES_OF_INTEREST;

			private static readonly AttributeOfInterest MICROSERVICE_ATTRIBUTE;
			private static readonly AttributeOfInterest STORAGE_OBJECT_ATTRIBUTE;
			private static readonly List<AttributeOfInterest> ATTRIBUTES_OF_INTEREST;

			static Registry()
			{
				MICROSERVICE_BASE_TYPE = new BaseTypeOfInterest(typeof(Microservice));
				MICROSERVICE_ATTRIBUTE = new AttributeOfInterest(typeof(MicroserviceAttribute), new Type[] { }, new[] { typeof(Microservice) });

				MONGO_STORAGE_OBJECT_BASE_TYPE = new BaseTypeOfInterest(typeof(MongoStorageObject));
				STORAGE_OBJECT_ATTRIBUTE = new AttributeOfInterest(typeof(StorageObjectAttribute), new Type[] { }, new[] { typeof(StorageObject) });

				BASE_TYPES_OF_INTEREST = new List<BaseTypeOfInterest>() { MICROSERVICE_BASE_TYPE, MONGO_STORAGE_OBJECT_BASE_TYPE };
				ATTRIBUTES_OF_INTEREST = new List<AttributeOfInterest>() { MICROSERVICE_ATTRIBUTE, STORAGE_OBJECT_ATTRIBUTE };
			}

			public List<BaseTypeOfInterest> BaseTypesOfInterest => BASE_TYPES_OF_INTEREST;
			public List<AttributeOfInterest> AttributesOfInterest => ATTRIBUTES_OF_INTEREST;

			private Dictionary<string, MicroserviceStateMachine> _serviceToStateMachine = new Dictionary<string, MicroserviceStateMachine>();
			private Dictionary<string, MicroserviceBuilder> _serviceToBuilder = new Dictionary<string, MicroserviceBuilder>();
			private Dictionary<string, MongoStorageBuilder> _storageToBuilder = new Dictionary<string, MongoStorageBuilder>();

			public readonly List<StorageObjectDescriptor> StorageDescriptors = new List<StorageObjectDescriptor>();
			public readonly List<MicroserviceDescriptor> Descriptors = new List<MicroserviceDescriptor>();
			public readonly List<IDescriptor> AllDescriptors = new List<IDescriptor>();

			private IBeamHintGlobalStorage _hintStorage;

			public void SetStorage(IBeamHintGlobalStorage hintGlobalStorage) => _hintStorage = hintGlobalStorage;

			public void ClearCachedReflectionData()
			{
				_serviceToStateMachine.Clear();
				_serviceToBuilder.Clear();
				_storageToBuilder.Clear();

				Descriptors.Clear();
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

				void ParseMicroserviceSubTypes(IReadOnlyList<MemberInfo> cachedMicroserviceSubtypes)
				{
					var validationResults = cachedMicroserviceSubtypes
						.GetAndValidateAttributeExistence(MICROSERVICE_ATTRIBUTE,
														  info =>
														  {
															  var message = $"Microservice sub-class [{info.Name}] does not have the [{nameof(MicroserviceAttribute)}].";
															  return new AttributeValidationResult(null, info, ReflectionCache.ValidationResultType.Error, message);
														  });

					// Get all Microservice Attribute usage errors found
					validationResults.SplitValidationResults(out _, out _, out var microserviceAttrErrors);

					// Register a hint with all its validation errors as the context object
					if (microserviceAttrErrors.Count > 0)
					{
						var hint = new BeamHintHeader(BeamHintType.Validation, BeamHintDomains.BEAM_CSHARP_MICROSERVICES_CODE_MISUSE, BeamHintIds.ID_MICROSERVICE_ATTRIBUTE_MISSING);
						_hintStorage.AddOrReplaceHint(hint, microserviceAttrErrors);
					}
				}

				void ParseStorageObjectSubTypes(IReadOnlyList<MemberInfo> cachedStorageObjectSubTypes)
				{
					var validationResults = cachedStorageObjectSubTypes
						.GetAndValidateAttributeExistence(STORAGE_OBJECT_ATTRIBUTE,
														  info =>
														  {
															  var message = $"{nameof(StorageObject)} sub-class [{info.Name}] does not have the [{nameof(StorageObjectAttribute)}].";
															  return new AttributeValidationResult(null, info, ReflectionCache.ValidationResultType.Error, message);
														  });

					// Get all Microservice Attribute usage errors found
					validationResults.SplitValidationResults(out _, out _, out var storageObjAttrErrors);

					// Register a hint with all its validation errors as the context object
					if (storageObjAttrErrors.Count > 0)
					{
						var hint = new BeamHintHeader(BeamHintType.Validation, BeamHintDomains.BEAM_CSHARP_MICROSERVICES_CODE_MISUSE, BeamHintIds.ID_STORAGE_OBJECT_ATTRIBUTE_MISSING);
						_hintStorage.AddOrReplaceHint(hint, storageObjAttrErrors);
					}
				}
			}

			public void OnAttributeOfInterestFound(AttributeOfInterest attributeType, IReadOnlyList<MemberAttribute> cachedMemberAttributes)
			{
				if (attributeType.Equals(MICROSERVICE_ATTRIBUTE))
					ParseMicroserviceAttributes(cachedMemberAttributes);

				if (attributeType.Equals(STORAGE_OBJECT_ATTRIBUTE))
					ParseStorageObjectAttributes(cachedMemberAttributes);

				void ParseMicroserviceAttributes(IReadOnlyList<MemberAttribute> cachedMicroserviceAttributes)
				{
					// Searches for all unique name collisions.
					var uniqueNameValidationResults = cachedMicroserviceAttributes.GetAndValidateUniqueNamingAttributes<MicroserviceAttribute>();

					// Registers a hint with all name collisions found.
					if (uniqueNameValidationResults.PerNameCollisions.Count > 0)
					{
						var hint = new BeamHintHeader(BeamHintType.Validation, BeamHintDomains.BEAM_CSHARP_MICROSERVICES_CODE_MISUSE, BeamHintIds.ID_MICROSERVICE_NAME_COLLISION);
						_hintStorage.AddOrReplaceHint(hint, uniqueNameValidationResults.PerNameCollisions);
					}

					// Get all ClientCallables
					var clientCallableValidationResults = cachedMicroserviceAttributes
														  .SelectMany(pair => pair.InfoAs<Type>().GetMethods(BindingFlags.Public | BindingFlags.Instance))
														  .GetOptionalAttributeInMembers<ClientCallableAttribute>();

					// Handle invalid signatures and warnings
					clientCallableValidationResults.SplitValidationResults(out var clientCallablesValid,
																		   out var clientCallableWarnings,
																		   out var clientCallableErrors);
					if (clientCallableWarnings.Count > 0)
					{
						var hint = new BeamHintHeader(BeamHintType.Hint, BeamHintDomains.BEAM_CSHARP_MICROSERVICES_CODE_MISUSE, BeamHintIds.ID_CLIENT_CALLABLE_ASYNC_VOID);
						_hintStorage.AddOrReplaceHint(hint, clientCallableWarnings);
					}

					if (clientCallableErrors.Count > 0)
					{
						var hint = new BeamHintHeader(BeamHintType.Validation, BeamHintDomains.BEAM_CSHARP_MICROSERVICES_CODE_MISUSE, BeamHintIds.ID_CLIENT_CALLABLE_UNSUPPORTED_PARAMETERS);
						_hintStorage.AddOrReplaceHint(hint, clientCallableErrors);
					}

					// Builds a lookup of DeclaringType => MemberAttribute.
					var validClientCallablesLookup = clientCallablesValid
													 .Concat(clientCallableWarnings)
													 .Concat(clientCallableErrors)
													 .Select(result => result.Pair)
													 .CreateMemberAttributeOwnerLookupTable();

					// Register all configured microservices
					foreach (var msAttrValidationResult in uniqueNameValidationResults.PerAttributeNameValidations)
					{
						var serviceAttribute = msAttrValidationResult.Pair.AttrAs<MicroserviceAttribute>();
						var type = msAttrValidationResult.Pair.InfoAs<Type>();

						// TODO: XXX this is a hacky way to ignore the default microservice...
						if (serviceAttribute.MicroserviceName.ToLower().Equals("xxxx")) continue;

						// Create descriptor
						var hasWarning = msAttrValidationResult.Type == ReflectionCache.ValidationResultType.Warning;
						var hasError = msAttrValidationResult.Type == ReflectionCache.ValidationResultType.Error;
						var descriptor = new MicroserviceDescriptor
						{
							Name = serviceAttribute.MicroserviceName,
							Type = type,
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
													 var paramName = string.IsNullOrEmpty(paramAttr?.ParameterNameOverride)
														 ? param.Name
														 : paramAttr.ParameterNameOverride;
													 return new ClientCallableParameterDescriptor { Name = paramName, Index = i, Type = param.ParameterType };
												 }).ToArray();

								return new ClientCallableDescriptor() { Path = callableName, Scopes = callableScopes, Parameters = parameters, };
							}).ToList();
						}
						else // If no client callables were found in the C#MS, initialize an empty list.
						{
							descriptor.Methods = new List<ClientCallableDescriptor>();
						}

						Descriptors.Add(descriptor);
						AllDescriptors.Add(descriptor);
					}
				}

				void ParseStorageObjectAttributes(IReadOnlyList<MemberAttribute> cachedStorageObjectAttributes)
				{
					// Searches for all unique name collisions.
					var uniqueNameValidationResults = cachedStorageObjectAttributes.GetAndValidateUniqueNamingAttributes<StorageObjectAttribute>();

					// Registers a hint with all name collisions found.
					if (uniqueNameValidationResults.PerNameCollisions.Count > 0)
					{
						var hint = new BeamHintHeader(BeamHintType.Validation, BeamHintDomains.BEAM_CSHARP_MICROSERVICES_CODE_MISUSE, BeamHintIds.ID_STORAGE_OBJECT_NAME_COLLISION);
						_hintStorage.AddOrReplaceHint(hint, uniqueNameValidationResults.PerNameCollisions);
					}

					// Register all configured storage object
					foreach (var storageObjectValResults in uniqueNameValidationResults.PerAttributeNameValidations)
					{
						var serviceAttribute = storageObjectValResults.Pair.AttrAs<StorageObjectAttribute>();
						var type = storageObjectValResults.Pair.InfoAs<Type>();

						// TODO: XXX this is a hacky way to ignore the default microservice...
						if (serviceAttribute.StorageName.ToLower().Equals("xxxx")) continue;

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

			#region Service Deployment

			public const string SERVICE_PUBLISHED_KEY = "service_published_{0}";

			public event Action<ManifestModel, int> OnBeforeDeploy;
			public event Action<ManifestModel, int> OnDeploySuccess;
			public event Action<ManifestModel, string> OnDeployFailed;
			public event Action<IDescriptor, ServicePublishState> OnServiceDeployStatusChanged;
			public event Action<IDescriptor> OnServiceDeployProgress;

			public async System.Threading.Tasks.Task Deploy(ManifestModel model, CancellationToken token, Action<IDescriptor> onServiceDeployed = null, Action<LogMessage> logger = null)
			{
				if (Descriptors.Count == 0) return; // don't do anything if there are no descriptors.

				if (logger == null)
				{
					logger = message => Debug.Log($"[{message.Level}] {message.Timestamp} - {message.Message}");
				}
				var descriptorsCount = Descriptors.Count;

				OnBeforeDeploy?.Invoke(model, descriptorsCount);

				OnDeploySuccess -= HandleDeploySuccess;
				OnDeploySuccess += HandleDeploySuccess;
				OnDeployFailed -= HandleDeployFailed;
				OnDeployFailed += HandleDeployFailed;

				// TODO perform sort of diff, and only do what is required. Because this is a lot of work.
				var de = BeamEditorContext.Default;
				await de.InitializePromise;

				var client = de.GetMicroserviceManager();
				var existingManifest = await client.GetCurrentManifest();
				var existingServiceToState = existingManifest.manifest.ToDictionary(s => s.serviceName);

				var nameToImageId = new Dictionary<string, string>();
				var enabledServices = new List<string>();

				foreach (var descriptor in Descriptors)
				{
					UpdateServiceDeployStatus(descriptor, ServicePublishState.InProgress);

					logger(new LogMessage
					{
						Level = LogLevel.INFO,
						Timestamp = LogMessage.GetTimeDisplay(DateTime.Now),
						Message = $"Building service=[{descriptor.Name}]"
					});

					var buildCommand = new BuildImageCommand(descriptor,
															 includeDebugTools: false,
															 watch: false,
															 pull: true);
					try
					{
						await buildCommand.StartAsync();
					}
					catch (Exception e)
					{
						OnDeployFailed?.Invoke(model, $"Deploy failed due to failed build of {descriptor.Name}: {e}.");
						UpdateServiceDeployStatus(descriptor, ServicePublishState.Failed);

						return;
					}

					if (token.IsCancellationRequested)
					{
						OnDeployFailed?.Invoke(model, $"Cancellation requested after build of {descriptor.Name}.");
						UpdateServiceDeployStatus(descriptor, ServicePublishState.Failed);

						return;
					}

					var uploader = new ContainerUploadHarness();
					var msModel = MicroservicesDataModel.Instance.GetModel<MicroserviceModel>(descriptor);
					uploader.onProgress += msModel.OnDeployProgress;
					uploader.onProgress += (_, __, ___) => OnServiceDeployProgress?.Invoke(descriptor);

					logger(new LogMessage
					{
						Level = LogLevel.INFO,
						Timestamp = LogMessage.GetTimeDisplay(DateTime.Now),
						Message = $"Getting Id service=[{descriptor.Name}]"
					});

					var imageId = await uploader.GetImageId(descriptor);
					if (string.IsNullOrEmpty(imageId))
					{
						OnDeployFailed?.Invoke(model, $"Failed due to failed Docker {nameof(GetImageIdCommand)} for {descriptor.Name}.");
						UpdateServiceDeployStatus(descriptor, ServicePublishState.Failed);
						return;
					}

					nameToImageId.Add(descriptor.Name, imageId);

					if (existingServiceToState.TryGetValue(descriptor.Name, out var existingReference))
					{
						if (existingReference.imageId == imageId)
						{

							logger(new LogMessage
							{
								Level = LogLevel.INFO,
								Timestamp = LogMessage.GetTimeDisplay(DateTime.Now),
								Message = string.Format(CONTAINER_ALREADY_UPLOADED_MESSAGE, descriptor.Name)
							});

							onServiceDeployed?.Invoke(descriptor);
							UpdateServiceDeployStatus(descriptor, ServicePublishState.Published);

							foreach (var storage in descriptor.GetStorageReferences())
							{
								if (!enabledServices.Contains(storage.Name))
									enabledServices.Add(storage.Name);
							}

							continue;
						}
					}

					var entryModel = model.Services[descriptor.Name];
					var serviceDependencies = new List<ServiceDependency>();
					foreach (var storage in descriptor.GetStorageReferences())
					{
						if (!enabledServices.Contains(storage.Name))
							enabledServices.Add(storage.Name);

						serviceDependencies.Add(new ServiceDependency { id = storage.Name, storageType = "storage" });
					}

					entryModel.Dependencies = serviceDependencies;

					logger(new LogMessage
					{
						Level = LogLevel.INFO,
						Timestamp = LogMessage.GetTimeDisplay(DateTime.Now),
						Message = $"Uploading container service=[{descriptor.Name}]"
					});
					await uploader.UploadContainer(descriptor, token, () =>
												   {
													   Debug.Log(string.Format(UPLOAD_CONTAINER_MESSAGE, descriptor.Name));
													   onServiceDeployed?.Invoke(descriptor);
													   UpdateServiceDeployStatus(descriptor, ServicePublishState.Published);
												   },
												   () =>
												   {
													   Debug.LogError(string.Format(CANT_UPLOAD_CONTAINER_MESSAGE, descriptor.Name));
													   if (token.IsCancellationRequested)
													   {
														   OnDeployFailed?.Invoke(model, $"Cancellation requested during upload of {descriptor.Name}.");
														   UpdateServiceDeployStatus(descriptor, ServicePublishState.Failed);
													   }
												   }, imageId);
				}

				logger(new LogMessage
				{
					Level = LogLevel.INFO,
					Timestamp = LogMessage.GetTimeDisplay(DateTime.Now),
					Message = $"Deploying Manifest..."
				});

				// Manifest Building:
				// 1- Find all locally know services and build their references (using the latest uploaded image ids for them).
				var localServiceReferences = nameToImageId.Select(kvp =>
				{
					var sa = model.Services[kvp.Key];
					return new ServiceReference()
					{
						serviceName = sa.Name,
						templateId = sa.TemplateId,
						enabled = sa.Enabled,
						comments = sa.Comment,
						imageId = kvp.Value,
						dependencies = sa.Dependencies
					};
				});

				// 2- Finds all Known Remote Service (and their last uploaded configuration/image id).
				var remoteServiceReferences = model.ServerManifest.Select(kvp => kvp.Value);

				// 3- Join those two lists and configure the enabled status of all services based on the user input (stored in model.Services[serviceName].Enabled)
				var manifest = localServiceReferences.Union(remoteServiceReferences).ToList();
				foreach (var uploadServiceReference in manifest)
				{
					var sa = model.Services[uploadServiceReference.serviceName];
					uploadServiceReference.enabled = sa.Enabled;
				}

				// 4- Make sure we only have each service once on the list.
				manifest = manifest.Distinct(new ServiceReferenceNameComp()).ToList();


				// Identify storages to enable
				// 1- Make a list of all dependencies that are depended on by any of the services that will be enabled
				var allDependenciesThatMustBeEnabled = manifest.Where(serviceRef => serviceRef.enabled).SelectMany(sr => sr.dependencies)
															   .Select(deps => deps.id)
															   .ToList();

				// 2- Only enable storages that are actually depended on by services.
				var storageManifest = model.Storages.Select(kvp =>
				{
					kvp.Value.Enabled &= allDependenciesThatMustBeEnabled.Contains(kvp.Value.Name);
					return new ServiceStorageReference
					{
						id = kvp.Value.Name,
						storageType = kvp.Value.Type,
						templateId = kvp.Value.TemplateId,
						enabled = kvp.Value.Enabled,
					};
				}).ToList();

				await client.Deploy(new ServiceManifest { comments = model.Comment, manifest = manifest, storageReference = storageManifest });
				OnDeploySuccess?.Invoke(model, descriptorsCount);

				logger(new LogMessage
				{
					Level = LogLevel.INFO,
					Timestamp = LogMessage.GetTimeDisplay(DateTime.Now),
					Message = $"Service Deploy Complete"
				});

				void HandleDeploySuccess(ManifestModel _, int __)
				{
					WindowStateUtility.EnableAllWindows();
				}

				void HandleDeployFailed(ManifestModel _, string __)
				{
					WindowStateUtility.EnableAllWindows();
				}
			}

			private struct ServiceReferenceNameComp : IEqualityComparer<ServiceReference>
			{
				public bool Equals(ServiceReference x, ServiceReference y)
				{
					if (ReferenceEquals(x, y))
					{
						return true;
					}

					if (ReferenceEquals(x, null))
					{
						return false;
					}

					if (ReferenceEquals(y, null))
					{
						return false;
					}

					if (x.GetType() != y.GetType())
					{
						return false;
					}

					return x.serviceName == y.serviceName;
				}

				public int GetHashCode(ServiceReference obj)
				{
					return (obj.serviceName != null ? obj.serviceName.GetHashCode() : 0);
				}
			}

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
						var configEntry = MicroserviceConfiguration.Instance.GetEntry(name); //config.FirstOrDefault(s => s.ServiceName == name);
						var descriptor = Descriptors.FirstOrDefault(d => d.Name == configEntry.ServiceName);
						var serviceDependencies = new List<ServiceDependency>();
						if (descriptor != null)
						{
							foreach (var storage in descriptor.GetStorageReferences())
							{
								serviceDependencies.Add(new ServiceDependency { id = storage.Name, storageType = "storage" });
							}
						}

						return new ManifestEntryModel
						{
							Comment = "",
							Name = name,
							Enabled = configEntry?.Enabled ?? true,
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

			private void UpdateServiceDeployStatus(MicroserviceDescriptor descriptor, ServicePublishState status)
			{
				OnServiceDeployStatusChanged?.Invoke(descriptor, status);

				foreach (var storageDesc in descriptor.GetStorageReferences())
					OnServiceDeployStatusChanged?.Invoke(storageDesc, status);
			}

			#endregion

			#region Running Services

			public async Promise<Dictionary<string, string>> GetConnectionStringEnvironmentVariables(MicroserviceDescriptor service)
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
						$"mongodb://{config.LocalInitUser}:{config.LocalInitPass}@gateway.docker.internal:{config.LocalDataPort}";
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

			#region On Script Reload Callbacks

			[DidReloadScripts]
			private static void AutomaticMachine()
			{
				// If we are not initialized, delay the call until we are.
				if (!BeamEditor.IsInitialized || !MicroserviceEditor.IsInitialized)
				{
					EditorApplication.delayCall += AutomaticMachine;
					return;
				}
				var registry = BeamEditor.GetReflectionSystem<Registry>();
				if (DockerCommand.DockerNotInstalled) return;
				try
				{
					foreach (var d in registry.Descriptors)
					{
						GetServiceStateMachine(registry, d);
					}
				}
				catch (DockerNotInstalledException)
				{
					// do not do anything.
				}

				MicroserviceStateMachine GetServiceStateMachine(Registry microserviceRegistry, MicroserviceDescriptor descriptor)
				{
					var key = descriptor.Name;

					if (!microserviceRegistry._serviceToStateMachine.ContainsKey(key))
					{
						var pw = new CheckImageCommand(descriptor);
						pw.WriteLogToUnity = false;
						pw.Start();
						pw.Join();

						var initialState = pw.IsRunning ? MicroserviceState.RUNNING : MicroserviceState.IDLE;

						microserviceRegistry._serviceToStateMachine.Add(key, new MicroserviceStateMachine(descriptor, initialState));
					}

					return microserviceRegistry._serviceToStateMachine[key];
				}
			}

			#endregion
		}
	}
}
