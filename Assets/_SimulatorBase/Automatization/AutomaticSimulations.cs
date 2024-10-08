using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public static class AutomaticSimulations
{
    public static string simulationScene;
    public static string resultPath;
    public static List<string> caseFilePaths;
    public static int simulateEachCase;
    public static string namingScheme;
    public static bool fixedRandomization;
    public static int fixedRandomizationSeed;

    private static AutomatizationUI automatizationUI = null;

    private static int totalSimulation;
    private static int simulationCount;
    private static int simulationSceneCount;
    private static int[] fixedRandoms;

    private static string countFormat;

    public static void StartAutomatizedSimulations()
    {
        simulationCount = 0;
        simulationSceneCount = 0;
        totalSimulation = caseFilePaths.Count * simulateEachCase;
        countFormat = string.Concat(System.Linq.Enumerable.Repeat("0", totalSimulation.ToString().Length));
        if (fixedRandomization)
        {
            fixedRandoms = new int[totalSimulation];
            Random.InitState(fixedRandomizationSeed);
            for (int i = 0; i < totalSimulation; i++)
            {
                Random.Range(0, 1);
                fixedRandoms[i] = Random.seed;                
            }
        }

        automatizationUI = (GameObject.Instantiate(Resources.Load("Automatization/AutoSim UI")) as GameObject).GetComponent<AutomatizationUI>();
        GameObject.DontDestroyOnLoad(automatizationUI);

        CallNextSimulation();
    }
    private static void CallNextSimulation()
    {
        if (simulationSceneCount >= simulateEachCase)
        {
            simulationSceneCount = 0;
            caseFilePaths.RemoveAt(0);
        }
        if(caseFilePaths.Count <= 0)
        {
            Debug.Log("Automatic simulation finished");
            SceneManager.LoadScene(0);
            return;
        }

        Debug.Log($"-- Simulating {(simulationCount+1).ToString(countFormat)}/{totalSimulation}");

        // load scene
        SceneManager.sceneLoaded += SimulationLoaded;
        SceneManager.LoadScene(simulationScene);        
    }

    private static void SimulationLoaded(Scene arg0, LoadSceneMode arg1)
    {
        SceneManager.sceneLoaded -= SimulationLoaded;

        // read file
        StreamReader r = new StreamReader(caseFilePaths[0]);
        r.ReadLine(); // skip header
        string[] strData = r.ReadLine().Split(';');
        //"Room width;Room lenght;Exit size;ObstType;ObstLevel;Input flow;Flow Duration;Initial population"
        float rWidth  = float.Parse(strData[0], SimGlobalConfig.ExportFormat);
        float rLength = float.Parse(strData[1], SimGlobalConfig.ExportFormat);
        float eWidth  = float.Parse(strData[2], SimGlobalConfig.ExportFormat);
        ObstacleType oType = Enum.Parse<ObstacleType>(strData[3], true);
        float oLevel  = float.Parse(strData[4], SimGlobalConfig.ExportFormat);
        float inFlow  = float.Parse(strData[5], SimGlobalConfig.ExportFormat);
        float inTime  = float.Parse(strData[6], SimGlobalConfig.ExportFormat);
        float pop     = float.Parse(strData[7], SimGlobalConfig.ExportFormat);
        r.Close();

        // setup random
        if (fixedRandomization)
        {
            Random.InitState(fixedRandoms[simulationCount]);
        }

        // setup simulator
        GeneratorSimulatorManager orca = Component.FindObjectOfType<GeneratorSimulatorManager>();
        if (orca != null)
        {
            SetupORCASimulation(orca, rWidth, rLength, eWidth, inFlow, inTime, pop);
            // TO DO: atualizar isso um dia
        }

        BioCrowdsSimulatorManager biocrowd = Component.FindObjectOfType<BioCrowdsSimulatorManager>();
        if (biocrowd != null)
        {
            biocrowd.obstacleType = oType;
            biocrowd.obstacularity = oLevel;
            biocrowd.fixedRandom = fixedRandomization;
            if(fixedRandomization)
                biocrowd.fixedRandomSeed = fixedRandoms[simulationCount];
            SetupBioCrowdsSimulation(biocrowd, rWidth, rLength, eWidth, inFlow, inTime, pop);

            automatizationUI.SetData(simulationCount, totalSimulation, biocrowd);
        }        
    }
    static void SetupORCASimulation(GeneratorSimulatorManager gen, float rWidth, float rLength, float eWidth, float inFlow, float inTime, float pop)
    {
        // setup
        gen.saveSummary = resultPath + '/' + namingScheme + ".csv";
        gen.appendSummary = true;
        gen.roomWidth = rWidth;
        gen.roomLength = rLength;
        gen.exitSize = eWidth;
        gen.inputFlow = inFlow;
        gen.flowDuration = inTime;
        gen.initialPopulation = pop;
        gen.inputsAgents = Mathf.CeilToInt(inFlow * inTime);
        gen.generateScene = true;
        gen.OnSimulationFinished += SimulationFinished;
    }
    static void SetupBioCrowdsSimulation(BioCrowdsSimulatorManager gen, float rWidth, float rLength, float eWidth, float inFlow, float inTime, float pop)
    {
        // setup
        gen.saveSummary = resultPath + '/' + namingScheme + ".csv";
        gen.appendSummary = true;
        gen.roomWidth = rWidth;
        gen.roomLength = rLength;
        gen.exitSize = eWidth;
        gen.inputFlow = inFlow;
        gen.flowDuration = inTime;
        gen.initialPopulation = pop;
        gen.inputsAgents = Mathf.CeilToInt(inFlow * inTime);        
        gen.OnSimulationFinished += SimulationFinished;
    }

    private static void SimulationFinished()
    {
        simulationSceneCount++;
        simulationCount++;

        CallNextSimulation();
    }

}
