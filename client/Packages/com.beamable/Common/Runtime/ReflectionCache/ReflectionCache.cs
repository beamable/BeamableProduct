using Beamable.Common.Assistant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static System.Diagnostics.Debug;

namespace Beamable.Common.Reflection
{
	/// <summary>
	/// Implement on systems that require access to cached type information.
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
		void ParseFullCachedData(PerBaseTypeCache perBaseTypeCache, PerAttributeCache perAttributeCache);

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

	/// <summary>
	/// Cached List of Types stored by a specific type. Currently only supports Cache-ing by BaseType/Interface.
	///
	/// Used to initialize all reflection based systems with consistent validation and to ensure we are only doing the parsing once.
	/// We can also use this to setup up compile-time validation of our Attribute-based systems such as Content and Microservices.
	/// </summary>
	// ReSharper disable once ClassNeverInstantiated.Global
	public partial class ReflectionCache : IBeamHintProvider
	{
		public enum ValidationResultType
		{
			Valid,
			Warning,
			Error,
			
			Discarded
		}

		private const int PRE_ALLOC_SYSTEM_AND_PROVIDER_AMOUNT = 16;
		private const int PRE_ALLOC_TYPE_CACHES_AMOUNT = 256;

		private readonly List<IReflectionCacheTypeProvider> _registeredProvider;
		private readonly List<IReflectionCacheUserSystem> _registeredCacheUserSystems;
		
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
		/// Call to reset all <see cref="_registeredCacheUserSystems"/> to a blank slate.
		/// </summary>
		public void ClearUserSystems()
		{
			_registeredCacheUserSystems.ForEach(sys => sys.ClearUserCache());
		}

		/// <summary>
		/// Returns the first instance of the <see cref="IReflectionCacheUserSystem"/> of type <typeparamref name="T"/> that was previously registered.
		/// </summary>
		public T GetFirstRegisteredUserSystemOfType<T>() where T : IReflectionCacheUserSystem
		{
			return (T) _registeredCacheUserSystems.First(system => system.GetType() == typeof(T));
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
			Assert(provider != null, "Provider cannot be null. Please ensure the provider instance exists when passing it in here.");
			Assert(!_registeredProvider.Contains(provider), "Already registered this provider --- Please ensure providers are registered a single time. " +
			                                                                    "This is makes the Assembly Sweep more efficient.");
			
			// Guard so people don't accidentally shoot themselves in the foot when defining their attributes of interest.
			foreach (var attributeOfInterest in provider.AttributesOfInterest)
			{
				// What this does is:
				//   - If the attribute of interest Has a Method/Constructor/Property/Field/Event Target, we'll look for them into each individual type that's given in the two lists declared here., 
				//   - Will work with structs, classes both declared at root or internal as the Assembly.GetTypes() returns all of these.
				//
				// Assumption 1 ===> Does not need work for parameters or return values --- this is specific enough that each individual user system can do their own thing here.            
				if (attributeOfInterest.Targets.HasFlag(AttributeOfInterest.INTERNAL_TYPE_SEARCH_WHEN_TARGETS))
				{
					// If you didn't tell us where to look, we'd have to look everywhere -- which is terrible for editor performance so we don't support it.
					if (attributeOfInterest.FoundInBaseTypes.Count == 0 && attributeOfInterest.FoundInTypesWithAttributes.Count == 0)
					{
						throw new ArgumentException($"{nameof(AttributeOfInterest)} [{attributeOfInterest.AttributeType.Name}] with these {nameof(AttributeTargets)} [{AttributeOfInterest.INTERNAL_TYPE_SEARCH_WHEN_TARGETS.ToString()}]" +
						                            $"must have at least one entry into the {nameof(attributeOfInterest.FoundInBaseTypes)} or {nameof(attributeOfInterest.FoundInTypesWithAttributes)} lists.\n" +
						                            $"Without it, we would need to go into every existing type which would be bad for editor performance.");
					}
				}   
			}

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
			Assert(system != null, "System cannot be null. Please ensure the system instance exists when passing it in here.");
			Assert(!_registeredCacheUserSystems.Contains(system), "Already registered this system --- Please ensure systems are registered a single time. " +
			                                                           "This is makes the Assembly Sweep more efficient and makes it so that you run the system callbacks run only once.");

			_registeredCacheUserSystems.Add(system);
		}

		/// <summary>
		/// This is a very slow function call. It triggers a full sweep of all assemblies in the project and regenerate <see cref="_perBaseTypeCache"/> and <see cref="_perAttributeCache"/>.
		/// Then, it invokes all callbacks of all <see cref="IReflectionCacheUserSystem"/> registered via <see cref="RegisterCacheUserSystem"/>.
		/// </summary>
		/// <param name="assembliesToSweep"></param>
		/// <param name="excludedReflectionUserCaches">
		///     Excludes types implementing <see cref="IReflectionCacheUserSystem"/> from having their callbacks called.
		/// </param>
		public void GenerateReflectionCache(IReadOnlyList<string> assembliesToSweep, List<Type> excludedReflectionUserCaches = null)
		{
			Assert(_hintGlobalStorage != null, 
			                                $"A Reflection Cache must have a {nameof(IBeamHintGlobalStorage)} instance! Please call {nameof(SetStorage)} before calling this method!");
			
			ClearUserSystems();
			RebuildReflectionCache(assembliesToSweep);
			RebuildReflectionUserSystems(excludedReflectionUserCaches);
		}

		/// <summary>
		/// This is a very slow function call. It triggers a full sweep of all assemblies in the project and regenerate <see cref="_perBaseTypeCache"/> and <see cref="_perAttributeCache"/>.
		/// Strive to call this once at initialization or editor reload.
		/// </summary>
		private void RebuildReflectionCache(IReadOnlyList<string> sortedAssembliesToSweep = null)
		{
			// Clear existing cache
			_perBaseTypeCache.BaseTypes.Clear();
			_perBaseTypeCache.MappedSubtypes.Clear();

			_perAttributeCache.AttributeTypes.Clear();
			_perAttributeCache.MemberAttributeTypes.Clear();
			_perAttributeCache.AttributeMappings.Clear();
			
			
			// Prepare lists of base types and attributes that we care about.
			var baseTypesOfInterest = (IReadOnlyList<BaseTypeOfInterest>) _registeredProvider.SelectMany(provider => provider.BaseTypesOfInterest).ToList();
			var attributesOfInterest = (IReadOnlyList<AttributeOfInterest>) _registeredProvider.SelectMany(provider => provider.AttributesOfInterest).ToList();

			// Prepare lists of assemblies we don't care about or care about preventing people from defining types/attributes of interest in them.
			sortedAssembliesToSweep = sortedAssembliesToSweep ?? new List<string>();

			BuildTypeCaches(in _perBaseTypeCache, 
			                in _perAttributeCache,
			                in baseTypesOfInterest,
			                in attributesOfInterest,
			                in sortedAssembliesToSweep);

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

				reflectionBasedSystem.ParseFullCachedData(_perBaseTypeCache, _perAttributeCache);
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
		                             in IReadOnlyList<BaseTypeOfInterest> baseTypesOfInterest,
		                             in IReadOnlyList<AttributeOfInterest> attributesOfInterest,
		                             in IReadOnlyList<string> sortedAssembliesToSweep)
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
			var assembliesToSweepStr = "₢"+string.Join("₢", sortedAssembliesToSweep)+"₢";
			var checkedOrIgnoredAssemblySplit = assemblies
			                                    .GroupBy(asm => assembliesToSweepStr.Contains("₢"+asm.GetName().Name+"₢"))
			                                    .ToList();
			
			// Gets all groups that don't have the IgnoreFromBeamableAssemblySweepAttribute and parse them
			{
				var validAssemblies = checkedOrIgnoredAssemblySplit
				                      .Where(group => group.Key == true)
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
		}
	}

	public static partial class ReflectionCacheExtensions
	{
		public static string ToHumanReadableSignature(this MethodInfo info)
		{
			var paramsDeclaration = string.Join(",", info.GetParameters().Select(param =>
			{
				var prefix = param.IsOut ? "out " :
					param.IsIn ? "in " :
					param.ParameterType.IsByRef ? "ref " :
					"";

				return $"{prefix}{param.ParameterType.Name} {param.Name}";
			}));
			return $"{info.ReturnType.Name}({paramsDeclaration})";
		}
		
	}

}
