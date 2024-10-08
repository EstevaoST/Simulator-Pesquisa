using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using SFB;
using UnityEngine.UI;
using System.IO;
using System.Linq;

public class AutoSummarizePanel : MonoBehaviour
{
    [Header("Fields")]
    public Toggle fieldAverages;
    public Toggle fieldDeviations;
    public Toggle fieldUpperDeviation;
    public Toggle fieldLowerDeviation;
    public TMP_InputField fieldSelectedFolderPath;
    public TMP_InputField fieldSelectedNamingScheme;
    public TMP_InputField fieldSaveNamingScheme;
    public Toggle fieldIgnoreRandomFields;

    public Button summarizeButton;

    [Header("Storage Keys")]
    public string savePathKey = "savePathKey";
    public string storageScheme = "autoSimulation";

    [Header("Summarize fields names")]
    public string[] inputs = { "Room width", "Room lenght", "Exit size", "Input flow", "Flow Duration", "Initial population", "Random Seed", "Obst.Type", "Obst.Level" };
    public string[] dataOutputs = {"Simulation time (seconds)",
                                "Average exit time (seconds)",
                                "Average speed (m/s)",
                                "First exit time(s)",
                                "Average traveled distance (m)",
                                "Avg density (AgentArea)",
                                "Max density (AgentArea)",
                                "Avg density (Agent-SmartArea)",
                                "Max density (Agent-SmartArea)",
                                "Avg density (Cells)",
                                "Max density (Cells)",
                                "Avg density (Cells-SmartArea)",
                                "Max density (Cells-SmartArea)",
                                "Avg density (Global)",
                                "Max density (Global)",
                                "Avg density (Global-SmartArea)",
                                "Max density (Global-SmartArea)"};


    // Unity events
    private void OnEnable()
    {
        fieldSelectedFolderPath.onSelect.AddListener(FolderFieldSelected);
        summarizeButton.onClick.AddListener(Summarize);

        fieldSelectedFolderPath.text    = PlayerPrefs.GetString(savePathKey, "");
        fieldSaveNamingScheme.text      = PlayerPrefs.GetString(storageScheme + "/1", "");
        fieldSelectedNamingScheme.text  = PlayerPrefs.GetString(storageScheme + "/2", "");
        fieldIgnoreRandomFields.isOn    = PlayerPrefs.GetInt   (storageScheme + "/3", 1) != 0;
        fieldAverages.isOn              = PlayerPrefs.GetInt   (storageScheme + "/4", 1) != 0;
        fieldDeviations.isOn            = PlayerPrefs.GetInt   (storageScheme + "/5", 1) != 0;
        fieldUpperDeviation.isOn        = PlayerPrefs.GetInt   (storageScheme + "/6", 1) != 0;
        fieldLowerDeviation.isOn        = PlayerPrefs.GetInt   (storageScheme + "/7", 1) != 0;
    }
    private void OnDisable()
    {
        fieldSelectedFolderPath.onSelect.RemoveListener(FolderFieldSelected);
        summarizeButton.onClick.RemoveListener(Summarize);
    }

    private void FolderFieldSelected(string arg0)
    {
        string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", false);
        if (paths.Length > 0)
        {
            fieldSelectedFolderPath.text = paths[0];
        }
        fieldSelectedFolderPath.OnDeselect(null);
    }

    private void Summarize()
    {
        PlayerPrefs.SetString(savePathKey, fieldSelectedFolderPath.text);
        PlayerPrefs.SetString(storageScheme + "/1", fieldSaveNamingScheme.text);
        PlayerPrefs.SetString(storageScheme + "/2", fieldSelectedNamingScheme.text);
        PlayerPrefs.SetInt   (storageScheme + "/3", fieldIgnoreRandomFields.isOn ? 1 : 0);
        PlayerPrefs.SetInt   (storageScheme + "/4", fieldAverages.isOn ? 1 : 0);
        PlayerPrefs.SetInt   (storageScheme + "/5", fieldDeviations.isOn ? 1 : 0);
        PlayerPrefs.SetInt   (storageScheme + "/6", fieldUpperDeviation.isOn ? 1 : 0);
        PlayerPrefs.SetInt   (storageScheme + "/7", fieldLowerDeviation.isOn ? 1 : 0);

        var allDatas = Directory.GetFiles(fieldSelectedFolderPath.text).ToList();
        allDatas = allDatas.FindAll(x => x.EndsWith(".csv") && (fieldSelectedNamingScheme.text == "" || x.Contains(fieldSelectedNamingScheme.text)));

        // all resulting dictionary
        Dictionary<string, List<float[]>> dict = new Dictionary<string, List<float[]>>();
        char sep = ';'; // separator

        HashSet<string> allheaders = new HashSet<string>();

        // reading
        foreach (var dataFile in allDatas)
        {
            StreamReader r = new StreamReader(dataFile);
            string[] header = r.ReadLine().Split(sep);
            foreach (string s in header)
                allheaders.Add(s);

            int finIndex = Array.IndexOf(header, "Finished");
            int inIndex  = Array.IndexOf(header, "INPUTS");
            int outIndex = Array.IndexOf(header, "OUTPUTS");

            int inSize = outIndex - inIndex - 1;
            int outSize = header.Length - outIndex- 1;

            int randIndex = Array.IndexOf(header, "Random Seed");

            while(!r.EndOfStream)
            {
                var l = r.ReadLine().Split(sep);
                if (l[finIndex] != "Yes")
                    continue;

                var aux = l.Skip(inIndex + 1).Take(outIndex - inIndex - 1).ToList();
                for (int i = 0; i < aux.Count; i++) // fix entries that are in wrong float format
                {
                    if(float.TryParse(aux[i], System.Globalization.NumberStyles.AllowDecimalPoint, SimGlobalConfig.ExportFormat, out float a) ||
                       float.TryParse(aux[i], out a))
                    {
                        aux[i] = a.ToString("0.00", SimGlobalConfig.ExportFormat);
                    }
                }

                if (fieldIgnoreRandomFields.isOn) {
                    aux[randIndex - (inIndex + 1)] = "-";
                }

                // add fixed fields that are missing on the simulations file
                // this is done by index, maybe make it depend on field name...
                for (int i = aux.Count; i < inputs.Length; i++) {
                    aux.Add("0.00");
                }

                string key = String.Join(sep, aux);                
                var query = l.Skip(outIndex + 1).Take(outSize);
                float[] data = query.Select(x => {                    
                    if (float.TryParse(x, System.Globalization.NumberStyles.AllowDecimalPoint, SimGlobalConfig.ExportFormat, out float a) ||
                        float.TryParse(x, out a))
                    {
                        return a;
                    }
                    throw new Exception("Could not parse float");
                }).ToArray();

                if (!dict.ContainsKey(key))
                    dict[key] = new List<float[]>();
                dict[key].Add(data);
            }
            r.Close();
        }

        if (dict.Count <= 0)
            return;

        //////////////////
        // writing
        //////////////////
        StreamWriter w = new StreamWriter($"{fieldSelectedFolderPath.text}/{fieldSaveNamingScheme.text}.csv");               

        // Write Header
        List<string> headerItems = new List<string>();
        if (fieldAverages.isOn)
            headerItems.Add("Mean {0}");
        if (fieldDeviations.isOn)
            headerItems.Add("StdDev {0}");
        if (fieldUpperDeviation.isOn)
            headerItems.Add("Upper {0}");
        if (fieldLowerDeviation.isOn)
            headerItems.Add("Lower {0}");
        string baseHeader = string.Join(sep, headerItems);

        w.Write("Finished");
        w.Write(sep + "INPUTS");
        w.Write(sep + String.Join(sep, inputs));
        w.Write(sep + "Count");
        w.Write(sep + "OUTPUTS");
        w.Write(sep + String.Join(sep, dataOutputs.Select(x => string.Format(baseHeader, x))));
        w.WriteLine();

        // Write lines
        foreach(var e in dict)
        {
            if (e.Value.Count <= 0)
                continue;

            w.Write("Yes"); // Finished
            w.Write(sep); // INPUTS
            w.Write(sep + e.Key);            
            w.Write(sep + e.Value.Count.ToString(SimGlobalConfig.ExportFormat)); // Count
            w.Write(sep); // OUTPUTS
            for (int i = 0; i < e.Value[0].Length; i++)
            {
                float avg = e.Value.Average(x => x[i]);
                float dev = e.Value.StdDev(x => x[i]);

                if (fieldAverages.isOn)
                    w.Write(sep + avg.ToString("0.000000", SimGlobalConfig.ExportFormat)); // Mean
                if (fieldDeviations.isOn)
                    w.Write(sep + dev.ToString("0.000000", SimGlobalConfig.ExportFormat)); // StdDev
                if (fieldUpperDeviation.isOn)
                    w.Write(sep + (avg + dev).ToString("0.000000", SimGlobalConfig.ExportFormat)); // Upper deviation
                if (fieldLowerDeviation.isOn)
                    w.Write(sep + (avg - dev).ToString("0.000000", SimGlobalConfig.ExportFormat)); // Lower deviation
            }
            w.WriteLine();
        }        
        Debug.Log($"Summarized {dict.Sum(x => x.Value.Count)} lines from {allDatas.Count} files into {((FileStream)w.BaseStream).Name}");

        w.Close();
    }
}
