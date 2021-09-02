using Beamable.Editor.Environment;
using Beamable.Editor.Microservice.UI;
using UnityEditor;

namespace Beamable.Server.Editor
{
   [InitializeOnLoad]
   public class PackageAvailability
   {
      static PackageAvailability()
      {
         
         #if BEAMABLE_NEWMS
         BeamablePackages.ProvideServerWindow(MicroserviceWindow.Init);
#else
         BeamablePackages.ProvideServerWindow(DebugWindow.Init);
         #endif
      }
   }
}