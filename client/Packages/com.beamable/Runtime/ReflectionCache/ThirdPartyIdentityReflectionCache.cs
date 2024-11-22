using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Common.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

namespace Beamable.Reflection
{
#if BEAMABLE_DEVELOPER
	[CreateAssetMenu(fileName = "ThirdPartyIdentityReflectionCache",
	                 menuName = "Beamable/Reflection/Third Party Identities Cache",
	                 order = Constants.MenuItems.Assets.Orders.MENU_ITEM_PATH_ASSETS_BEAMABLE_ORDER_1)]
#endif
	public class ThirdPartyIdentityReflectionCache : ReflectionSystemObject
	{
		private Registry Cache;

		public override IReflectionSystem System => Cache;
		public override IReflectionTypeProvider TypeProvider => Cache;
		public override Type SystemType => typeof(Registry);

		public ThirdPartyIdentityReflectionCache()
		{
			Cache = new Registry();
		}

		public class Registry : IReflectionSystem
		{
			private static readonly BaseTypeOfInterest I_THIRD_PARTY_CLOUD_IDENTITY_INTERFACE = new BaseTypeOfInterest(
				typeof(IFederationId));

			public List<string> ThirdPartiesOptions { get; private set; } = new List<string>();

			public List<BaseTypeOfInterest> BaseTypesOfInterest =>
				new List<BaseTypeOfInterest> { I_THIRD_PARTY_CLOUD_IDENTITY_INTERFACE };

			public List<AttributeOfInterest> AttributesOfInterest => new List<AttributeOfInterest>();

			public void OnBaseTypeOfInterestFound(BaseTypeOfInterest baseType, IReadOnlyList<MemberInfo> cachedSubTypes)
			{
				ThirdPartiesOptions = GetIdentitiesOptions(cachedSubTypes);
			}

			private List<string> GetIdentitiesOptions(IReadOnlyList<MemberInfo> cachedSubTypes)
			{
				List<string> list = new List<string>();

				foreach (MemberInfo info in cachedSubTypes)
				{
					
					if (!(info is Type type)) continue;
					if (type.IsInterface || type.IsAbstract) continue; // cannot create an instance
					
					try
					{
						if (FormatterServices.GetUninitializedObject(type) is IFederationId identity)
						{
							list.Add(identity.GetUniqueName());
						}
					}
					catch (MissingMethodException)
					{
						Debug.LogError($"Beamable unable to understand {nameof(IFederationId)} type=[{type.Name}]");
						throw;
					}
				}

				return list;
			}

			public void ClearCachedReflectionData() { }
			public void OnAttributeOfInterestFound(AttributeOfInterest attributeType,
												   IReadOnlyList<MemberAttribute> cachedMemberAttributes)
			{ }
			public void OnSetupForCacheGeneration() { }
			public void OnReflectionCacheBuilt(PerBaseTypeCache perBaseTypeCache,
											   PerAttributeCache perAttributeCache)
			{ }
		}
	}
}
