using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using SFB;
using UnityEngine.UI;
using System.IO;
using System.Linq;

public class AutoSimulationPanel : MonoBehaviour
{
    [Header("Fields")]
    public TMP_Dropdown scenesDropdown;
    public TMP_InputField fieldSelectedFolderPath;
    public TMP_InputField fieldSaveNamingScheme;
    public TMP_InputField fieldSimulateQty;
    public Toggle fieldRandomize;
    public TMP_InputField fieldRandomizationValue;

    public Button simulateButton;

    [Header("Storage Keys")]
    public string savePathKey = "savePathKey";
    public string storageScheme = "autoSimulation";

    // Unity events
    private void OnEnable()
    {
        fieldSelectedFolderPath.onSelect.AddListener(FolderFieldSelected);        
        fieldRandomize.onValueChanged.AddListener(RandomizeToggleChanged);

        simulateButton.onClick.AddListener(Simulate);

        int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
        string[] scenes = new string[sceneCount];
        for (int i = 0; i < sceneCount; i++)
        {
            scenes[i] = System.IO.Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i));
            
        }
        scenesDropdown.ClearOptions();
        scenesDropdown.AddOptions(new List<string>(scenes));

        fieldSelectedFolderPath.text = PlayerPrefs.GetString(savePathKey, "");
        fieldSimulateQty.text        = PlayerPrefs.GetString(storageScheme + "/1", "");
        string aux                   = PlayerPrefs.GetString(storageScheme + "/2", "");
        scenesDropdown.value         = scenesDropdown.options.FindIndex(x => x.text == aux);
        fieldSaveNamingScheme.text   = PlayerPrefs.GetString(storageScheme + "/3", "");
        fieldRandomize.isOn          = PlayerPrefs.GetInt   (storageScheme + "/5", 0) != 0;
        fieldRandomizationValue.text = PlayerPrefs.GetInt   (storageScheme + "/6", 0).ToString();
    }
    private void OnDisable()
    {
        fieldSelectedFolderPath.onSelect.RemoveListener(FolderFieldSelected);
        simulateButton.onClick.RemoveListener(Simulate);
        fieldRandomize.onValueChanged.RemoveListener(RandomizeToggleChanged);
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

    private void Simulate()
    {
        PlayerPrefs.SetString(savePathKey, fieldSelectedFolderPath.text);
        PlayerPrefs.SetString(storageScheme + "/1", fieldSimulateQty.text);
        PlayerPrefs.SetString(storageScheme + "/2", scenesDropdown.options[scenesDropdown.value].text);
        PlayerPrefs.SetString(storageScheme + "/3", fieldSaveNamingScheme.text);
        PlayerPrefs.SetInt   (storageScheme + "/5", fieldRandomize.isOn ? 1 : 0);
        PlayerPrefs.SetInt   (storageScheme + "/6", int.Parse(fieldRandomizationValue.text));        


        var allCases = Directory.GetFiles(fieldSelectedFolderPath.text).ToList();
        allCases = allCases.FindAll(x => x.EndsWith(".case"));

        AutomaticSimulations.simulationScene = scenesDropdown.options[scenesDropdown.value].text;
        AutomaticSimulations.caseFilePaths = allCases;
        AutomaticSimulations.simulateEachCase = int.Parse(fieldSimulateQty.text);
        AutomaticSimulations.resultPath = fieldSelectedFolderPath.text;
        AutomaticSimulations.namingScheme = fieldSaveNamingScheme.text;
        AutomaticSimulations.fixedRandomization = fieldRandomize.isOn;
        AutomaticSimulations.fixedRandomizationSeed = int.Parse(fieldRandomizationValue.text);

        AutomaticSimulations.StartAutomatizedSimulations();
        // to do
        // criar output
        // Debug.Log($"Starting simulating {count} files total, for {allScenarios.Count} scenarios");
    }

    private void RandomizeToggleChanged(bool arg0)
    {
        fieldRandomizationValue.gameObject.SetActive(arg0);
    }
}
