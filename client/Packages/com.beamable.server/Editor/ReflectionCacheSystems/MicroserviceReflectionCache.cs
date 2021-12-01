using Beamable.Common;
using Beamable.Editor.UI.Model;
using Beamable.Server;
using Beamable.Server.Editor;
using Common.Runtime.BeamHints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Editor.ReflectionCacheSystems
{
	[CreateAssetMenu(fileName = "MicroserviceReflectionCache", menuName = "MENUNAME", order = 0)]
	public class MicroserviceReflectionCache : ReflectionCacheUserSystemObject
	{
		public Registry Cache;

		public override IReflectionCacheUserSystem UserSystem => Cache;

		public override IReflectionCacheTypeProvider UserTypeProvider => Cache;

		public override Type UserSystemType => typeof(Registry);

		private void OnEnable()
		{
			Cache = new Registry();
		}

		public class Registry : IReflectionCacheUserSystem, IBeamHintProvider
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

			private List<MicroserviceDescriptor> _descriptors = new List<MicroserviceDescriptor>();
			private List<IDescriptor> _allDescriptors = new List<IDescriptor>();

			private IBeamHintGlobalStorage _hintStorage;

			public void SetStorage(IBeamHintGlobalStorage hintGlobalStorage) => _hintStorage = hintGlobalStorage;

			public void ClearUserCache()
			{
				_serviceToStateMachine.Clear();
				_serviceToBuilder.Clear();
				_storageToBuilder.Clear();

				_descriptors.Clear();
				_allDescriptors.Clear();
			}

			public void ParseFullCachedData(PerBaseTypeCache perBaseTypeCache,
			                                PerAttributeCache perAttributeCache,
			                                IReadOnlyList<IgnoredFromAssemblySweepStrictErrorData> identifiedStrictErrors)
			{
				// TODO: Display BeamHint of validation type for microservices declared in ignored assemblies.
			}

			public void ParseBaseTypeOfInterestData(BaseTypeOfInterest baseType, IReadOnlyList<MemberInfo> cachedSubTypes)
			{
				if (baseType.Equals(MICROSERVICE_BASE_TYPE))
					ParseMicroserviceSubTypes(cachedSubTypes);
			}

			public void ParseAttributeOfInterestData(AttributeOfInterest attributeType, IReadOnlyList<MemberAttributePair> cachedMemberAttributePairs)
			{
				if (attributeType.Equals(MICROSERVICE_ATTRIBUTE))
					ParseMicroserviceAttributes(cachedMemberAttributePairs);
			}

			private void ParseMicroserviceSubTypes(IReadOnlyList<MemberInfo> cachedMicroserviceSubtypes)
			{
				var microserviceAttributePairs = new List<MemberAttributePair>();
				var validationResults = cachedMicroserviceSubtypes
					.GetAndValidateAttributeExistence(MICROSERVICE_ATTRIBUTE,
					                                  info =>
						                                  new AttributeValidationResult<MicroserviceAttribute>(null,
						                                                                                       info,
						                                                                                       ReflectionCache.ValidationResultType.Error,
						                                                                                       $"Microservice sub-class [{info.Name}] does not have the [{nameof(Beamable.Server.MicroserviceAttribute)}]."),
					                                  microserviceAttributePairs);

				// Get all Microservice Attribute usage errors found
				validationResults.SplitValidationResults(out _, out _, out var errors);

				// Register a hint with all its validation errors as the context object
				//if (errors.Count > 0)
				{
					var hint = new BeamHintHeader(BeamHintType.Validation, BeamHintDomains.BEAM_CSHARP_MICROSERVICES_CODE_MISUSE, "MicroserviceAttributeMisuse");
					_hintStorage.AddOrReplaceHint(hint, errors);
				}
			}

			private void ParseMicroserviceAttributes(IReadOnlyList<MemberAttributePair> cachedMicroserviceAttributes)
			{
				// Searches for all unique name collisions.
				var uniqueNameValidationResults = cachedMicroserviceAttributes.GetAndValidateUniqueNamingAttributes<MicroserviceAttribute>();

				// Registers a hint with all name collisions found.
				if (uniqueNameValidationResults.PerNameCollisions.Count > 0)
				{
					var hint = new BeamHintHeader(BeamHintType.Validation, BeamHintDomains.BEAM_CSHARP_MICROSERVICES_CODE_MISUSE, "MicroserviceNameCollision");
					_hintStorage.AddOrReplaceHint(hint, uniqueNameValidationResults.PerNameCollisions);
				}

				// Gets all properly configured microservices
				uniqueNameValidationResults.PerAttributeNameValidations.SplitValidationResults(out var valid, out _, out _);

				// Register all configured microservices 
				foreach (var memberAttributePair in valid.Select(result => result.Pair))
				{
					var serviceAttribute = (MicroserviceAttribute)memberAttributePair.Attribute;
					var type = (Type)memberAttributePair.Info;

					// TODO: XXX this is a hacky way to ignore the default microservice...
					if (serviceAttribute.MicroserviceName.ToLower().Equals("xxxx")) continue;

					var descriptor = new MicroserviceDescriptor {Name = serviceAttribute.MicroserviceName, Type = type, AttributePath = serviceAttribute.GetSourcePath()};
					_descriptors.Add(descriptor);
					_allDescriptors.Add(descriptor);
				}
			}
		}
	}
}
