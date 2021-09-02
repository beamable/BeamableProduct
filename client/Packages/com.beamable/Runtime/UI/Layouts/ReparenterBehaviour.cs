using System;
using System.Collections;
using System.Collections.Generic;
using Beamable.Coroutines;
using Beamable.Signals;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Beamable.UI.Layouts
{

   public class ReparenterBehaviour : MediaQueryBehaviour
   {
      public Transform Origin, Destination;

      public bool Output { get; private set; }
      private Transform _parent;

      public Transform CurrentParent => _parent ?? Origin;

      public void MoveToOrigin()
      {
         MoveChildren(Destination, Origin);
      }

      public void MoveToDestination()
      {
         MoveChildren(Origin, Destination);
      }

      public void Move(bool toDestination)
      {
         if (toDestination)
         {
            _parent = Destination;
            MoveToDestination();
         }
         else
         {
            _parent = Origin;
            MoveToOrigin();
         }
      }

      protected override void OnMediaQueryChange(MediaSourceBehaviour query, bool output)
      {
         Output = output;
         Move(output);
         base.OnMediaQueryChange(query, output);
      }

      void MoveChildren(Transform from, Transform to)
      {
         var children = new List<Transform>();
         for (var i = 0; i < from.childCount ; i++)
         {
            children.Add(from.GetChild(i));
         }

         foreach (var child in children)
         {
            child.SetParent(to, false);
         }

         from.gameObject.SetActive(false);
         to.gameObject.SetActive(true);

         if (to is RectTransform rect)
         {
            LayoutRebuilder.MarkLayoutForRebuild(rect);
         }
      }

   }
}