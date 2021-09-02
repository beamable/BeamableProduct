using UnityEngine;
using UnityEngine.EventSystems;

namespace Beamable
{
   public class BeamableModule : MonoBehaviour
   {
      void Start()
      {
         if (EventSystem.current == null)
         {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
         }
      }
   }
}