using Beamable.Common.Assistant;
using Beamable.Common.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Beamable.Editor.Reflection
{
	[CreateAssetMenu(fileName = "BeamHintDomainAndIdsCache", menuName = "Beamable/Reflection/Assistant/Hint and Domains Reflection Cache", order = 0)]
	public class BeamHintDomainsAndIdsCache : ReflectionCacheUserSystemObject
	{
		[NonSerialized]
		public Registry Cache;

		public BeamHintDomainsAndIdsCache()
		{
			Cache = new Registry();
		}

		public override IReflectionCacheUserSystem UserSystem => Cache;

		public override IReflectionCacheTypeProvider UserTypeProvider => Cache;

		public override Type UserSystemType => typeof(Registry);

		public class Registry : IReflectionCacheUserSystem
		{
			private static readonly List<BaseTypeOfInterest> BASE_TYPES_OF_INTEREST;

			private static readonly AttributeOfInterest BEAM_HINT_DOMAIN_PROVIDER_ATTRIBUTE;
			private static readonly AttributeOfInterest BEAM_HINT_ID_PROVIDER_ATTRIBUTE;
			private static readonly List<AttributeOfInterest> ATTRIBUTES_OF_INTEREST;

			static Registry()
			{
				BEAM_HINT_DOMAIN_PROVIDER_ATTRIBUTE = new AttributeOfInterest(typeof(BeamHintDomainAttribute), new Type[] { }, new[] {typeof(BeamHintDomainProvider)});
				BEAM_HINT_ID_PROVIDER_ATTRIBUTE = new AttributeOfInterest(typeof(BeamHintIdAttribute), new Type[] { }, new Type[] {typeof(BeamHintIdProvider)});

				BASE_TYPES_OF_INTEREST = new List<BaseTypeOfInterest>() { };
				ATTRIBUTES_OF_INTEREST = new List<AttributeOfInterest>() {BEAM_HINT_ID_PROVIDER_ATTRIBUTE, BEAM_HINT_DOMAIN_PROVIDER_ATTRIBUTE};
			}

			public List<BaseTypeOfInterest> BaseTypesOfInterest => BASE_TYPES_OF_INTEREST;
			public List<AttributeOfInterest> AttributesOfInterest => ATTRIBUTES_OF_INTEREST;

			public Dictionary<string, List<string>> PerProviderDomains;
			public Dictionary<string, List<string>> PerProviderIds;

			public Registry()
			{
				PerProviderDomains = new Dictionary<string, List<string>>(16);
				PerProviderIds = new Dictionary<string, List<string>>(16);
			}

			public void ClearUserCache()
			{
				PerProviderDomains.Clear();
				PerProviderIds.Clear();
			}

			public void ParseFullCachedData(PerBaseTypeCache perBaseTypeCache,
			                                PerAttributeCache perAttributeCache)
			{
				// Do nothing here for now.
			}

			public void ParseBaseTypeOfInterestData(BaseTypeOfInterest baseType, IReadOnlyList<MemberInfo> cachedSubTypes)
			{
				// We don't actually care about subtyping in this system...
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
						if (!PerProviderDomains.TryGetValue(providerName, out var domainList))
						{
							domainList = new List<string>();
							PerProviderDomains.Add(providerName, domainList);
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
						if (!PerProviderIds.TryGetValue(providerName, out var idsList))
						{
							idsList = new List<string>();
							PerProviderIds.Add(providerName, idsList);
						}

						idsList.Add((string)idField.GetValue(null));
					}
				}
			}
		}
	}
}
