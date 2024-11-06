using Beamable.Editor.Environment;
using Beamable.Editor.Microservice.UI2;
using UnityEditor;

namespace Beamable.Server.Editor
{
	[InitializeOnLoad]
	public class PackageAvailability
	{
		static PackageAvailability()
		{
			BeamablePackages.ProvideServerWindow(UsamWindow.Init);
		}
	}
}
