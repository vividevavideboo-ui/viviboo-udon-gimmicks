#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UdonSharp;
using UdonSharpEditor;
using VRC.SDK3.Components;

namespace Vivi
{
    [CustomEditor(typeof(WashbasinDrop))]
    public class WashbasinDropEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target);
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("たらいを指定してから押すと、衝突検知を追加して必要な参照を配線します。AudioSource の設定は手動で行ってください。", MessageType.Info);
            if (GUILayout.Button("衝突検知を追加して配線"))
            {
                ConfigureImpact((WashbasinDrop)target);
            }
        }

        private void ConfigureImpact(WashbasinDrop controller)
        {
            SerializedObject controllerSerializedObject = new SerializedObject(controller);
            GameObject washbasin = controllerSerializedObject.FindProperty("washbasin").objectReferenceValue as GameObject;
            if (washbasin == null)
            {
                EditorUtility.DisplayDialog("Washbasin Drop", "先に「たらい」を指定してください。", "OK");
                return;
            }

            Rigidbody washbasinRigidbody = washbasin.GetComponent<Rigidbody>();
            VRCObjectSync washbasinObjectSync = washbasin.GetComponent<VRCObjectSync>();
            if (washbasinRigidbody == null || washbasinObjectSync == null)
            {
                EditorUtility.DisplayDialog("Washbasin Drop", "たらいに Rigidbody と VRC Object Sync を追加してから実行してください。", "OK");
                return;
            }

            WashbasinDropImpact impact = washbasin.GetComponent<WashbasinDropImpact>();
            if (impact == null)
            {
                impact = UdonSharpUndo.AddComponent<WashbasinDropImpact>(washbasin);
            }

            SerializedObject impactSerializedObject = new SerializedObject(impact);
            impactSerializedObject.FindProperty("controller").objectReferenceValue = controller;
            impactSerializedObject.ApplyModifiedProperties();
            UdonSharpEditorUtility.CopyProxyToUdon(impact);

            controllerSerializedObject.Update();
            controllerSerializedObject.FindProperty("washbasinRigidbody").objectReferenceValue = washbasinRigidbody;
            controllerSerializedObject.FindProperty("washbasinObjectSync").objectReferenceValue = washbasinObjectSync;
            controllerSerializedObject.FindProperty("washbasinImpact").objectReferenceValue = impact;
            controllerSerializedObject.ApplyModifiedProperties();
            UdonSharpEditorUtility.CopyProxyToUdon(controller);

            EditorUtility.SetDirty(washbasin);
            EditorUtility.SetDirty(controller);
        }
    }
}
#endif
