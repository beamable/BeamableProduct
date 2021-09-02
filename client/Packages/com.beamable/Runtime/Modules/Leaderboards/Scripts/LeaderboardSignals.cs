using System;

using System.Collections;

using System.Collections.Generic;

using System.Linq;

using System.Linq.Expressions;

using System.Security.Authentication;
using Beamable.Signals;
using Beamable.Coroutines;

using Beamable.Platform.SDK;

using Beamable.Platform.SDK.Auth;
using Beamable.UI;

using Beamable.UI.Scripts;

using TMPro;

using UnityEngine;

using UnityEngine.Events;



namespace Beamable.Leaderboards

{

   [System.Serializable]

   public class ToggleEvent : DeSignal<bool>

   {



   }

   public class LeaderboardSignals : DeSignalTower

   {

      [Header("Flow Events")]

      public ToggleEvent OnToggleLeaderboard;


      private static bool _toggleState;


      public static bool ToggleState => _toggleState;


      private void Broadcast<TArg>(TArg arg, Func<LeaderboardSignals, DeSignal<TArg>> getter)

      {

         this.BroadcastSignal(arg, getter);

      }


      public void ToggleLeaderboard()

      {

         _toggleState = !_toggleState;

         Broadcast(_toggleState, s => s.OnToggleLeaderboard);

      }

      public void ToggleLeaderboard(bool desiredState)

      {

         if (desiredState == ToggleState) return;



         _toggleState = desiredState;

         Broadcast(_toggleState, s => s.OnToggleLeaderboard);

      }
   }
}

