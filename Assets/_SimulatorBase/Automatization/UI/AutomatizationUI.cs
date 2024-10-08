using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class AutomatizationUI : MonoBehaviour
{
    public TMP_Text simulationText;
    public TMP_InputField frameskipInput;
    public ScrollRect parameterList;
    public Button skipSimButton;

    private BioCrowdsSimulatorManager manager;

    private void Awake()
    {
        frameskipInput.onValueChanged.AddListener(FrameSkipChanged);
        skipSimButton.onClick.AddListener(SkipSimulation);
    }    

    internal void SetData(int curSim, int totalSim, BioCrowdsSimulatorManager biocrowd)
    {
        simulationText.text = $"{biocrowd.saveSummary} {curSim.ToString("000")}/{totalSim.ToString("000")}";
        frameskipInput.SetTextWithoutNotify(SimGlobalConfig.framesToUpdate.value.ToString());
        manager = biocrowd;

        ClearParameters();
        AddParameter("Width", biocrowd.roomWidth.ToString());
        AddParameter("Lenght", biocrowd.roomLength.ToString());
        AddParameter("Exit", biocrowd.exitSize.ToString());
        AddParameter("Input flow", biocrowd.inputFlow.ToString());
        AddParameter("Input duration", biocrowd.flowDuration.ToString());
        AddParameter("Initial pop", biocrowd.initialPopulation.ToString());   
        AddParameter("ObstacleType", biocrowd.obstacleType.ToString());
        AddParameter("ObstacleLevel", biocrowd.obstacularity.ToString());
        if (biocrowd.fixedRandom)
            AddParameter("Seed", biocrowd.fixedRandomSeed.ToString());
    }
    private void ClearParameters()
    {
        int c = 0;
        foreach (RectTransform item in parameterList.content)
        {
            if (c++ >= 2)
                Destroy(item.gameObject);
        }
    }
    private void AddParameter(string name, string value)
    {
        RectTransform tName  = Instantiate(parameterList.content.GetChild(0), parameterList.content) as RectTransform;
        RectTransform tValue = Instantiate(parameterList.content.GetChild(1), parameterList.content) as RectTransform;

        tName.GetComponent<TMP_Text>().text = name;
        tValue.GetComponent<TMP_Text>().text = value;
    }

    private void FrameSkipChanged(string value)
    {
        if(int.TryParse(value, out int fs))
        {
            SimGlobalConfig.framesToUpdate.value = fs;
        }
    }
    private void SkipSimulation()
    {
        manager.InterruptSimulation();
    }
}
