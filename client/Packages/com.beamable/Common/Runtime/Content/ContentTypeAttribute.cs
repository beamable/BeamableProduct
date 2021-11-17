using System;
using System.Runtime.CompilerServices;

namespace Beamable.Common.Content
{
	/// <summary>
	/// This type defines part of the %Beamable %ContentObject system.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Common.Content.ContentObject script reference
	/// 
	/// ![img beamable-logo]
	///
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class ContentTypeAttribute : UnityEngine.Scripting.PreserveAttribute, IHasSourcePath
	{
		public string TypeName
		{
			get;
		}

		public string SourcePath
		{
			get;
		}

		public ContentTypeAttribute(string typeName, [CallerFilePath] string sourcePath = "")
		{
			TypeName = typeName;
			SourcePath = sourcePath;
		}
	}

	/// <summary>
	/// This type defines part of the %Beamable %ContentObject system.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Common.Content.ContentObject script reference
	/// 
	/// ![img beamable-logo]
	///
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class ContentFormerlySerializedAsAttribute : Attribute
	{
		public string OldTypeName
		{
			get;
		}

		public ContentFormerlySerializedAsAttribute(string oldTypeName)
		{
			OldTypeName = oldTypeName;
		}
	}
}
