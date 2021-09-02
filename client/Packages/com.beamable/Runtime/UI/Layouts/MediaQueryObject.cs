using System;

using System.Collections.Generic;
using Beamable.UI.Scripts;
using UnityEngine;

using UnityEngine.Events;



namespace Beamable.UI.Layouts

{

    public enum MediaQueryOperation

    {

        GREATER_THAN,

        LESS_THAN

    }



    public enum MediaQueryDimension

    {

        WIDTH,

        HEIGHT,

        ASPECT,

        KEYBOARD_HEIGHT

    }

    public delegate void MediaQueryCallback(MediaSourceBehaviour query, bool output);

   [CreateAssetMenu(
      fileName = "Media Query",
      menuName = BeamableConstants.MENU_ITEM_PATH_ASSETS_BEAMABLE + "/" +
      "Media Query",
      order = BeamableConstants.MENU_ITEM_PATH_ASSETS_BEAMABLE_ORDER_1)]
    public class MediaQueryObject : ScriptableObject

    {

        public MediaQueryDimension Dimension;

        public MediaQueryOperation Operation;

        public float Threshold;



        float GetDimensionValue()

        {

            var screen = GetScreen();

            switch (Dimension)

            {

                case MediaQueryDimension.WIDTH:

                    return screen.x;

                case MediaQueryDimension.HEIGHT:

                    return screen.y;

                case MediaQueryDimension.ASPECT:

                    return screen.x / (float)screen.y;

                case MediaQueryDimension.KEYBOARD_HEIGHT:

                    return MobileUtilities.GetKeyboardHeight(false);

                default:

                    throw new Exception("Unknown dimension");

            }

        }



        float GetDimensionValue(RectTransform transform)

        {

            switch (Dimension)

            {

                case MediaQueryDimension.WIDTH:

                    return transform.rect.width;

                case MediaQueryDimension.HEIGHT:

                    return transform.rect.height;

                case MediaQueryDimension.ASPECT:

                    var rect = transform.rect;

                    return rect.width / rect.height;

                default:

                    throw new Exception("Dimension value not supported on transform");

            }

        }



        Vector2 GetScreen()

        {

            // IF WE ARE IN EDITOR, THEN WE WANT TO GET THE GAME-SCREEN SIZE, NOT THE EDITOR SCREEN SIZE...

            #if UNITY_EDITOR

            if (!Application.isPlaying)

            {

                return GetMainGameViewSize();

            }

            #endif

            return new Vector2(Screen.width, Screen.height);

        }



        bool CompareDimensionAndThreshold(float dimensionValue, float thresholdValue)

        {

            switch (Operation)

            {

                case MediaQueryOperation.LESS_THAN:

                    return dimensionValue < thresholdValue;

                case MediaQueryOperation.GREATER_THAN:

                    return dimensionValue > thresholdValue;

                default:

                    throw new Exception("unknown operation");

            }

        }



        public bool CalculateScreen()

        {

            var dimensionValue = GetDimensionValue();

            return CompareDimensionAndThreshold(dimensionValue, Threshold);

        }



        public bool Calculate(RectTransform target)

        {

            var dimensionValue = GetDimensionValue(target);

            return CompareDimensionAndThreshold(dimensionValue, Threshold);

        }



        static Vector2 GetMainGameViewSize()

        {

            System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");

            System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView",System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            System.Object Res = GetSizeOfMainGameView.Invoke(null,null);

            return (Vector2)Res;

        }



    }

}