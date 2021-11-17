using System;
using System.Collections.Generic;

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
	[System.AttributeUsage(System.AttributeTargets.Method)]
	public class ClientCallableAttribute : System.Attribute
	{
		private string pathName = "";

		public HashSet<string> RequiredScopes
		{
			get;
		}

		public ClientCallableAttribute() : this("", null) { }

		public ClientCallableAttribute(string pathnameOverride = "", string[] requiredScopes = null)
		{
			pathName = pathnameOverride;
			RequiredScopes = requiredScopes == null
				? new HashSet<string>()
				: new HashSet<string>(requiredScopes);
		}

		public string PathName
		{
			set
			{
				pathName = value;
			}
			get
			{
				return pathName;
			}
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
	[System.AttributeUsage(System.AttributeTargets.Method)]
	public class AdminOnlyCallableAttribute : ClientCallableAttribute
	{
		public AdminOnlyCallableAttribute(string pathnameOverride = "") : base(pathnameOverride,
																			   requiredScopes: new[] { "*" })
		{
		}
	}

	[System.AttributeUsage(System.AttributeTargets.Method)]
	public class CustomResponseSerializationAttribute : Attribute
	{
		public virtual string SerializeResponse(object raw)
		{
			return raw.ToString();
		}
	}
}
