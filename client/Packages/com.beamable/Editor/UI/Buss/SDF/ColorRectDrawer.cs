using System;
using Beamable.UI.SDF.Styles;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.SDF {
    [CustomPropertyDrawer(typeof(ColorRect))]
    public class ColorRectDrawer : PropertyDrawer {
        private Mode _mode = Mode.PerVertexColor;
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight * (_mode == Mode.SingleColor ? 2 : 3);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var rc = new EditorGUIRectController(position);

            var bottomLeft = property.FindPropertyRelative("BottomLeftColor");
            var bottomRight = property.FindPropertyRelative("BottomRightColor");
            var topLeft = property.FindPropertyRelative("TopLeftColor");
            var topRight = property.FindPropertyRelative("TopRightColor");

            EditorGUI.BeginChangeCheck();
            
            _mode = (Mode) EditorGUI.EnumPopup(rc.ReserveSingleLine(), label, _mode);
            
            rc.MoveIndent(1);
            
            switch (_mode) {
                case Mode.SingleColor:
                    bottomLeft.colorValue = bottomRight.colorValue = topLeft.colorValue = topRight.colorValue =
                        EditorGUI.ColorField(rc.ReserveSingleLine(), "Color", topLeft.colorValue);
                    break;
                case Mode.HorizontalGradient:
                    bottomLeft.colorValue =  topLeft.colorValue =
                        EditorGUI.ColorField(rc.ReserveSingleLine(), "Left", topLeft.colorValue);
                    bottomRight.colorValue = topRight.colorValue =
                        EditorGUI.ColorField(rc.ReserveSingleLine(), "Right", topRight.colorValue);
                    break;
                case Mode.VerticalGradient:
                    topLeft.colorValue = topRight.colorValue =
                        EditorGUI.ColorField(rc.ReserveSingleLine(), "Top", topLeft.colorValue);
                    bottomLeft.colorValue = bottomRight.colorValue =
                        EditorGUI.ColorField(rc.ReserveSingleLine(), "Bottom", bottomLeft.colorValue);
                    break;
                case Mode.DiagonalGradient:
                    topLeft.colorValue =
                        EditorGUI.ColorField(rc.ReserveSingleLine(), "Start", topLeft.colorValue);
                    bottomRight.colorValue =
                        EditorGUI.ColorField(rc.ReserveSingleLine(), "End", bottomRight.colorValue);
                    topRight.colorValue = bottomLeft.colorValue =
                        Color.Lerp(topLeft.colorValue, bottomRight.colorValue, .5f);
                    break;
                case Mode.FlippedDiagonalGradient:
                    bottomLeft.colorValue =
                        EditorGUI.ColorField(rc.ReserveSingleLine(), "Start", bottomLeft.colorValue);
                    topRight.colorValue =
                        EditorGUI.ColorField(rc.ReserveSingleLine(), "End", topRight.colorValue);
                    bottomRight.colorValue = topLeft.colorValue =
                        Color.Lerp(bottomLeft.colorValue, topRight.colorValue, .5f);
                    break;
                case Mode.PerVertexColor:
                    var topRow = rc.ReserveSingleLine().ToRectController();
                    topLeft.colorValue =
                        EditorGUI.ColorField(topRow.ReserveWidthByFraction(.5f), topLeft.colorValue);
                    topRight.colorValue =
                        EditorGUI.ColorField(topRow.rect, topRight.colorValue);
                    bottomLeft.colorValue =
                        EditorGUI.ColorField(rc.ReserveWidthByFraction(.5f), bottomLeft.colorValue);
                    bottomRight.colorValue =
                        EditorGUI.ColorField(rc.rect, bottomRight.colorValue);
                    break;
            }

            if (EditorGUI.EndChangeCheck()) {
                property.serializedObject.ApplyModifiedProperties();
            }
        }
        
        private enum Mode {
            SingleColor,
            HorizontalGradient,
            VerticalGradient,
            DiagonalGradient,
            FlippedDiagonalGradient,
            PerVertexColor
        }
    }
}