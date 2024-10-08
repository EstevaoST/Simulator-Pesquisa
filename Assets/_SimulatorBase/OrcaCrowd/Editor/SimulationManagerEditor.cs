using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace OrcaSimulator.Core
{
    [CustomEditor(typeof(SimulationManager), true)]
    public class SimulationManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUI.color = Color.green;
            EditorGUILayout.LabelField("Static");
            int iAux = EditorGUILayout.IntField("Frames to Update", SimGlobalConfig.framesToUpdate.value);
            if (iAux != SimGlobalConfig.framesToUpdate.value)
                SimGlobalConfig.framesToUpdate.value = iAux;
            GUI.color = Color.white;

            base.OnInspectorGUI();
        }
    }
}
#endif
