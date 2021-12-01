using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Beamable.Common.Content;
using Common.Runtime.BeamHints;
using UnityEngine;

namespace Beamable.Common
{
	/// <summary>
	/// TODO: Implement on systems that require access to cached type information.
	/// </summary>
	public interface IReflectionCacheUserSystem : IReflectionCacheTypeProvider
	{
		/// <summary>
		/// Called once on each <see cref="IReflectionCacheUserSystem"/> before building the reflection cache.
		/// Exists mostly to deal with the fact that Unity's initialization hooks are weird and seem to trigger twice when entering playmode. 
		/// </summary>
		void ClearUserCache();

		/// <summary>
		/// Called once per <see cref="ReflectionCache.GenerateReflectionCache"/> invocation after the assembly sweep <see cref="ReflectionCache.RebuildReflectionCache"/> is completed.
		/// </summary>
		/// <param name="perBaseTypeCache">
		/// Current cached Per-Base Type information.
		/// </param>
		/// <param name="perAttributeCache">
		/// Currently cached Per-Attribute information.
		/// </param>
		/// <param name="identifiedStrictErrors">
		/// Any errors coming from <see cref="IgnoreFromBeamableAssemblySweepAttribute.IsStrict"/> checks.
		/// </param>
		void ParseFullCachedData(PerBaseTypeCache perBaseTypeCache, PerAttributeCache perAttributeCache, IReadOnlyList<IgnoredFromAssemblySweepStrictErrorData> identifiedStrictErrors);

		/// <summary>
		/// Called once per declared <see cref="IReflectionCacheTypeProvider.BaseTypesOfInterest"/> with each base type and
		/// the cached list of types for which <see cref="Type.IsAssignableFrom"/> returns true.
		/// </summary>
		/// <param name="baseType">The base type of interest.</param>
		/// <param name="cachedSubTypes">The list of types for which <see cref="Type.IsAssignableFrom"/> returns true.</param>
		void ParseBaseTypeOfInterestData(BaseTypeOfInterest baseType, IReadOnlyList<MemberInfo> cachedSubTypes);

		/// <summary>
		/// Called once per declared <see cref="IReflectionCacheTypeProvider.AttributesOfInterest"/>.
		/// </summary>
		/// <param name="attributeType">The attribute type of interest.</param>
		/// <param name="cachedMemberAttributePairs">
		/// 
		/// </param>
		void ParseAttributeOfInterestData(AttributeOfInterest attributeType, IReadOnlyList<MemberAttributePair> cachedMemberAttributePairs);
	}

	/// <summary>
	/// Implement this interface and call <see cref="ReflectionCache.RegisterTypeProvider"/> to inform the reflection cache that these types are of interest to you.
	/// </summary>
	public interface IReflectionCacheTypeProvider
	{
		/// <summary>
		/// List of <see cref="BaseTypeOfInterest"/> this provider adds to the assembly sweep.
		/// </summary>
		List<BaseTypeOfInterest> BaseTypesOfInterest
		{
			get;
		}

		/// <summary>
		/// List of <see cref="AttributeOfInterest"/> this provider adds to the assembly sweep.
		/// </summary>
		List<AttributeOfInterest> AttributesOfInterest
		{
			get;
		}
	}

	public struct AllowedOnTypeErrorData
	{
		public Type NotAllowedType;
		public ReflectionCache.ValidationResultType ResultType;
		public string NotAllowedReason;
	}

	public struct IgnoredFromAssemblySweepStrictErrorData
	{
		public Type Type;
		public Assembly FoundInAssembly;
		public BaseTypeOfInterest FailedStrictCheckBaseTypeOfInterest;
		public Dictionary<AttributeOfInterest, List<MemberAttributePair>> FailedStrictCheckAttributesOfInterest;
	}

	/// <summary>
	/// Cached List of Types stored by a specific type. Currently only supports Cache-ing by BaseType/Interface.
	///
	/// Used to initialize all reflection based systems with consistent validation and to ensure we are only doing the parsing once.
	/// We can also use this to setup up compile-time validation of our Attribute-based systems such as Content and Microservices.
	/// </summary>
	public partial class ReflectionCache : IBeamHintProvider
	{
		public enum ValidationResultType
		{
			Valid,
			Warning,
			Error
		}

		private const int PRE_ALLOC_SYSTEM_AND_PROVIDER_AMOUNT = 16;
		private const int PRE_ALLOC_TYPE_CACHES_AMOUNT = 256;

		private readonly List<IReflectionCacheTypeProvider> _registeredProvider;
		private readonly List<IReflectionCacheUserSystem> _registeredCacheUserSystems;
		private List<IgnoredFromAssemblySweepStrictErrorData> _invalidTypesInAssembliesErrorData;
		private readonly PerBaseTypeCache _perBaseTypeCache;
		private readonly PerAttributeCache _perAttributeCache;
		private IBeamHintGlobalStorage _hintGlobalStorage;

		public ReflectionCache()
		{
			_registeredProvider = new List<IReflectionCacheTypeProvider>(PRE_ALLOC_SYSTEM_AND_PROVIDER_AMOUNT);
			_registeredCacheUserSystems = new List<IReflectionCacheUserSystem>(PRE_ALLOC_SYSTEM_AND_PROVIDER_AMOUNT);

			_perBaseTypeCache = new PerBaseTypeCache(
				new List<BaseTypeOfInterest>(PRE_ALLOC_TYPE_CACHES_AMOUNT),
				new Dictionary<BaseTypeOfInterest, List<Type>>(PRE_ALLOC_TYPE_CACHES_AMOUNT)
			);

			_perAttributeCache = new PerAttributeCache(
				new List<AttributeOfInterest>(PRE_ALLOC_SYSTEM_AND_PROVIDER_AMOUNT),
				new List<AttributeOfInterest>(PRE_ALLOC_SYSTEM_AND_PROVIDER_AMOUNT),
				new Dictionary<AttributeOfInterest, List<MemberAttributePair>>(PRE_ALLOC_TYPE_CACHES_AMOUNT)
			);
		}

		public void SetStorage(IBeamHintGlobalStorage hintGlobalStorage)
		{
			_hintGlobalStorage = hintGlobalStorage;
			foreach (var userSystem in _registeredCacheUserSystems)
			{
				if(userSystem is IBeamHintProvider hintProviderSystem) 
					hintProviderSystem.SetStorage(hintGlobalStorage); 
			}
		}

		/// <summary>
		/// Call to unregister all <see cref="IReflectionCacheTypeProvider"/> from the <see cref="ReflectionCache"/>.
		/// </summary>
		public void ClearProviders()
		{
			_registeredProvider.Clear();
		}

		/// <summary>
		/// Call to reset all <see cref="_registeredCacheUserSystems"/> to a blank slate and then unregister them from the <see cref="ReflectionCache"/>.
		/// </summary>
		public void ClearUserSystems()
		{
			_registeredCacheUserSystems.ForEach(sys => sys.ClearUserCache());
			_registeredCacheUserSystems.Clear();
		}

		/// <summary>
		/// Register a <see cref="IReflectionCacheTypeProvider"/> with the cache.
		/// This must be called before Beamable's initialization or you must manage the initialization of this cache yourself.
		/// <para/>
		/// Type providers define which types should the ReflectionCache gather up and build maps around. See the properties of <see cref="IReflectionCacheTypeProvider"/> for more info on how they
		/// are used. 
		/// </summary>        
		public void RegisterTypeProvider(IReflectionCacheTypeProvider provider)
		{
			Debug.Assert(provider != null, "Provider cannot be null. Please ensure the provider instance exists when passing it in here.");
			Debug.Assert(!_registeredProvider.Contains(provider), "Already registered this provider --- Please ensure providers are registered a single time. " +
			                                                     "This is makes the Assembly Sweep more efficient.");

			_registeredProvider.Add(provider);

			// TODO: Add warning message for registering the same type twice.
			// TODO: Add warning message for registering classes that implement a registered interface or similar cases.
		}

		/// <summary>
		/// Registers a <see cref="IReflectionCacheUserSystem"/> with the Reflection Cache System.
		/// This must be called before Beamable's initialization or you must manage the initialization of this cache yourself.
		/// <para/>
		/// User systems define callback implementations that get processed on Beamable initialization when the Scripting Define Symbol <b>BEAMABLE_DISABLE_CACHE_INITIALIZATION</b> is not defined.
		/// If it is defined, you must register Beamable's systems yourself via this function call. If you don't, Beamable will not work.
		/// <para/>
		/// You can find Beamable's systems at: API.cs and EditorAPI.cs, for runtime dependencies and editor dependencies respectively. 
		/// </summary>        
		public void RegisterCacheUserSystem(IReflectionCacheUserSystem system)
		{
			Debug.Assert(system != null, "System cannot be null. Please ensure the system instance exists when passing it in here.");
			Debug.Assert(!_registeredCacheUserSystems.Contains(system), "Already registered this system --- Please ensure systems are registered a single time. " +
			                                                           "This is makes the Assembly Sweep more efficient and makes it so that you run the system callbacks run only once.");

			_registeredCacheUserSystems.Add(system);
		}

		/// <summary>
		/// This is a very slow function call. It triggers a full sweep of all assemblies in the project and regenerate <see cref="_perBaseTypeCache"/> and <see cref="_perAttributeCache"/>.
		/// Then, it invokes all callbacks of all <see cref="IReflectionCacheUserSystem"/> registered via <see cref="RegisterCacheUserSystem"/>.
		/// </summary>
		/// <param name="excludedReflectionUserCaches">
		/// Excludes types implementing <see cref="IReflectionCacheUserSystem"/> from having their callbacks called.
		/// </param>
		public void GenerateReflectionCache(List<Type> excludedReflectionUserCaches = null)
		{
			System.Diagnostics.Debug.Assert(_hintGlobalStorage != null, 
			                                $"A Reflection Cache must have a {nameof(IBeamHintGlobalStorage)} instance! Please call {nameof(SetStorage)} before calling this method!");
			
			RebuildReflectionCache();
			RebuildReflectionUserSystems(excludedReflectionUserCaches);
		}

		/// <summary>
		/// This is a very slow function call. It triggers a full sweep of all assemblies in the project and regenerate <see cref="_perBaseTypeCache"/> and <see cref="_perAttributeCache"/>.
		/// Strive to call this once at initialization or editor reload.
		/// </summary>
		private void RebuildReflectionCache()
		{
			var baseTypesOfInterest = _registeredProvider.SelectMany(provider => provider.BaseTypesOfInterest).ToList();
			var attributesOfInterest = _registeredProvider.SelectMany(provider => provider.AttributesOfInterest).ToList();

			_perBaseTypeCache.BaseTypes.Clear();
			_perBaseTypeCache.MappedSubtypes.Clear();

			_perAttributeCache.AttributeTypes.Clear();
			_perAttributeCache.MemberAttributeTypes.Clear();
			_perAttributeCache.AttributeMappings.Clear();

			BuildTypeCaches(in _perBaseTypeCache, in _perAttributeCache, in baseTypesOfInterest, in attributesOfInterest, out _invalidTypesInAssembliesErrorData);

			// TODO: Decide what to do for results of validation for strict IgnoreFromAssemblySweepAttributes (InvalidTypesInAssembliesErrorData).  
		}

		/// <summary>
		/// Goes through all <see cref="_registeredCacheUserSystems"/> and invokes their callbacks with the currently cached data.
		/// </summary>
		/// <param name="excludedReflectionUserCaches">
		/// Excludes types implementing <see cref="IReflectionCacheUserSystem"/> from having their callbacks called.
		/// </param>
		private void RebuildReflectionUserSystems(List<Type> excludedReflectionUserCaches = null)
		{
			excludedReflectionUserCaches = excludedReflectionUserCaches ?? new List<Type>();

			// Pass down to each given system only the types they are interested in
			foreach (var reflectionBasedSystem in _registeredCacheUserSystems)
			{
				if (excludedReflectionUserCaches.Contains(reflectionBasedSystem.GetType()))
				{
					BeamableLogger.Log($"Skipping Reflection User System [{reflectionBasedSystem.GetType().Name}] on this rebuild");
					continue;
				}

				reflectionBasedSystem.ParseFullCachedData(_perBaseTypeCache, _perAttributeCache, _invalidTypesInAssembliesErrorData);
				foreach (var type in reflectionBasedSystem.BaseTypesOfInterest)
				{
					reflectionBasedSystem.ParseBaseTypeOfInterestData(type, _perBaseTypeCache.MappedSubtypes[type]);
				}

				foreach (var attributeType in reflectionBasedSystem.AttributesOfInterest)
				{
					reflectionBasedSystem.ParseAttributeOfInterestData(attributeType, _perAttributeCache.AttributeMappings[attributeType]);
				}
			}
		}

		/// <summary>
		/// Internal method that generates, given a list of base types, a dictionary of each type that <see cref="Type.IsAssignableFrom"/> to each base type. 
		/// </summary>
		private void BuildTypeCaches(in PerBaseTypeCache perBaseTypeLists,
		                             in PerAttributeCache perAttributeLists,
		                             in List<BaseTypeOfInterest> baseTypesOfInterest,
		                             in List<AttributeOfInterest> attributesOfInterest,
		                             out List<IgnoredFromAssemblySweepStrictErrorData> ignoredFromAssemblySweepStrictErrorData)
		{
			// Initialize Per-Base Cache
			{
				perBaseTypeLists.BaseTypes.AddRange(baseTypesOfInterest);
				foreach (var baseType in baseTypesOfInterest)
				{
					perBaseTypeLists.MappedSubtypes.Add(baseType, new List<Type>());
				}
			}

			// Initialize Per-Attribute Cache
			{
				// Split attributes between declared over types and declared over members 
				var attrTypesSplit = attributesOfInterest.GroupBy(attrOfInterest => attrOfInterest.IsDeclaredMember).ToList();

				// Clear the existing list
				perAttributeLists.AttributeTypes.Clear();
				perAttributeLists.MemberAttributeTypes.Clear();

				// Add the correct subset to each list
				perAttributeLists.AttributeTypes.AddRange(attrTypesSplit.Where(group => !group.Key).SelectMany(group => group));
				perAttributeLists.MemberAttributeTypes.AddRange(attrTypesSplit.Where(group => group.Key).SelectMany(group => group));

				foreach (var attrType in attributesOfInterest)
				{
					perAttributeLists.AttributeMappings.Add(attrType, new List<MemberAttributePair>());
				}
			}

			// TODO: Use TypeCache in editor and Unity 2019 and above ---  This path should go through BEAMABLE_MICROSERVICE || DB_MICROSERVICE
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();

			// Groups by whether or not the assembly has the IgnoreFromBeamableAssemblySweepAttribute.
			var checkedOrIgnoredAssemblySplit = assemblies
			                                    .GroupBy(asm => asm.GetCustomAttributes(typeof(IgnoreFromBeamableAssemblySweepAttribute)).FirstOrDefault())
			                                    .ToList();

			// Gets all groups that don't have the IgnoreFromBeamableAssemblySweepAttribute and parse them
			{
				var validAssemblies = checkedOrIgnoredAssemblySplit
				                      .Where(group => group.Key == null)
				                      .SelectMany(group => group.ToList())
				                      .ToList();

				foreach (var assembly in validAssemblies)
				{
					var types = assembly.GetTypes();
					foreach (var type in types)
					{
						// Get a list of all attributes of interest that were found on this type.
						GatherMemberAttributePairsFromAttributesOfInterest(type,
						                                                   perAttributeLists.AttributeTypes,
						                                                   perAttributeLists.MemberAttributeTypes,
						                                                   perAttributeLists.AttributeMappings);

						// Check for base types of interest                        
						if (TryFindBaseTypesOfInterest(type, baseTypesOfInterest, out var foundType))
						{
							if (perBaseTypeLists.MappedSubtypes.TryGetValue(foundType, out var baseTypesList))
								baseTypesList.Add(type);
						}
					}
				}
			}

			// Gets all groups that do have the IgnoreFromBeamableAssemblySweepAttribute and parse them when in editor
			{
				// If we are in the editor, sweep the invalid assemblies to enforce that
				ignoredFromAssemblySweepStrictErrorData = new List<IgnoredFromAssemblySweepStrictErrorData>();
#if UNITY_EDITOR
				var invalidAssemblies = checkedOrIgnoredAssemblySplit
				                        .Where(group => group.Key != null)
				                        .SelectMany(group => group.Select(asm => ((IgnoreFromBeamableAssemblySweepAttribute)group.Key, asm)))
				                        .ToList();

				// Preallocate buffer so we don't keep allocating stuff over and over inside the sweep --- just clearing. This is a large performance boost since allocations are heavy.            
				var matchingAttributesMapping = new Dictionary<AttributeOfInterest, List<MemberAttributePair>>(perAttributeLists.TotalAttributesOfInterestCount);
				for (var i = 0; i < perAttributeLists.AttributeTypes.Count; i++) matchingAttributesMapping.Add(perAttributeLists.AttributeTypes[i], new List<MemberAttributePair>(128));
				for (var i = 0; i < perAttributeLists.MemberAttributeTypes.Count; i++) matchingAttributesMapping.Add(perAttributeLists.MemberAttributeTypes[i], new List<MemberAttributePair>(128));

				foreach (var (assemblySweepAttribute, asm) in invalidAssemblies)
				{
					if (!assemblySweepAttribute.IsStrict)
					{
						_hintGlobalStorage.AddOrReplaceHint(BeamHintType.Hint, BeamHintDomains.BEAM_REFLECTION_CACHE, $"Skip_Strict_{asm.GetName().Name}", asm);
						BeamableLogger.Log($"Ignoring Assembly [{asm.FullName}] from Reflection Cache Sweep. There may be relevant types in this assembly that you are missing.\n" +
						                   $"Use IgnoreFromAssemblySweepAttribute to get a list of all types if you want to know for sure.");
						continue;
					}

					var types = asm.GetTypes();
					foreach (var type in types)
					{
						var isOfBaseTypeOfInterest = TryFindBaseTypesOfInterest(type, baseTypesOfInterest, out var foundType);

						matchingAttributesMapping.Clear();
						GatherMemberAttributePairsFromAttributesOfInterest(type, perAttributeLists.AttributeTypes, perAttributeLists.MemberAttributeTypes, matchingAttributesMapping);
						var hasOfAttributeOfInterest = matchingAttributesMapping.Any();

						if (isOfBaseTypeOfInterest || hasOfAttributeOfInterest)
						{
							ignoredFromAssemblySweepStrictErrorData.Add(new IgnoredFromAssemblySweepStrictErrorData
							{
								Type = type,
								FoundInAssembly = asm,
								FailedStrictCheckBaseTypeOfInterest = foundType,
								FailedStrictCheckAttributesOfInterest = matchingAttributesMapping
							});
						}
					}
				}
#endif
			}
		}
	}

	public static partial class ReflectionCacheExtensions
	{
		public static string ToHumanReadableSignature(this MethodInfo info)
		{
			return $"{info.ReturnType.Name}({string.Join(",", info.GetParameters().Select(param => $"{param.ParameterType.Name} {param.Name}"))})";
		}
	}
}
