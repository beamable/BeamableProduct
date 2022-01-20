using Beamable.Common.Assistant;
using Beamable.Common.Reflection;
using Beamable.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[CreateAssetMenu(menuName = "UserProject/MyNonEditorReflectionSystem", fileName = "MyNonEditorReflectionSystem", order = 0)]
public class MyNonEditorReflectionSystem : ReflectionSystemObject, IReflectionSystem
{
	private static readonly BaseTypeOfInterest MY_INTERESTING_BASE_TYPE;
	private static readonly AttributeOfInterest MY_REFLECTION_ATTRIBUTE;

	private static readonly List<BaseTypeOfInterest> BASE_TYPES_OF_INTEREST;
	private static readonly List<AttributeOfInterest> ATTRIBUTES_OF_INTEREST;

	static MyNonEditorReflectionSystem()
	{
		MY_INTERESTING_BASE_TYPE = new BaseTypeOfInterest(typeof(MyInterestingBaseType));

		MY_REFLECTION_ATTRIBUTE = new AttributeOfInterest(typeof(MyReflectionAttribute),
		                                                  new Type[] { },
		                                                  new[] {typeof(MyInterestingBaseType)});

		BASE_TYPES_OF_INTEREST = new List<BaseTypeOfInterest>() {MY_INTERESTING_BASE_TYPE};
		ATTRIBUTES_OF_INTEREST = new List<AttributeOfInterest>() {MY_REFLECTION_ATTRIBUTE};
	}

	public override IReflectionSystem System => this;
	public override IReflectionTypeProvider TypeProvider => this;
	public override Type SystemType => GetType();

	public List<BaseTypeOfInterest> BaseTypesOfInterest => BASE_TYPES_OF_INTEREST;
	public List<AttributeOfInterest> AttributesOfInterest => ATTRIBUTES_OF_INTEREST;

	public Dictionary<string, MyInterestingBaseType> CachedInstancesOfBaseType;
	public Dictionary<string, List<Func<bool, int>>> CachedFirstSignatureMethods;
	public Dictionary<string, List<Func<bool, string, int>>> CachedSecondSignatureMethods;

	private void OnEnable()
	{
		CachedInstancesOfBaseType = new Dictionary<string, MyInterestingBaseType>();
		CachedFirstSignatureMethods = new Dictionary<string, List<Func<bool, int>>>();
		CachedSecondSignatureMethods = new Dictionary<string, List<Func<bool, string, int>>>();
	}

#if UNITY_EDITOR
	private IBeamHintGlobalStorage _hintGlobalStorage;
#endif

	public void ClearCachedReflectionData()
	{
		if (CachedInstancesOfBaseType == null)
			CachedInstancesOfBaseType = new Dictionary<string, MyInterestingBaseType>();

		if (CachedFirstSignatureMethods == null)
			CachedFirstSignatureMethods = new Dictionary<string, List<Func<bool, int>>>();

		if (CachedSecondSignatureMethods == null)
			CachedSecondSignatureMethods = new Dictionary<string, List<Func<bool, string, int>>>();
		
		CachedInstancesOfBaseType.Clear();
		CachedFirstSignatureMethods.Clear();
		CachedSecondSignatureMethods.Clear();
	}

	public void OnSetupForCacheGeneration()
	{
		// Do nothing here... but we could load scriptable objects or some other data that we need for parsing the reflected data we found  
	}

	public void OnReflectionCacheBuilt(PerBaseTypeCache perBaseTypeCache, PerAttributeCache perAttributeCache)
	{
		// We don't do anything in this callback --- this one is called once with ALL reflected data we gathered. 
	}

	public void OnBaseTypeOfInterestFound(BaseTypeOfInterest baseType, IReadOnlyList<MemberInfo> cachedSubTypes)
	{
#if UNITY_EDITOR 
		// We care about this type, since we want to ensure it always has at least one member with a MyReflectionAttribute --- so we add some quick editor-only validations.
		if (baseType.Equals(MY_INTERESTING_BASE_TYPE))
		{

			var typesWithoutAtLeastOneAttr = new List<Type>();
			foreach (var type in cachedSubTypes.Cast<Type>())
			{
				var validationResults = type.GetMembers().GetOptionalAttributeInMembers<MyReflectionAttribute>();
				// Adds a hint informing that the given type does not have at least one of MyReflectionAttribute
				if (validationResults.Count == 0 || validationResults.TrueForAll(v => v.Type == ReflectionCache.ValidationResultType.Discarded))
					typesWithoutAtLeastOneAttr.Add(type);
			}
			
			_hintGlobalStorage.AddOrReplaceHint(BeamHintType.Validation,
			                                    UserBeamHintDomains.MyUserSystemDomain_SubDomain1,
			                                    UserBeamHintIds.MyInterestingBaseTypeMissingAttributesOnMembers,
			                                    typesWithoutAtLeastOneAttr);
		}
#endif
	}

	public void OnAttributeOfInterestFound(AttributeOfInterest attributeType, IReadOnlyList<MemberAttribute> cachedMemberAttributes)
	{
		// We care about this attribute since we want to cache the functions it's annotated.
		if (attributeType.Equals(MY_REFLECTION_ATTRIBUTE))
		{
			var validationResults = cachedMemberAttributes.Validate();
			validationResults.SplitValidationResults(out var valid, out var warning, out var error);

#if UNITY_EDITOR
			// Handle hint code --- remember to wrap this in UNITY_EDITOR since this system is meant to run outside of an editor environment, where hints don't exist. 
			if (error.Count > 0)
			{
				_hintGlobalStorage.AddOrReplaceHint(BeamHintType.Validation, UserBeamHintDomains.MyUserSystemDomain_SubDomain1, UserBeamHintIds.MyReflectionAttributeInvalidUsage, error);
			}

			if (warning.Count > 0)
			{
				_hintGlobalStorage.AddOrReplaceHint(BeamHintType.Hint, UserBeamHintDomains.MyUserSystemDomain_SubDomain1, UserBeamHintIds.MyReflectionAttributeInterestingCase, warning);
			}
#endif

			// For each of MyInterestingBaseType
			var groupedByDeclaringMembers = valid.Select(v => v.Pair).CreateMemberAttributeOwnerLookupTable();
			foreach (var declaringTypeFunctions in groupedByDeclaringMembers)
			{
				var type = (Type)declaringTypeFunctions.Key;
				var functions = declaringTypeFunctions.Value;
				
				// Create the cached instance of the declaring type
				var constructor = type.GetConstructor(new Type[] { });
				var instance = (MyInterestingBaseType)constructor.Invoke(null);
				
				CachedInstancesOfBaseType.Add(type.Name, instance);
				CachedFirstSignatureMethods.Add(type.Name, new List<Func<bool, int>>());
				CachedSecondSignatureMethods.Add(type.Name, new List<Func<bool, string, int>>());
				
				// Create Delegates for found methods.
				foreach (var memberAttribute in functions)
				{
					var methodInfo = memberAttribute.InfoAs<MethodInfo>();

					// If matches the first signature, we add it to the first signature cached delegates
					if (MyReflectionAttribute.ValidSignatures.MatchSignatureAtIdx(0, methodInfo))
					{
						var cachedMethod = (Func<bool, int>) Delegate.CreateDelegate(typeof(Func<bool, int>), instance, methodInfo.Name);
						CachedFirstSignatureMethods[type.Name].Add(cachedMethod);
					}

					// If matches the second signature, we add it to the second signature cached delegates
					if (MyReflectionAttribute.ValidSignatures.MatchSignatureAtIdx(1, methodInfo))
					{
						var cachedMethod = (Func<bool, string, int>)Delegate.CreateDelegate(typeof(Func<bool, string, int>), instance, methodInfo.Name);
						CachedSecondSignatureMethods[type.Name].Add(cachedMethod);
					}
				}
			}

		}
	}

	public void SetStorage(IBeamHintGlobalStorage hintGlobalStorage)
	{
#if UNITY_EDITOR
		_hintGlobalStorage = hintGlobalStorage;
#endif
	}

	/// <summary>
	/// Calls all functions implementing <see cref="MyReflectionAttribute"/> and returns all their return values.
	/// </summary>
	public List<int> InvokeAllCachedFirstFunctions(string fromType, bool param)
	{
		var cachedMethods = CachedFirstSignatureMethods[fromType];
		var returnedValues = new List<int>(cachedMethods.Count);
		foreach (var cachedMethod in cachedMethods) returnedValues.Add(cachedMethod.Invoke(param));

		return returnedValues;
	}
	
	/// <summary>
	/// Calls all functions implementing <see cref="MyReflectionAttribute"/> and returns all their return values.
	/// </summary>
	public List<int> InvokeAllCachedSecondFunctions(string fromType, bool param, string param2)
	{
		var cachedMethods = CachedSecondSignatureMethods[fromType];
		var returnedValues = new List<int>(cachedMethods.Count);
		foreach (var cachedMethod in cachedMethods) returnedValues.Add(cachedMethod.Invoke(param, param2));

		return returnedValues;
	}
}



public abstract class MyInterestingBaseType
{
	public int FieldOne;
	public int FieldTwo;
	public int FieldThree;
}

public class MySubtype : MyInterestingBaseType
{
	public int MyData;

	[MyReflection]
	public int FirstSignatureMethod(bool param)
	{
		Debug.Log($"Doing some game-specific stuff here with {param}");
		return param ? 1 : 0;
	}
	
	[MyReflection]
	public int SecondSignatureMethod(bool param, string text)
	{
		Debug.Log($"Doing some game-specific stuff here with {param} and {text}");
		return param ? text.Length : 0;
	}

}

public class MyInvalidSubtype : MyInterestingBaseType
{
	public int MyData;
	
	[MyReflection]
	public int SecondSignatureMethod(bool param, string text, GameObject invalidParam)
	{
		Debug.Log($"This will fail the ReflectionSystem's validations since it won't match any signatures");
		return param ? text.Length : 0;
	}
}

public class MyIncompleteSubtype : MyInterestingBaseType
{
	public int MyData;

	public void SomeOtherUnrelatedFunction()
	{
		Debug.Log($"This type will also fail the ReflectionSystem's validations since it has no method with [{nameof(MyReflectionAttribute)}].");
	}
}


[AttributeUsage(AttributeTargets.Method)]
public class MyReflectionAttribute : Attribute, IReflectionAttribute
{
	public static readonly List<SignatureOfInterest> ValidSignatures = new List<SignatureOfInterest>()
	{
		new SignatureOfInterest(false, typeof(int), new[] {new ParameterOfInterest(typeof(bool), false, false, false)}),
		new SignatureOfInterest(false, typeof(int), new[] {new ParameterOfInterest(typeof(bool), false, false, false), new ParameterOfInterest(typeof(string), false, false, false)}),
	};

	public static readonly string ValidSignaturesText = string.Join(" || ", ValidSignatures.Select(sig => sig.ToHumanReadableSignature()));

	public AttributeValidationResult IsAllowedOnMember(MemberInfo member)
	{
		var methodInfo = (MethodInfo)member;

		var matchedSignatureIndices = ValidSignatures.FindMatchingMethodSignatures(methodInfo);

		// If no matched signatures were found
		if (matchedSignatureIndices.TrueForAll(idx => idx == -1))
		{
			return new AttributeValidationResult(this, methodInfo, ReflectionCache.ValidationResultType.Error,
			                                     $"Method with signature [{methodInfo.ToHumanReadableSignature()}] doesn't match a valid signature [{ValidSignaturesText}]");
		}

		// If matched second signature, leave a warning that the user should know.
		if (matchedSignatureIndices.Contains(1))
		{
			return new AttributeValidationResult(this, methodInfo, ReflectionCache.ValidationResultType.Warning,
			                                     $"This version of the method is interesting and you should know about this assumption here!");
		}

		if (methodInfo.DeclaringType == null || !methodInfo.DeclaringType.IsSubclassOf(typeof(MyInterestingBaseType)))
		{
			return new AttributeValidationResult(this, methodInfo, ReflectionCache.ValidationResultType.Error,
			                                     $"This attribute only works inside {nameof(MyInterestingBaseType)} subclasses!");
		}
		
		return new AttributeValidationResult(this, methodInfo, ReflectionCache.ValidationResultType.Valid, "");
	}
}
