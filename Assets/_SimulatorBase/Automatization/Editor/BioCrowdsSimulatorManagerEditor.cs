using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BioCrowdsSimulatorManager), true)]
public class BioCrowdsSimulatorManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // static values
        GUI.color = Color.green;
        EditorGUILayout.LabelField("Static");
        int iAux = EditorGUILayout.IntField("Frames to Update", SimGlobalConfig.framesToUpdate.value);
        if (iAux != SimGlobalConfig.framesToUpdate.value)
            SimGlobalConfig.framesToUpdate.value = iAux;
        GUI.color = Color.white;

        // base
        base.OnInspectorGUI();

        // extra controls
        if (GUILayout.Button("Calculate current Density"))
        {
            BioCrowdsSimulatorManager sim = (target as BioCrowdsSimulatorManager);
            float byAgentArea  = sim.CalculateAvgWorldAreaDensity();
            float byAgentArea2 = sim.CalculateAvgWorldAreaDensitySmart();
            float byCells      = sim.CalculateCellsDensity(out float max);
            float byCells2     = sim.CalculateCellsDensitySmart(out max);
            Debug.Log($"AgentArea:{byAgentArea} - Smart:{byAgentArea2} - Cells:{byCells} - Smart:{byCells2}");
        }
    }
}
