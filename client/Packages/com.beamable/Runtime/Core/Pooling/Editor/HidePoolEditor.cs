using Beamable.Service;
using UnityEditor;
using UnityEngine;

namespace Beamable.Pooling.Editor
{
   [CustomEditor(typeof(HidePool))]
   public class HidePoolEditor : UnityEditor.Editor
   {
      public string savePath;
      public override void OnInspectorGUI()
      {
         DrawDefaultInspector();

         HidePool hp = this.target as HidePool;

         var d = hp.GetPools();
         EditorGUILayout.BeginHorizontal();
         EditorGUILayout.LabelField("prefab", GUILayout.Width(240f));
         EditorGUILayout.LabelField("free", GUILayout.Width(80f));
         EditorGUILayout.LabelField("max", GUILayout.Width(80f));
         EditorGUILayout.LabelField("reused", GUILayout.Width(80f));

         EditorGUILayout.EndHorizontal();
         foreach (GameObject prefab in d.Keys)
         {
            HidePool.Pool p = d[prefab];
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(prefab.name, GUILayout.Width(240f));
            EditorGUILayout.LabelField(p.currentlyFree.ToString(), GUILayout.Width(80f));
            EditorGUILayout.LabelField(p.highWaterMark.ToString(), GUILayout.Width(80f));
            EditorGUILayout.LabelField(p.reused.ToString(), GUILayout.Width(80f));

            EditorGUILayout.EndHorizontal();
         }
      }
   }
}
