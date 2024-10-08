using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(CrowdSimConverter), true)]
public class CrowdSimConverterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var rect = GUILayoutUtility.GetRect(200, 40);
        var t = (target as CrowdSimConverter);
     
        if (GUI.Button(rect, "Read NavMesh as model"))
        {
            if (t.crowdSimFile != null)
            {
                t.ReadNGenerateModel();
            }
            else
                Debug.LogError("Inform a CrowdSim file first");
        }

        rect = GUILayoutUtility.GetRect(200, 40);
        if (GUI.Button(rect, "Read Contexts"))
        {
            if (t.crowdSimFile != null)
            {
                t.ReadNGenerateSpawners();
            }
            else
                Debug.LogError("Inform a CrowdSim file first");
        }

        t.scenarioRoot = EditorGUILayout.ObjectField("Scenario Root Transform", t.scenarioRoot, typeof(Transform), true) as Transform;
        t.contextsRoot = EditorGUILayout.ObjectField("Contexts Root Transform", t.contextsRoot, typeof(Transform), true) as Transform;
        t.filename = EditorGUILayout.TextField("Filename", t.filename) as string;

        serializedObject.ApplyModifiedProperties();

        rect = GUILayoutUtility.GetRect(200, 40);
        if (GUI.Button(rect, "Convert to estimation"))
        {
            if (t.scenarioRoot && t.contextsRoot && !string.IsNullOrEmpty(t.filename))
            {
                t.ConvertToEstimatorFile();
            }
            else
                Debug.LogError("Missing parameters");
        }

    }
}
