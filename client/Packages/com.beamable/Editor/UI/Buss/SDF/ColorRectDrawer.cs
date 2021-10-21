using System;
using Beamable.UI.SDF.Styles;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.SDF {
    [CustomPropertyDrawer(typeof(ColorRect))]
    public class ColorRectDrawer : PropertyDrawer {
        private Mode _mode = Mode.PerVertexColor;
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return GetHeight();
        }

        public float GetHeight() {
            return EditorGUIUtility.singleLineHeight * (_mode == Mode.SingleColor ? 2 : 3);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var rc = new EditorGUIRectController(position);

            var bottomLeft = property.FindPropertyRelative("BottomLeftColor");
            var bottomRight = property.FindPropertyRelative("BottomRightColor");
            var topLeft = property.FindPropertyRelative("TopLeftColor");
            var topRight = property.FindPropertyRelative("TopRightColor");

            var colorRect = new ColorRect(
                bottomLeft.colorValue, bottomRight.colorValue, 
                topLeft.colorValue, topRight.colorValue);

            EditorGUI.BeginChangeCheck();
            
            colorRect = DrawColorRect(label, rc, colorRect);

            if (EditorGUI.EndChangeCheck()) {
                bottomLeft.colorValue = colorRect.BottomLeftColor;
                bottomRight.colorValue = colorRect.BottomRightColor;
                topLeft.colorValue = colorRect.TopLeftColor;
                topRight.colorValue = colorRect.TopRightColor;
                
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        public ColorRect DrawColorRect(GUIContent label, EditorGUIRectController rc, ColorRect colorRect) {
            _mode = (Mode) EditorGUI.EnumPopup(rc.ReserveSingleLine(), label, _mode);

            rc.MoveIndent(1);

            colorRect = DrawColorFields(colorRect, rc);

            rc.MoveIndent(-1);
            return colorRect;
        }

        private ColorRect DrawColorFields(ColorRect colorRect, EditorGUIRectController rc) {
            switch (_mode) {
                case Mode.SingleColor:
                    colorRect.BottomLeftColor = colorRect.BottomRightColor = colorRect.TopLeftColor = colorRect.TopRightColor =
                        EditorGUI.ColorField(rc.ReserveSingleLine(), "Color", colorRect.TopLeftColor);
                    break;
                case Mode.HorizontalGradient:
                    colorRect.BottomLeftColor = colorRect.TopLeftColor =
                        EditorGUI.ColorField(rc.ReserveSingleLine(), "Left", colorRect.TopLeftColor);
                    colorRect.BottomRightColor = colorRect.TopRightColor =
                        EditorGUI.ColorField(rc.ReserveSingleLine(), "Right", colorRect.TopRightColor);
                    break;
                case Mode.VerticalGradient:
                    colorRect.TopLeftColor = colorRect.TopRightColor =
                        EditorGUI.ColorField(rc.ReserveSingleLine(), "Top", colorRect.TopLeftColor);
                    colorRect.BottomLeftColor = colorRect.BottomRightColor =
                        EditorGUI.ColorField(rc.ReserveSingleLine(), "Bottom", colorRect.BottomLeftColor);
                    break;
                case Mode.DiagonalGradient:
                    colorRect.TopLeftColor =
                        EditorGUI.ColorField(rc.ReserveSingleLine(), "Start", colorRect.TopLeftColor);
                    colorRect.BottomRightColor =
                        EditorGUI.ColorField(rc.ReserveSingleLine(), "End", colorRect.BottomRightColor);
                    colorRect.TopRightColor = colorRect.BottomLeftColor =
                        Color.Lerp(colorRect.TopLeftColor, colorRect.BottomRightColor, .5f);
                    break;
                case Mode.FlippedDiagonalGradient:
                    colorRect.BottomLeftColor =
                        EditorGUI.ColorField(rc.ReserveSingleLine(), "Start", colorRect.BottomLeftColor);
                    colorRect.TopRightColor =
                        EditorGUI.ColorField(rc.ReserveSingleLine(), "End", colorRect.TopRightColor);
                    colorRect.BottomRightColor = colorRect.TopLeftColor =
                        Color.Lerp(colorRect.BottomLeftColor, colorRect.TopRightColor, .5f);
                    break;
                case Mode.PerVertexColor:
                    var topRow = rc.ReserveSingleLine().ToRectController();
                    var bottomRow = rc.ReserveSingleLine().ToRectController();
                    colorRect.TopLeftColor =
                        EditorGUI.ColorField(topRow.ReserveWidthByFraction(.5f), colorRect.TopLeftColor);
                    colorRect.TopRightColor =
                        EditorGUI.ColorField(topRow.rect, colorRect.TopRightColor);
                    colorRect.BottomLeftColor =
                        EditorGUI.ColorField(bottomRow.ReserveWidthByFraction(.5f), colorRect.BottomLeftColor);
                    colorRect.BottomRightColor =
                        EditorGUI.ColorField(bottomRow.rect, colorRect.BottomRightColor);
                    break;
            }

            return colorRect;
        }

        public enum Mode {
            SingleColor,
            HorizontalGradient,
            VerticalGradient,
            DiagonalGradient,
            FlippedDiagonalGradient,
            PerVertexColor
        }
    }
}