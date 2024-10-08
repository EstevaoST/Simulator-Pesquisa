using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Windows;
using SFB;
using UnityEngine.UI;
using System.IO;
using System.Xml;
using System.Text;
using System.Globalization;
using System.Linq;

public class AutoGenerateScenariosPanel : MonoBehaviour
{    
    [Header("Fields")]
    public TMP_InputField fieldGenerateQty;
    public TMP_InputField fieldRoomSizeMinX;
    public TMP_InputField fieldRoomSizeMinY;
    public TMP_InputField fieldRoomSizeMaxX;
    public TMP_InputField fieldRoomSizeMaxY;
    public TMP_InputField fieldExitSizeMinX;
    public TMP_InputField fieldExitSizeMaxX;
    public TMP_Dropdown   fieldObstacleType;
    public TMP_InputField fieldObstacleMin;
    public TMP_InputField fieldObstacleMax;
    public TMP_InputField fieldSavePathFolder;
    public TMP_InputField fieldSaveNamingScheme;
    public Toggle fieldRandomGeneration;
    public Button generateButton;

    [Header("Storage Keys")]
    public string savePathKey = "savePathKey";
    public string storageScheme = "autoScenario";

    // Unity events
    private void OnEnable()
    {
        fieldSavePathFolder.onSelect.AddListener(storageFieldSelected);
        generateButton.onClick.AddListener(Generate);

        fieldObstacleType.ClearOptions();
        fieldObstacleType.AddOptions(Enum.GetNames(typeof(ObstacleType)).ToList());

        fieldSavePathFolder  .text = PlayerPrefs.GetString(savePathKey, "");
        fieldGenerateQty     .text = PlayerPrefs.GetString(storageScheme + "/fieldGenerateQty"     , "");
        fieldRoomSizeMinX    .text = PlayerPrefs.GetString(storageScheme + "/fieldRoomSizeMinX"    , "");
        fieldRoomSizeMinY    .text = PlayerPrefs.GetString(storageScheme + "/fieldRoomSizeMinY"    , "");
        fieldRoomSizeMaxX    .text = PlayerPrefs.GetString(storageScheme + "/fieldRoomSizeMaxX"    , "");
        fieldRoomSizeMaxY    .text = PlayerPrefs.GetString(storageScheme + "/fieldRoomSizeMaxY"    , "");
        fieldExitSizeMinX    .text = PlayerPrefs.GetString(storageScheme + "/fieldExitSizeMinX"    , "");
        fieldExitSizeMaxX    .text = PlayerPrefs.GetString(storageScheme + "/fieldExitSizeMaxX"    , "");
        fieldObstacleMin     .text = PlayerPrefs.GetString(storageScheme + "/fieldObstacleMin"     , "");
        fieldObstacleMax     .text = PlayerPrefs.GetString(storageScheme + "/fieldObstacleMax"     , "");
        fieldSaveNamingScheme.text = PlayerPrefs.GetString(storageScheme + "/fieldSaveNamingScheme", "");
        string aux                 = PlayerPrefs.GetString(storageScheme + "/fieldObstacleType"    , "");
        fieldObstacleType    .value = fieldObstacleType.options.FindIndex(x => x.text == aux);
    }
    private void OnDisable()
    {
        fieldSavePathFolder.onSelect.RemoveListener(storageFieldSelected);
        generateButton.onClick.RemoveListener(Generate);
    }

    // Callbacks
    private void storageFieldSelected(string arg0)
    {
        string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", false);
        if (paths.Length > 0)
        {
            fieldSavePathFolder.text = paths[0];
        }
        fieldSavePathFolder.OnDeselect(null);
    }
    private void Generate()
    {
        PlayerPrefs.SetString(storageScheme + "/fieldGenerateQty", fieldGenerateQty.text);
        PlayerPrefs.SetString(storageScheme + "/fieldRoomSizeMinX", fieldRoomSizeMinX.text);
        PlayerPrefs.SetString(storageScheme + "/fieldRoomSizeMinY", fieldRoomSizeMinY.text);
        PlayerPrefs.SetString(storageScheme + "/fieldRoomSizeMaxX", fieldRoomSizeMaxX.text);
        PlayerPrefs.SetString(storageScheme + "/fieldRoomSizeMaxY", fieldRoomSizeMaxY.text);
        PlayerPrefs.SetString(storageScheme + "/fieldExitSizeMinX", fieldExitSizeMinX.text);
        PlayerPrefs.SetString(storageScheme + "/fieldExitSizeMaxX", fieldExitSizeMaxX.text);
        PlayerPrefs.SetString(storageScheme + "/fieldObstacleType", fieldObstacleType.options[fieldObstacleType.value].text);
        PlayerPrefs.SetString(storageScheme + "/fieldObstacleMin", fieldObstacleMin.text);
        PlayerPrefs.SetString(storageScheme + "/fieldObstacleMax", fieldObstacleMax.text);
        PlayerPrefs.SetString(savePathKey, fieldSavePathFolder.text);
        PlayerPrefs.SetString(storageScheme + "/fieldSaveNamingScheme", fieldSaveNamingScheme.text);
        
        if (fieldRandomGeneration.isOn)
            GenerateRandomizedCases();
        else
            GenerateDiscreetCases();
    }

    // Generating
    private void GenerateRandomizedCases()
    {
        int qty = int.Parse(fieldGenerateQty.text);
        for (int i = 1; i <= qty; i++)
        {
            //Room width; Room lenght; Exit size
            float roomWidth = UnityEngine.Random.Range(float.Parse(fieldRoomSizeMinX.text), float.Parse(fieldRoomSizeMaxX.text));
            float roomLength = UnityEngine.Random.Range(float.Parse(fieldRoomSizeMinY.text), float.Parse(fieldRoomSizeMaxY.text));
            float exitWidth = UnityEngine.Random.Range(float.Parse(fieldExitSizeMinX.text), float.Parse(fieldExitSizeMaxX.text));
            float obstacularity = UnityEngine.Random.Range(float.Parse(fieldObstacleMin.text), float.Parse(fieldObstacleMax.text));

            GenerateCaseFile(i, roomWidth, roomLength, exitWidth, obstacularity);
        }
        // to do
        // criar output falando de sucesso

        Debug.Log($"Generated {qty} files");
    }
    private void GenerateDiscreetCases()
    {
        int nSteps = int.Parse(fieldGenerateQty.text);

        float[] widths  = GetValuesFromSteps(nSteps, float.Parse(fieldRoomSizeMinX.text), float.Parse(fieldRoomSizeMaxX.text));
        float[] lenghts = GetValuesFromSteps(nSteps, float.Parse(fieldRoomSizeMinY.text), float.Parse(fieldRoomSizeMaxY.text));
        float[] exits   = GetValuesFromSteps(nSteps, float.Parse(fieldExitSizeMinX.text), float.Parse(fieldExitSizeMaxX.text));
        float[] obstacularities = GetValuesFromSteps(nSteps, float.Parse(fieldObstacleMin.text), float.Parse(fieldObstacleMax.text));     

        int[] indexes = new int[] { 0, 0, 0, 0 };
        int nCase = 0;
        while (true)
        {
            GenerateCaseFile(nCase++, widths[indexes[0]], lenghts[indexes[1]], exits[indexes[2]], obstacularities[indexes[3]]);

            indexes[3]++;
            if (indexes[3] >= obstacularities.Length)
            {
                indexes[3] = 0;
                indexes[2]++;
            }            
            if (indexes[2] >= exits.Length)
            {
                indexes[2] = 0;
                indexes[1]++;
            }
            if (indexes[1] >= lenghts.Length)
            {
                indexes[1] = 0;
                indexes[0]++;
            }
            if (indexes[0] >= widths.Length)
            {
                indexes[0] = 0;
                break;
            }
        }        
        // to do
        // criar output falando de sucesso

        Debug.Log($"Generated {nCase} files");
    }    

    private void GenerateCaseFile(int nCase, float roomWidth, float roomLength, float exitWidth, float obstacularity)
    {
        using (StreamWriter w = new StreamWriter($"{fieldSavePathFolder.text}/{fieldSaveNamingScheme.text}{nCase:000}.scenario", false))
        {
            w.WriteLine("Room width;Room lenght;Exit width;Obst.Type;Obst.Level"); // header
            w.Write(roomWidth.ToString("0.00", SimGlobalConfig.ExportFormat));
            w.Write(";");
            w.Write(roomLength.ToString("0.00", SimGlobalConfig.ExportFormat));
            w.Write(";");
            w.Write(exitWidth.ToString("0.00", SimGlobalConfig.ExportFormat));
            w.Write(";");
            w.Write(fieldObstacleType.options[fieldObstacleType.value].text);
            w.Write(";");
            w.Write(obstacularity.ToString("0.00", SimGlobalConfig.ExportFormat));
            w.Close();
        }
    }

    // Helper
    private float[] GetValuesFromSteps(int nSteps, float min, float max)
    {
        if (min == max)
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
