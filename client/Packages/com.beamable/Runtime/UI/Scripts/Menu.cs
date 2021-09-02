using System;
using Beamable.Theme.Palettes;
using UnityEngine;
using UnityEngine.Events;

namespace Beamable.UI.Scripts
{
   [System.Serializable]
   public abstract class MenuBase : MonoBehaviour
   {
      public StringBinding Title;
      public bool Float = false;
      public bool DestoryOnLeave;
      public UnityEvent OnOpen;
      public UnityEvent OnClosed;

      private MenuManagementBehaviour _manager;

      public MenuManagementBehaviour Manager
      {
         get => _manager;
         set => _manager = value;
      }

      public virtual void OnOpened()
      {
         // maybe do something?
      }

      public virtual string GetTitleText()
      {
         return Title?.Localize() ?? "Menu";
      }

      public virtual void OnWentBack()
      {
         // maybe do something?
      }

      public void Hide()
      {
         Manager.Close(this);
      }
   }

}