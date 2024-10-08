using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Windows;
using SFB;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using System.Globalization;

public class AutoGenerateCrowdsPanel : MonoBehaviour
{
    [Header("Fields")]
    public TMP_InputField fieldSelectedFolderPath;
    public TMP_InputField fieldGenerateQty;
    public TMP_InputField fieldLocalAgentsMin;
    public TMP_InputField fieldLocalAgentsMax;   
    public TMP_InputField fieldEnteringAgentsMin;
    public TMP_InputField fieldEnteringAgentsMax;
    public TMP_InputField fieldEnteringRatioMin;
    public TMP_InputField fieldEnteringRatioMax;    
    public TMP_InputField fieldSaveNamingScheme;
    public Toggle fieldRandomGeneration;
    public Button generateButton;

    [Header("Storage Keys")]
    public string savePathKey = "savePathKey";
    public string storageScheme = "autoCrowds";

    // Unity events
    private void OnEnable()
    {
        fieldSelectedFolderPath.onSelect.AddListener(FolderFieldSelected);
        generateButton.onClick.AddListener(Generate);

        fieldSelectedFolderPath.text= PlayerPrefs.GetString(savePathKey, "");
        fieldGenerateQty.text       = PlayerPrefs.GetString(storageScheme + "/1", "");
        fieldLocalAgentsMin.text    = PlayerPrefs.GetString(storageScheme + "/2", "");
        fieldLocalAgentsMax.text    = PlayerPrefs.GetString(storageScheme + "/3", "");
        fieldEnteringAgentsMin.text = PlayerPrefs.GetString(storageScheme + "/4", "");
        fieldEnteringAgentsMax.text = PlayerPrefs.GetString(storageScheme + "/5", "");
        fieldEnteringRatioMin.text  = PlayerPrefs.GetString(storageScheme + "/6", "");
        fieldEnteringRatioMax.text  = PlayerPrefs.GetString(storageScheme + "/7", "");
        fieldSaveNamingScheme.text  = PlayerPrefs.GetString(storageScheme + "/8", "");
    }
    private void OnDisable()
    {
        fieldSelectedFolderPath.onSelect.RemoveListener(FolderFieldSelected);
        generateButton.onClick.RemoveListener(Generate);
    }

    // Callbacks
    private void FolderFieldSelected(string arg0)
    {
        string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", false);
        if (paths.Length > 0)
        {
            fieldSelectedFolderPath.text = paths[0];
        }
        fieldSelectedFolderPath.OnDeselect(null);
    }
    private void Generate()
    {
        PlayerPrefs.SetString(savePathKey, fieldSelectedFolderPath.text);
        PlayerPrefs.SetString(storageScheme + "/1", fieldGenerateQty.text);
        PlayerPrefs.SetString(storageScheme + "/2", fieldLocalAgentsMin.text);
        PlayerPrefs.SetString(storageScheme + "/3", fieldLocalAgentsMax.text);
        PlayerPrefs.SetString(storageScheme + "/4", fieldEnteringAgentsMin.text);
        PlayerPrefs.SetString(storageScheme + "/5", fieldEnteringAgentsMax.text);
        PlayerPrefs.SetString(storageScheme + "/6", fieldEnteringRatioMin.text);
        PlayerPrefs.SetString(storageScheme + "/7", fieldEnteringRatioMax.text);
        PlayerPrefs.SetString(storageScheme + "/8", fieldSaveNamingScheme.text);

        var allScenarios = Directory.GetFiles(fieldSelectedFolderPath.text).ToList();
        allScenarios = allScenarios.FindAll(x => x.EndsWith(".scenario"));

        int count;
        if(fieldRandomGeneration.isOn)
            count = GenerateRandomizedCases(allScenarios);
        else
            count = GenerateDiscreetCases(allScenarios);

        // to do
        // criar output falando de sucesso

        Debug.Log($"Generated {count} files total, for {allScenarios.Count} scenarios");
    }

    private int GenerateRandomizedCases(List<string> allScenarios)
    {
        int qty = int.Parse(fieldGenerateQty.text);
        int count = 0;

        foreach (var scenario in allScenarios)
        {
            StreamReader r = new StreamReader(scenario);
            string sceneHeader = r.ReadLine();
            string sceneData = r.ReadLine();
            r.Close();

            for (int i = 1; i <= qty; i++)
            {
                //Room width; Room lenght; Exit size
                float inPop = UnityEngine.Random.Range(int.Parse(fieldEnteringAgentsMin.text), int.Parse(fieldEnteringAgentsMax.text));
                float inRatio = UnityEngine.Random.Range(int.Parse(fieldEnteringRatioMin.text), int.Parse(fieldEnteringRatioMax.text));
                float pop = UnityEngine.Random.Range(int.Parse(fieldLocalAgentsMin.text), int.Parse(fieldLocalAgentsMax.text));

                if (inRatio == 0 || inPop == 0)
                {
                    inRatio = 0;
                    inPop = 0;
                }

                float inTime = inRatio <= 0 ? 0 : inPop / inRatio;

                GenerateFile(scenario.Replace(".scenario", $"{fieldSaveNamingScheme.text}{i:000}.case"), sceneHeader, sceneData,
                          inRatio, inTime, pop);                

                count++;
            }
        }

        return count;
    }
    private int GenerateDiscreetCases(List<string> allScenarios)
    {
        int nSteps = int.Parse(fieldGenerateQty.text);
        int count = 0;

        float[] inPops = GetValuesFromSteps(nSteps, float.Parse(fieldEnteringAgentsMin.text), float.Parse(fieldEnteringAgentsMax.text));
        float[] inRatios = GetValuesFromSteps(nSteps, float.Parse(fieldEnteringRatioMin.text), float.Parse(fieldEnteringRatioMax.text));
        float[] pops = GetValuesFromSteps(nSteps, float.Parse(fieldLocalAgentsMin.text), float.Parse(fieldLocalAgentsMax.text));

        foreach (var scenario in allScenarios)
        {
            StreamReader r = new StreamReader(scenario);
            string sceneHeader = r.ReadLine();
            string sceneData = r.ReadLine();
            r.Close();
            
            int[] indexes = new int[] { 0, 0, 0 };
            int i = 0;
            while (true)
            {
                float inPop = inPops[indexes[0]];
                float inRatio = inRatios[indexes[1]];
                float pop = pops[indexes[2]];

                if (inRatio == 0 || inPop == 0)
                {
                    inRatio = 0;
                    inPop = 0;
                }

                float inTime = inRatio <= 0 ? 0 : inPop / inRatio;
                GenerateFile(scenario.Replace(".scenario", $"{fieldSaveNamingScheme.text}{i:000}.case"), sceneHeader, sceneData, inRatio, inTime, pop);

                i++;
                count++;

                indexes[2]++;
                if (indexes[2] >= pops.Length)
                {
                    indexes[2] = 0;
                    indexes[1]++;
                }
                if (indexes[1] >= inRatios.Length)
                {
                    indexes[1] = 0;
                    indexes[0]++;
                }
                if (indexes[0] >= inPops.Length)
                {
                    indexes[0] = 0;
                    break;
                }
            }
        }

        return count;
    }

    private void GenerateFile(string filename, string header, string sourceData, float inRatio, float inTime, float localPop)
    {
        StreamWriter w = new StreamWriter(filename, false);

        // header
        w.Write(header);
        w.Write(";");
        w.WriteLine("Input flow; Flow Duration; Initial population"); // header

        // data
        w.Write(sourceData);
        w.Write(";");
        w.Write(inRatio.ToString("0.00", SimGlobalConfig.ExportFormat));
        w.Write(";");
        w.Write(inTime.ToString("0.00", SimGlobalConfig.ExportFormat));
        w.Write(";");
        w.Write(localPop.ToString("0.00", SimGlobalConfig.ExportFormat));
        w.Close();
    }

    // Helper
    private float[] GetValuesFromSteps(int nSteps, float min, float max)
    {
        if(min == max)
            nSteps = 1;
        
        float[] values = new float[nSteps];


        if (nSteps > 1)
        {
            for (int i = 0; i < nSteps; i++)
            {
                values[i] = Mathf.Lerp(min, max, i / (nSteps - 1.0f));
            }
        }
        else
        {
            values[0] = max;
        }

        return values;
    }
}
