using Beamable.Common;
using Beamable.Common.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Beamable.Server
{
	/// <summary>
	/// This type defines the %Microservice method attribute for any
	/// %Microservice method which can be called EITHER from the %Client or
	/// a %Microservice by a User account of any type.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See Beamable.Server.Microservice script reference
	/// - See Beamable.Server.AdminOnlyCallableAttribute script reference
	///
	/// ### Example
	/// This demonstrates example usage from WITHIN a custom %Beamable %Microservice.
	///
	/// ```
	/// [ClientCallable]
	/// private async void MyMicroserviceMethod()
	/// {
	///
	///   // Do something...
	/// 
	/// }
	///
	/// ```
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class ClientCallableAttribute : Attribute, INamingAttribute
	{
		public static readonly List<ParameterOfInterest> UNSUPPORTED_PARAMETER_TYPES = new List<ParameterOfInterest>() {
		   new ParameterOfInterest(typeof(Delegate), false, false, false),
		   new ParameterOfInterest(typeof(Task), false, false, false),
		   new ParameterOfInterest(typeof(Promise), false, false, false),
	   };

		private string pathName = "";
		public HashSet<string> RequiredScopes { get; }

		public ClientCallableAttribute() : this("", null)
		{

		}

		public ClientCallableAttribute(string pathnameOverride = "", string[] requiredScopes = null)
		{
			pathName = pathnameOverride;
			RequiredScopes = requiredScopes == null
			   ? new HashSet<string>()
			   : new HashSet<string>(requiredScopes);
		}

		public string PathName
		{
			set { pathName = value; }
			get { return pathName; }
		}

		public string[] Names => new[] { pathName };

		public AttributeValidationResult IsAllowedOnMember(MemberInfo member)
		{
			var methodInfo = (MethodInfo)member;

			// Check for any unsupported parameter types.
			if (UNSUPPORTED_PARAMETER_TYPES.MatchAnyParametersOfMethod(methodInfo, out var detectedUnsupportedTypes))
			{
				var message = $"The unsupported parameters are: {string.Join(", ", detectedUnsupportedTypes.Select(p => $"{p.ParameterType.Name} {p.Name}"))}";
				return new AttributeValidationResult(this, member, ReflectionCache.ValidationResultType.Error, message);
			}

			// Check for void signatures to send out warning.
			if (methodInfo.IsAsyncMethodOfType(typeof(void)))
			{
				var message = $"";
				return new AttributeValidationResult(this, member, ReflectionCache.ValidationResultType.Warning, message);
			}

			return new AttributeValidationResult(this, member, ReflectionCache.ValidationResultType.Valid, $"");
		}

		public AttributeValidationResult AreValidNameForType(MemberInfo member, string[] potentialNames)
		{
			return new AttributeValidationResult(this, member, ReflectionCache.ValidationResultType.Valid, $"");
		}
	}

	/// <summary>
	/// This type defines the %Microservice method attribute for any
	/// %Microservice method which can be called ONLY from a
	/// %Microservice by a User account of %Admin type.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See Beamable.Server.Microservice script reference
	/// - See Beamable.Server.ClientCallableAttribute script reference
	///
	/// ### Example
	/// This demonstrates example usage from WITHIN a custom %Beamable %Microservice.
	///
	/// ```
	/// [AdminOnlyCallable]
	/// private async void MyMicroserviceMethod()
	/// {
	///
	///   // Do something...
	/// 
	/// }
	///
	/// ```
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class AdminOnlyCallableAttribute : ClientCallableAttribute
	{
		public AdminOnlyCallableAttribute(string pathnameOverride = "") : base(pathnameOverride,
		   requiredScopes: new[] { "*" })
		{

		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	public class CustomResponseSerializationAttribute : Attribute
	{
		public virtual string SerializeResponse(object raw)
		{
			return raw.ToString();
		}
	}
}
