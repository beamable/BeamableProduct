// this file was copied from nuget package Beamable.Common@6.1.0-PREVIEW.RC1
// https://www.nuget.org/packages/Beamable.Common/6.1.0-PREVIEW.RC1

using System;

namespace Beamable.Content
{
	/// <summary>
	/// This type defines the field attribute that marks a %Beamable %ContentObject field
	/// as ignored from the %Content %Serialization process.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/profile-storage/content/content-overview">Content - IgnoreContentField</a> documentation
	/// 
	/// ![img beamable-logo]
	/// 
	/// </summary>
	[AttributeUsage(validOn: AttributeTargets.Field)]
	public class IgnoreContentFieldAttribute : Attribute
	{

	}
}
