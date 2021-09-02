using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Beamable.Coroutines
{
   public static class Yielders
   {
      static Dictionary<float, WaitForSeconds> _timeInterval = new Dictionary<float, WaitForSeconds>(20);

      static WaitForEndOfFrame _endOfFrame = new WaitForEndOfFrame();
      public static WaitForEndOfFrame EndOfFrame {
         get{ return _endOfFrame;}
      }

      static WaitForFixedUpdate _fixedUpdate = new WaitForFixedUpdate();
      public static WaitForFixedUpdate FixedUpdate{
         get{ return _fixedUpdate; }
      }

      public static WaitForSeconds Seconds(float seconds){
         if(!_timeInterval.ContainsKey(seconds))
            _timeInterval.Add(seconds, new WaitForSeconds(seconds));
         return _timeInterval[seconds];
      }
   }
}
