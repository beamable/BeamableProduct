using Beamable.Common.Assistant;
using Beamable.Common.Reflection;
using Beamable.Editor.Assistant;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Reflection
{
	[CreateAssetMenu(fileName = "BeamHintDetailsReflectionCache", menuName = "Beamable/Reflection/Assistant/Hint Details Cache", order = 0)]
	public class BeamHintReflectionCache : ReflectionCacheUserSystemObject
	{
		[NonSerialized] private Registry _cache;

		public override IReflectionCacheUserSystem UserSystem => _cache;

		public override IReflectionCacheTypeProvider UserTypeProvider => _cache;

		public override Type UserSystemType => typeof(Registry);

		private BeamHintReflectionCache()
		{
			_cache = new Registry();
		}

		public delegate void DefaultConverter(in BeamHint hint, in BeamHintTextMap textMap, BeamHintVisualsInjectionBag injectionBag);
		
		public class Registry : IReflectionCacheUserSystem, IBeamHintProvider
		{
			private static readonly BaseTypeOfInterest BEAM_HINT_DETAIL_CONVERTER_PROVIDER_TYPE;
			private static readonly List<BaseTypeOfInterest> BASE_TYPES_OF_INTEREST;

			private static readonly AttributeOfInterest BEAM_HINT_DOMAIN_PROVIDER_ATTRIBUTE;
			private static readonly AttributeOfInterest BEAM_HINT_ID_PROVIDER_ATTRIBUTE;
			private static readonly AttributeOfInterest BEAM_HINT_DETAIL_CONVERTER_ATTRIBUTE;
			private static readonly List<AttributeOfInterest> ATTRIBUTES_OF_INTEREST;

			static Registry()
			{
				BEAM_HINT_DETAIL_CONVERTER_PROVIDER_TYPE = new BaseTypeOfInterest(typeof(BeamHintDetailConverterProvider), true);
				BEAM_HINT_DETAIL_CONVERTER_ATTRIBUTE = new AttributeOfInterest(typeof(BeamHintDetailConverterAttribute), new Type[] { }, new[] {typeof(BeamHintDetailConverterProvider)});

				BEAM_HINT_DOMAIN_PROVIDER_ATTRIBUTE = new AttributeOfInterest(typeof(BeamHintDomainAttribute), new Type[] { }, new[] {typeof(BeamHintDomainProvider)});
				BEAM_HINT_ID_PROVIDER_ATTRIBUTE = new AttributeOfInterest(typeof(BeamHintIdAttribute), new Type[] { }, new Type[] {typeof(BeamHintIdProvider)});
				
				BASE_TYPES_OF_INTEREST = new List<BaseTypeOfInterest>() {BEAM_HINT_DETAIL_CONVERTER_PROVIDER_TYPE,};
				ATTRIBUTES_OF_INTEREST = new List<AttributeOfInterest>() {BEAM_HINT_DOMAIN_PROVIDER_ATTRIBUTE, BEAM_HINT_ID_PROVIDER_ATTRIBUTE, BEAM_HINT_DETAIL_CONVERTER_ATTRIBUTE,};
			}

			public List<BaseTypeOfInterest> BaseTypesOfInterest => BASE_TYPES_OF_INTEREST;
			public List<AttributeOfInterest> AttributesOfInterest => ATTRIBUTES_OF_INTEREST;

			private readonly Dictionary<string, List<string>> _perProviderDomains;
			private readonly Dictionary<string, List<string>> _perProviderIds;

			private readonly List<BeamHintDetailsConfig> _loadedConfigs;
			private readonly List<BeamHintTextMap> _loadedTextMaps;
			private readonly List<ConverterData<DefaultConverter>> _defaultConverterDelegates;

			private IBeamHintGlobalStorage _hintStorage;

			public Registry()
			{
				_perProviderDomains = new Dictionary<string, List<string>>(16);
				_perProviderIds = new Dictionary<string, List<string>>(16);
				
				_loadedConfigs = new List<BeamHintDetailsConfig>(16);
				_loadedTextMaps = new List<BeamHintTextMap>(16);
				_defaultConverterDelegates = new List<ConverterData<DefaultConverter>>(16);
			}

			public void ReloadHintTextMapScriptableObjects(List<string> hintConfigPaths)
			{
				var beamHintTextMapGuids = AssetDatabase.FindAssets("t:BeamHintTextMap", hintConfigPaths
				                                                                                      .Where(Directory.Exists)
				                                                                                      .ToArray());
				
				// Reload Detail Config Scriptable Objects
				foreach (string beamHintTextMapGuid in beamHintTextMapGuids)
				{
					var assetPath = AssetDatabase.GUIDToAssetPath(beamHintTextMapGuid);
					var hintTextMap = AssetDatabase.LoadAssetAtPath<BeamHintTextMap>(assetPath);
					_loadedTextMaps.Add(hintTextMap);
				}

				
				// Update Configured Converters to ensure they point to the correct HintDetailsConfigs after the reload
				for (var i = 0; i < _defaultConverterDelegates.Count; i++)
				{
					var converterData = _defaultConverterDelegates[i];
					_defaultConverterDelegates[i] = BuildConverterData(converterData.Matcher.MatchType, converterData.Matcher.Domain, converterData.Matcher.IdRegex,
					                                                   converterData.UserOverrideHintConfigDetailsConfigId,  converterData.HintConfigDetailsConfigId, converterData.ConverterCall);
				}
			}
			
			public void ReloadHintDetailConfigScriptableObjects(List<string> hintConfigPaths)
			{
				var beamHintDetailsConfigsGuids = AssetDatabase.FindAssets("t:BeamHintDetailsConfig", hintConfigPaths
				                                                                                      .Where(Directory.Exists)
				                                                                                      .ToArray());
				
				// Reload Detail Config Scriptable Objects
				foreach (string beamHintDetailConfigGuid in beamHintDetailsConfigsGuids)
				{
					var assetPath = AssetDatabase.GUIDToAssetPath(beamHintDetailConfigGuid);
					var hintDetailsConfig = AssetDatabase.LoadAssetAtPath<BeamHintDetailsConfig>(assetPath);
					_loadedConfigs.Add(hintDetailsConfig);
				}

				
				// Update Configured Converters to ensure they point to the correct HintDetailsConfigs after the reload
				for (var i = 0; i < _defaultConverterDelegates.Count; i++)
				{
					var converterData = _defaultConverterDelegates[i];
					_defaultConverterDelegates[i] = BuildConverterData(converterData.Matcher.MatchType, converterData.Matcher.Domain, converterData.Matcher.IdRegex,
						converterData.UserOverrideHintConfigDetailsConfigId,  converterData.HintConfigDetailsConfigId, converterData.ConverterCall);
				}
				
			}

			public bool TryGetConverterDataForHint(BeamHintHeader header, out ConverterData<DefaultConverter> converter)
			{
				var firstMatchingConverterIdx = _defaultConverterDelegates.FindIndex(cvt => cvt.Matcher.MatchAgainstHeader(header));
				if (firstMatchingConverterIdx != -1)
				{
					converter = _defaultConverterDelegates[firstMatchingConverterIdx];
					return true;	
				}
				
				converter = default;
				return false;
			}

			public bool TryGetHintTitleText(BeamHintHeader header, out string titleText)
			{
				for (var i = 0; i < _loadedTextMaps.Count; i++)
				{
					var txtMap = _loadedTextMaps[i];

					if (txtMap.TryGetHintTitle(header, out titleText))
						return true;
				}

				titleText = header.Id;
				return false;
			}
			
			public bool TryGetDomainTitleText(string domainSubString, out string titleText)
			{
				for (var i = 0; i < _loadedTextMaps.Count; i++)
				{
					var txtMap = _loadedTextMaps[i];

					if (txtMap.TryGetDomainTitle(domainSubString, out titleText))
						return true;
				}

				titleText = domainSubString;
				return false;
			}
			

			public void SetStorage(IBeamHintGlobalStorage hintGlobalStorage) => _hintStorage = hintGlobalStorage;

			public void ClearUserCache()
			{
				_perProviderDomains.Clear();
				_perProviderIds.Clear();
				_defaultConverterDelegates.Clear();
			}

			public void ParseFullCachedData(PerBaseTypeCache perBaseTypeCache,
			                                PerAttributeCache perAttributeCache)
			{
				
			}

			public void ParseBaseTypeOfInterestData(BaseTypeOfInterest baseType, IReadOnlyList<MemberInfo> cachedSubTypes)
			{
				// We Don't actually care about this type --- we care about its members
			}

			public void ParseAttributeOfInterestData(AttributeOfInterest attributeType, IReadOnlyList<MemberAttributePair> cachedMemberAttributePairs)
			{
				// Handle Beam Hint Domain providers
				if (attributeType.Equals(BEAM_HINT_DOMAIN_PROVIDER_ATTRIBUTE))
				{
					// TODO: Store domains in whatever way makes it easier for users to get the list of domains they are interested in. 
					foreach (var domainFields in cachedMemberAttributePairs.Select(result => result.Info).Cast<FieldInfo>())
					{
						var providerName = domainFields.DeclaringType?.FullName ?? string.Empty;
						if (!_perProviderDomains.TryGetValue(providerName, out var domainList))
						{
							domainList = new List<string>();
							_perProviderDomains.Add(providerName, domainList);
						}

						domainList.Add((string)domainFields.GetValue(null));
					}
				}

				// Handle Beam Hint Id providers
				if (attributeType.Equals(BEAM_HINT_ID_PROVIDER_ATTRIBUTE))
				{
					// TODO: Store domains in whatever way makes it easier for users to get the list of domains they are interested in. 
					foreach (var idField in cachedMemberAttributePairs.Select(result => result.Info).Cast<FieldInfo>())
					{
						var providerName = idField.DeclaringType?.FullName ?? string.Empty;
						if (!_perProviderIds.TryGetValue(providerName, out var idsList))
						{
							idsList = new List<string>();
							_perProviderIds.Add(providerName, idsList);
						}

						idsList.Add((string)idField.GetValue(null));
					}
				}
				
				// Handle Beam Hint Id providers
				if (attributeType.Equals(BEAM_HINT_DETAIL_CONVERTER_ATTRIBUTE))
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

						// Cache a built delegate to be called as a converter. 
						if (attribute.DelegateType == typeof(DefaultConverter))
						{
							var cachedDelegate = Delegate.CreateDelegate(attribute.DelegateType, methodInfo) as DefaultConverter;
							_defaultConverterDelegates.Add(BuildConverterData<DefaultConverter>(attribute.MatchType, attribute.Domain, attribute.IdRegex,
							                                                                    attribute.UserOverrideToHintDetailConfigId, attribute.HintDetailConfigId, cachedDelegate));
						}
					}
				}
			}

			private ConverterData<T> BuildConverterData<T>(BeamHintType type, string domain, string idRegex, string userOverrideHintDetailConfigId, string hintDetailConfigId, T cachedDelegate) where T : Delegate
			{
				var userOverrideConfig = _loadedConfigs.FirstOrDefault(cfg => userOverrideHintDetailConfigId == cfg.Id);
				var defaultConfig = _loadedConfigs.FirstOrDefault(cfg => hintDetailConfigId == cfg.Id);

				var textMap = _loadedTextMaps.FirstOrDefault(txt => txt.HintIdToHintTitle.Keys.Any(k => new Regex(idRegex).IsMatch(k)));
				
				return new ConverterData<T> {
					Matcher = new HeaderMatcher(type, domain, idRegex),

					HintConfigDetailsConfigId = hintDetailConfigId,
					UserOverrideHintConfigDetailsConfigId = userOverrideHintDetailConfigId,
					HintConfigDetailsConfig = userOverrideConfig == null ? defaultConfig : userOverrideConfig,
					HintTextMap = textMap,

					ConverterCall = cachedDelegate
				};
			}
		}

		[Serializable]
		public struct ConverterData<T> where T : Delegate
		{
			public HeaderMatcher Matcher;
			
			public string HintConfigDetailsConfigId;
			public string UserOverrideHintConfigDetailsConfigId;
			public BeamHintDetailsConfig HintConfigDetailsConfig;
			public BeamHintTextMap HintTextMap;

			public T ConverterCall;
		}
		
		[Serializable]
		public struct HeaderMatcher
		{
			public BeamHintType MatchType;
			public string Domain;
			public string IdRegex;

			private Regex _regex;

			public HeaderMatcher(BeamHintType matchType, string domain, string idRegex) : this()
			{
				MatchType = matchType;
				Domain = domain;
				IdRegex = idRegex;
				_regex = new Regex(idRegex);
			}

			public bool MatchAgainstHeader([NotNull] BeamHintHeader other)
			{
				var matchType = MatchType.HasFlag(other.Type);
				var matchDomain = string.IsNullOrEmpty(Domain) || other.Domain.Contains(Domain);
				var idMatch = string.IsNullOrEmpty(IdRegex) || _regex.IsMatch(other.Id);

				return matchType && matchDomain && idMatch;
			}
		}
	}
}
