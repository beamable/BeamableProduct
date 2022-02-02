using Beamable.Editor.Environment;
using Beamable.Editor.Microservice.UI;
using UnityEditor;
using UnityEngine;

namespace Beamable.Server.Editor
{
	[InitializeOnLoad]
	public class PackageAvailability
	{
		static PackageAvailability()
		{

#if !BEAMABLE_LEGACY_MSW
			BeamablePackages.ProvideServerWindow(() =>
			{

				// Trigger reimport of reflection cache to solve first import sadness...
				AssetDatabase.StartAssetEditing();
				AssetDatabase.ImportAsset("Packages/com.beamable.server/Editor/ReflectionCache/UserSystems/MicroserviceReflectionCache.asset", ImportAssetOptions.ForceUpdate);
				Debug.Log("Re-importing Microservice Reflection Cache so the reflection cache gets it.");
				AssetDatabase.StopAssetEditing();

				MicroserviceWindow.Init();
			});
#else
         BeamablePackages.ProvideServerWindow(DebugWindow.Init);
#endif
		}
	}
}
