using Beamable.Common.Assistant;
using Beamable.Common.Reflection;
using Beamable.Editor.Reflection;
using Beamable.Editor.UI.Model;
using Beamable.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Beamable.Server.Editor
{
	[CreateAssetMenu(fileName = "MicroserviceReflectionCache", menuName = "Beamable/Reflection/Server/Microservices Cache", order = 0)]
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
			private static readonly List<BaseTypeOfInterest> BASE_TYPES_OF_INTEREST;

			private static readonly AttributeOfInterest MICROSERVICE_ATTRIBUTE;
			private static readonly List<AttributeOfInterest> ATTRIBUTES_OF_INTEREST;

			static Registry()
			{
				MICROSERVICE_BASE_TYPE = new BaseTypeOfInterest(typeof(Microservice));
				MICROSERVICE_ATTRIBUTE = new AttributeOfInterest(typeof(MicroserviceAttribute), new Type[] { }, new[] {typeof(Microservice)});

				BASE_TYPES_OF_INTEREST = new List<BaseTypeOfInterest>() {MICROSERVICE_BASE_TYPE,};
				ATTRIBUTES_OF_INTEREST = new List<AttributeOfInterest>() {MICROSERVICE_ATTRIBUTE,};
			}

			public List<BaseTypeOfInterest> BaseTypesOfInterest => BASE_TYPES_OF_INTEREST;
			public List<AttributeOfInterest> AttributesOfInterest => ATTRIBUTES_OF_INTEREST;

			private Dictionary<string, MicroserviceStateMachine> _serviceToStateMachine = new Dictionary<string, MicroserviceStateMachine>();
			private Dictionary<string, MicroserviceBuilder> _serviceToBuilder = new Dictionary<string, MicroserviceBuilder>();
			private Dictionary<string, MongoStorageBuilder> _storageToBuilder = new Dictionary<string, MongoStorageBuilder>();

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
			}

			public void OnAttributeOfInterestFound(AttributeOfInterest attributeType, IReadOnlyList<MemberAttribute> cachedMemberAttributes)
			{
				if (attributeType.Equals(MICROSERVICE_ATTRIBUTE))
					ParseMicroserviceAttributes(cachedMemberAttributes);
			}

			private void ParseMicroserviceSubTypes(IReadOnlyList<MemberInfo> cachedMicroserviceSubtypes)
			{
				var validationResults = cachedMicroserviceSubtypes
					.GetAndValidateAttributeExistence(MICROSERVICE_ATTRIBUTE,
					                                  info => {
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

			private void ParseMicroserviceAttributes(IReadOnlyList<MemberAttribute> cachedMicroserviceAttributes)
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
					var descriptor = new MicroserviceDescriptor {
						Name = serviceAttribute.MicroserviceName,
						Type = type,
						AttributePath = serviceAttribute.GetSourcePath(),
						HasValidationError = hasError,
						HasValidationWarning = hasWarning,
					};

					// Add client callables for this microservice type
					var clientCallablesFound = validClientCallablesLookup.TryGetValue(type, out var clientCallables);

					// Generates descriptors for each of the individual client callables.
					descriptor.Methods = !clientCallablesFound ?
						// If no client callables were found in the C#MS, initialize an empty list.
						new List<ClientCallableDescriptor>() :
						
						// Otherwise, initialize the ClientCallableDescriptors.
						clientCallables.Select(delegate(MemberAttribute pair) {
						var clientCallableAttribute = pair.AttrAs<ClientCallableAttribute>();
						var clientCallableMethod = pair.InfoAs<MethodInfo>();

						var callableName = pair.GetOptionalNameOrMemberName<ClientCallableAttribute>();
						var callableScopes = clientCallableAttribute.RequiredScopes;

						var parameters = clientCallableMethod
						                 .GetParameters()
						                 .Select((param, i) => {
							                 var paramAttr = param.GetCustomAttribute<ParameterAttribute>();
							                 var paramName = string.IsNullOrEmpty(paramAttr?.ParameterNameOverride)
								                 ? param.Name
								                 : paramAttr.ParameterNameOverride;
							                 return new ClientCallableParameterDescriptor {
								                 Name = paramName,
								                 Index = i,
								                 Type = param.ParameterType
							                 };
						                 }).ToArray();

						return new ClientCallableDescriptor() {
							Path = callableName,
							Scopes = callableScopes,
							Parameters = parameters,
						};
					}).ToList();

					Descriptors.Add(descriptor);
					AllDescriptors.Add(descriptor);
				}
			}
		}
	}
}
