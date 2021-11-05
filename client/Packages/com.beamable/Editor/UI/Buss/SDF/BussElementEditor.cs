// using System;
// using Beamable.UI.BUSS;
// using Beamable.UI.SDF;
// using UnityEditor;
//
// namespace Beamable.Editor.UI.SDF {
//     
//     [CustomEditor(typeof(BUSSElement), true)]
//     public class BussElementEditor : UnityEditor.Editor {
//         private bool _selectorDirty;
//         private bool _inlineStyleDirty;
//         private bool _styleSheetDirty;
//         
//         private void OnEnable() {
//             foreach (var target in targets) {
//                 var bussElement = (BUSSElement) target;
//                 
//             }
//         }
//
//         public override void OnInspectorGUI() {
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("_id"));
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("_inlineStyle"));
//             EditorGUI.BeginChangeCheck();
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("_styleSheet"));
//             _styleSheetDirty = EditorGUI.EndChangeCheck();
//
//             if (_selectorDirty || _inlineStyleDirty || _styleSheetDirty) {
//                 EditorApplication.delayCall += Refresh;
//             }
//         }
//
//         private void Refresh() {
//             if (_styleSheetDirty) {
//                 
//             }
//         }
//     }
// }