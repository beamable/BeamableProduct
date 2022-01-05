using Beamable.Common.Assistant;
using Beamable.Common.Reflection;
using Beamable.Editor.Assistant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Reflection
{
	[CreateAssetMenu(fileName = "BeamHintDetailsReflectionCache", menuName = "Beamable/Reflection/Assistant/Hint Details Cache", order = 0)]
	public class BeamHintDetailsReflectionCache : ReflectionCacheUserSystemObject
	{
		[NonSerialized] private Registry _cache;

		public override IReflectionCacheUserSystem UserSystem => _cache;

		public override IReflectionCacheTypeProvider UserTypeProvider => _cache;

		public override Type UserSystemType => typeof(Registry);

		private BeamHintDetailsReflectionCache()
		{
			_cache = new Registry();
		}

		public class Registry : IReflectionCacheUserSystem, IBeamHintProvider
		{
			private static readonly BaseTypeOfInterest BEAM_HINT_DETAIL_CONVERTER_PROVIDER_TYPE;
			private static readonly List<BaseTypeOfInterest> BASE_TYPES_OF_INTEREST;

			private static readonly AttributeOfInterest BEAM_HINT_DETAIL_CONVERTER_ATTRIBUTE;
			private static readonly List<AttributeOfInterest> ATTRIBUTES_OF_INTEREST;

			static Registry()
			{
				BEAM_HINT_DETAIL_CONVERTER_PROVIDER_TYPE = new BaseTypeOfInterest(typeof(BeamHintDetailConverterProvider), true);
				BEAM_HINT_DETAIL_CONVERTER_ATTRIBUTE = new AttributeOfInterest(typeof(BeamHintDetailConverterAttribute), new Type[] { }, new[] {typeof(BeamHintDetailConverterProvider)});

				BASE_TYPES_OF_INTEREST = new List<BaseTypeOfInterest>() {BEAM_HINT_DETAIL_CONVERTER_PROVIDER_TYPE,};
				ATTRIBUTES_OF_INTEREST = new List<AttributeOfInterest>() {BEAM_HINT_DETAIL_CONVERTER_ATTRIBUTE,};
			}

			public List<BaseTypeOfInterest> BaseTypesOfInterest => BASE_TYPES_OF_INTEREST;
			public List<AttributeOfInterest> AttributesOfInterest => ATTRIBUTES_OF_INTEREST;

			public List<BeamHintDetailsConfig> LoadedConfigs;
			public List<Delegate> ConverterDelegates;

			private IBeamHintGlobalStorage _hintStorage;

			public Registry()
			{
				LoadedConfigs = new List<BeamHintDetailsConfig>(16);
				ConverterDelegates = new List<Delegate>(16);
			}

			public int GetFirstMatchingDetailsConfig(BeamHintHeader hint, out BeamHintDetailsConfig foundConfig)
			{
				LoadedConfigs.RemoveAll(config => config == null);
				
				var idx = LoadedConfigs.FindIndex(config => config.MatchesHint(hint));
				foundConfig = idx == -1 ? null : LoadedConfigs[idx];
				return idx;
			}

			public BeamHintDetailConverterProvider.DefaultConverterSignature GetConverterAtIdx(int idx)
			{
				return (BeamHintDetailConverterProvider.DefaultConverterSignature)ConverterDelegates[idx];
			}

			public void SetStorage(IBeamHintGlobalStorage hintGlobalStorage) => _hintStorage = hintGlobalStorage;

			public void ClearUserCache()
			{
				LoadedConfigs.Clear();
			}

			public void ParseFullCachedData(PerBaseTypeCache perBaseTypeCache,
			                                PerAttributeCache perAttributeCache)
			{
				// Do nothing here for now --- these errors should be impossible in this case as all converter methods (beamable and user)
				// are defined in the same partial class. This means that if the type is ignored, we ignore it from all assemblies.
			}

			public void ParseBaseTypeOfInterestData(BaseTypeOfInterest baseType, IReadOnlyList<MemberInfo> cachedSubTypes)
			{
				// We Don't actually care about this type --- we care about its members
			}

			public void ParseAttributeOfInterestData(AttributeOfInterest attributeType, IReadOnlyList<MemberAttributePair> cachedMemberAttributePairs)
			{
				var validationResults = cachedMemberAttributePairs.Validate();
				validationResults.SplitValidationResults(out var valid, out _, out var invalid);

				if (invalid.Count > 0)
				{
					_hintStorage.AddOrReplaceHint(BeamHintType.Validation, BeamHintDomains.BEAM_ASSISTANT_CODE_MISUSE, BeamHintIds.ID_MISCONFIGURED_HINT_DETAILS_PROVIDER, invalid);
				}

				var validConverters = valid.Select(result => result.Pair);
				foreach (var cachedMemberAttributePair in validConverters)
				{
					var attribute = (BeamHintDetailConverterAttribute)cachedMemberAttributePair.Attribute;
					var methodInfo = (MethodInfo)cachedMemberAttributePair.Info;

					// Load Config Scriptable Objects
					BeamHintDetailsConfig config = null;
					if (!string.IsNullOrEmpty(attribute.UserOverridePathToHintDetailConfig))
						config = AssetDatabase.LoadAssetAtPath<BeamHintDetailsConfig>(attribute.UserOverridePathToHintDetailConfig);

					if (config == null)
						config = AssetDatabase.LoadAssetAtPath<BeamHintDetailsConfig>(attribute.PathToBeamHintDetailConfig);

					LoadedConfigs.Add(config);

					// Cache a built delegate to be called as a converter. 
					var cachedDelegate = Delegate.CreateDelegate(attribute.DelegateType, methodInfo);
					ConverterDelegates.Add(cachedDelegate);
				}
			}
		}
	}
}
