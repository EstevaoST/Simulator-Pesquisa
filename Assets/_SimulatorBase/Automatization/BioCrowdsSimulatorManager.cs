using Biocrowds.Core;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Random = UnityEngine.Random;

[DefaultExecutionOrder(-1)]
public class BioCrowdsSimulatorManager : MonoBehaviour
{
    public World simulation;

    [Header("Scene Generation")]
    public float roomLength;
    public float roomWidth;
    public float inputFlow, flowDuration, initialPopulation, exitSize;
    public int inputsAgents;
    public float obstacularity;
    public ObstacleType obstacleType;
    public GameObject obstacleParent;
    private ObstacleGenerator obstacleGenerator;

    public bool fixedRandom;
    public int fixedRandomSeed;

    [Header("Result Saving")]
    // Data collecting variables
    public float simulationTimeLimit = 8 * 60;

    public string saveSummary;
    public bool appendSummary = true;
    public bool simulationInterrupted = false;
    public float density_side = 2;
    public float density_area => density_side * density_side;
    public float stepTime = 0.02f;
    private float firstExitTime = -1;
    private Dictionary<Agent, AgentData> agentData = new Dictionary<Agent, AgentData>();
    private WorldData worldData = new WorldData();

    private float updateTimer = 0;
    private float simulationTime = 0;
    public Action OnSimulationFinished = null;

    protected void Start()
    {
        StartCoroutine(SimulationLoop());
    }
    IEnumerator SimulationLoop()
    {
        // Setup
        simulation.simulateOnUpdate = false;
        SetupSceneForParameters();
        obstacleGenerator?.Generate();
        simulation.LoadWorld();
        simulation.OnAgentFinished += AgentFinished;

        while (!simulation.Ready)
            yield return null;

        updateTimer = 0;

        // Simulation loop
        while (!simulation.Finished && !simulationInterrupted)
        {
            simulation.Update(stepTime);
            foreach (Agent a in simulation._agents)
            {
                CollectData(a);
            }
            CollectWorldData();

            if (--updateTimer <= 0)
            {
                yield return null;
                updateTimer = SimGlobalConfig.framesToUpdate.value;
            }

            simulationTime += stepTime;
            if (simulationTime > simulationTimeLimit)
            {
                simulationInterrupted = true;
            }
        }
        SimulationFinished();
    }

    public void SetupSceneForParameters()
    {
        var room = GameObject.Find("Scenario");
        var agents = GameObject.Find("Agents");
        var goals = GameObject.Find("Goals");
        var spawns = GameObject.Find("Spawns");

        if (inputsAgents != 0 && inputFlow != 0)
            flowDuration = inputsAgents / inputFlow;
        else
            flowDuration = 0;

        // clean the scene         
        foreach (var ag in simulation._agents)
            Destroy(ag.gameObject);
        simulation._agents.Clear();

        // criar uma nova sala
        #region alterar tamanho da sala
        Vector2 roomSize = new Vector2(roomWidth, roomLength);
        simulation.SetDimensionAndOffset(roomSize + Vector2.up * 2, -roomSize / 2);
        if (room != null)
        {
            room.transform.localScale = new Vector3(roomSize.x, 1, roomSize.y);
        }
        foreach (Transform g in goals.transform)
        {
            Transform place = SearchChildOf(room.transform, g.name);
            if (place != null)
                g.transform.position = place.position - g.transform.forward * g.transform.localScale.z * 2;
        }
        foreach (Transform s in spawns.transform)
        {
            Transform place = SearchChildOf(room.transform, s.name);
            if (place != null)
                s.transform.position = place.position;// - s.transform.forward * s.transform.localScale.z;
        }
        #endregion

        #region alterar saída e tamanho da saída
        var gs = simulation.Areas[0].initialAgentsGoalList;
        float exitSizeParc = exitSize / gs.Count();
        foreach (GameObject g in gs)
            g.transform.localScale = Vector3.one + Vector3.right * (exitSizeParc - 1);// + Vector3.forward * roomLength;
        #endregion

        simulation.MAX_AGENTS = initialPopulation + inputsAgents;

        // Setup agents already on scene
        simulation.Areas[0].initialNumberOfAgents = Mathf.CeilToInt(initialPopulation);
        simulation.Areas[0].quantitySpawnedEachCycle = 0;
        simulation.Areas[0].transform.localScale = new Vector3(roomWidth, 1, roomLength);

        // setup input flow of agents
        simulation.Areas[1].initialNumberOfAgents = 0;
        simulation.Areas[1].quantitySpawnedEachCycle = 1;
        simulation.Areas[1].cycleLenght = 1 / inputFlow;
        simulation.Areas[1].limitRepeatingSpawn = true;
        simulation.Areas[1].quantityLimitToSpawn = inputsAgents;
        simulation.Areas[1].transform.localScale = new Vector3(1, 1, 1);

        // Setup obstacle generator
        obstacleGenerator = obstacleParent.GetComponents<ObstacleGenerator>().FirstOrDefault(x => x.GetType() == obstacleType.GetGeneratorType());
        if (obstacleGenerator != null)
        {
            obstacleGenerator.roomWidth = roomWidth;
            obstacleGenerator.roomLength = roomLength;
            obstacleGenerator.obstacularity = obstacularity;
        }

        // Setup random
        if (fixedRandom)
            Random.InitState(fixedRandomSeed);
        else
            fixedRandomSeed = Random.seed;
    }
    private void AgentFinished(Agent agent)
    {
        if (firstExitTime == -1)
            firstExitTime = simulation.SimulationTime;
    }
    private void SimulationFinished()
    {
        SaveSummaryResult();
        OnSimulationFinished?.Invoke();
    }
    public void InterruptSimulation()
    {
        simulationInterrupted = true;       
    }

    // Save data
    protected void SaveSummaryResult()
    {
        if (saveSummary == "")
            saveSummary = SceneManager.GetActiveScene().name + ".csv";

        bool addHeader = !appendSummary || !File.Exists(saveSummary);
        StreamWriter sw = new StreamWriter(saveSummary, appendSummary);

        if (addHeader)
        {
            // 0: simulation has finished
            sw.Write("Finished");
            sw.Write(";INPUTS;");
            // 2 - 10 : Room definition
            sw.Write("Room width;Room lenght;Exit size;Input flow;Flow Duration;Initial population");
            sw.Write(";Random Seed");
            sw.Write(";Obst.Type");
            if(obstacleGenerator!= null)
                sw.Write(";" + string.Join(';', obstacleGenerator.ObstacleDataNames));

            sw.Write(";OUTPUTS;");
            // 12 - 16 : Basic simulation results
            sw.Write("Simulation time (seconds)");
            sw.Write(";Average exit time (seconds)");
            sw.Write(";Average speed (m/s)");
            sw.Write(";First exit time(s)");
            sw.Write(";Average traveled distance (m)");

            // 17 - : Densities results
            sw.Write(";Avg density (AgentArea) (ag/m²)");
            sw.Write(";Max density (AgentArea) (ag/m²)");
            sw.Write(";Avg density (Agent-SmartArea) (ag/m²)");
            sw.Write(";Max density (Agent-SmartArea) (ag/m²)");
            sw.Write(";Avg density (Cells) (ag/m²)");
            sw.Write(";Max density (Cells) (ag/m²)");
            sw.Write(";Avg density (Cells-SmartArea) (ag/m²)");
            sw.Write(";Max density (Cells-SmartArea) (ag/m²)");
            sw.Write(";Avg density (Global) (ag/m²)");
            sw.Write(";Max density (Global) (ag/m²)");
            sw.Write(";Avg density (Global-SmartArea) (ag/m²)");
            sw.Write(";Max density (Global-SmartArea) (ag/m²)");

            sw.WriteLine();
        }

        // write if finished
        if (simulationInterrupted)
            sw.Write("No");
        else
            sw.Write("Yes");
        sw.Write(";;");        

        ////// write simulation input
        // base data
        float[] fData = { roomWidth, roomLength, exitSize, inputFlow, flowDuration, initialPopulation, fixedRandomSeed };
        List<string> sData = fData.Select(x => x.ToString(SimGlobalConfig.ExportFormat)).ToList();

        // obstacle data
        sData.Add(obstacleType.ToString());
        if(obstacleGenerator != null)
            sData.AddRange(obstacleGenerator.GetObstacleData().Select(x => x.ToString(SimGlobalConfig.ExportFormat)));
        
        // join all
        string data = string.Join(';', sData);
        sw.Write(data);
        sw.Write(";;");

        List<Agent> agents = simulation._finishedAgents;

        float simulatedTime = simulation.SimulationTime;
        float firstExitTime = this.firstExitTime;

        // calculate simulation output
        float avgFrames    = agentData.Values.Average(x => (float)x.simulatedFrames);        
        float avgSpeed1    = agentData.Values.Average(x => x.meanSpeed);
        //float avgSpeed2    = agentData.Values.Sum(x => x.meanSpeed * x.simulatedFrames) / avgFrames;
        float avgDist      = agentData.Values.Average(x => x.distanceTraveled);
                           
        float avgDenAgent  = agentData.Values.Average(x => x.meanLocalDensityArea);
        float maxDenAgent  = agentData.Values.Max(x => x.maxLocalDensityArea);
        float avgDenAgentS = agentData.Values.Average(x => x.meanLocalDensitySmartArea);
        float maxDenAgentS = agentData.Values.Max(x => x.maxLocalDensitySmartArea);
        //float avgDenAg2  = agentData.Values.Sum(x => x.meanLocalDensityArea * x.simulatedFrames) / avgFrames;        
        float avgDenCells  = worldData.meanCellDensity;
        float maxDenCells  = worldData.maxCellDensity;
        float avgDenCellsS = worldData.meanCellDensitySmart;
        float maxDenCellsS = worldData.maxCellDensitySmart;
        float avgDenGlob   = worldData.meanGlobalDensity;
        float maxDenGlob   = worldData.maxGlobalDensity;
        float avgDenGlobS  = worldData.meanGlobalDensitySmart;
        float maxDenGlobS  = worldData.maxGlobalDensitySmart;

        // write simulated time output
        fData = new float[]{ simulatedTime , (avgFrames * stepTime) , avgSpeed1, firstExitTime, avgDist,
                             avgDenAgent , maxDenAgent, 
                             avgDenAgentS, maxDenAgentS,
                             avgDenCells , maxDenCells, 
                             avgDenCellsS, maxDenCellsS,
                             avgDenGlob  , maxDenGlob,
                             avgDenGlobS , maxDenGlobS
                           };
        data = string.Join(';', fData.Select(x => x.ToString(SimGlobalConfig.ExportFormat)));
        sw.WriteLine( data );

        Debug.Log("Saved at " + saveSummary);
        sw.Close();
    }
   
    // Data collection
    private void CollectData(Agent agent)
    {
        AgentData data;
        if (!agentData.ContainsKey(agent))
        {
            data = new AgentData();
            data.lastPos = agent.transform.position;
            agentData[agent] = data;
        }
        data = agentData[agent];        

        // calculate current speed
        float currentMovement = Vector3.Distance(agent.transform.position, data.lastPos);
        float currentSpeed = currentMovement / stepTime;

        // calculate current density
        float currentDensity = CalculateAgentAreaDensity(agent);
        float curSmartDensity = CalculateAgentAreaDensitySmart(agent);

        // aggregate data
        data.meanSpeed                  = ((data.meanSpeed * data.simulatedFrames) + currentSpeed) / (data.simulatedFrames + 1);
        data.meanLocalDensityArea       = ((data.meanLocalDensityArea * data.simulatedFrames) + currentDensity) / (data.simulatedFrames + 1);
        data.maxLocalDensityArea        = Mathf.Max(currentDensity, data.maxLocalDensityArea);
        data.meanLocalDensitySmartArea  = ((data.meanLocalDensitySmartArea * data.simulatedFrames) + curSmartDensity) / (data.simulatedFrames + 1);
        data.maxLocalDensitySmartArea   = Mathf.Max(curSmartDensity, data.maxLocalDensitySmartArea);
        data.distanceTraveled           += currentMovement;
        data.simulatedFrames++;
        data.lastPos = agent.transform.position;
    }
    private void CollectWorldData()
    {
        // collecta world data on this frame
        float density      = CalculateCellsDensity(out float maxDensity);
        float densitySmart = CalculateCellsDensitySmart(out float maxDensitySmart);
        float globalDensity      = CalculateGlobalDensity();
        float globalDensitySmart = CalculateGlobalDensitySmart();

        // aggregate world data
        worldData.meanCellDensity        = ((worldData.meanCellDensity        * worldData.simulatedFrames) + density           ) / (worldData.simulatedFrames + 1);
        worldData.meanCellDensitySmart   = ((worldData.meanCellDensitySmart   * worldData.simulatedFrames) + densitySmart      ) / (worldData.simulatedFrames + 1);
        worldData.meanGlobalDensity      = ((worldData.meanGlobalDensity      * worldData.simulatedFrames) + globalDensity     ) / (worldData.simulatedFrames + 1);
        worldData.meanGlobalDensitySmart = ((worldData.meanGlobalDensitySmart * worldData.simulatedFrames) + globalDensitySmart) / (worldData.simulatedFrames + 1);

        worldData.maxCellDensity         = Mathf.Max(worldData.maxCellDensity        , maxDensity);        
        worldData.maxCellDensitySmart    = Mathf.Max(worldData.maxCellDensitySmart   , maxDensitySmart);
        worldData.maxGlobalDensity       = Mathf.Max(worldData.maxGlobalDensity      , globalDensity);
        worldData.maxGlobalDensitySmart  = Mathf.Max(worldData.maxGlobalDensitySmart , globalDensitySmart);

        // update value
        worldData.simulatedFrames++;
    }
    private float CalculateAgentAreaDensity(Agent agent)
    {
        float currentDensity = 1; // me
        foreach (Agent other in simulation._agents)
        {
            if (other == agent)
                continue;
            Vector3 aux = agent.transform.position - other.transform.position;
            if (Mathf.Abs(aux.x) < density_side * 0.5f && Mathf.Abs(aux.z) < density_side * 0.5f)
                currentDensity++;
        }
        currentDensity /= density_area;
        return currentDensity;
    }
    private float CalculateAgentAreaDensitySmart(Agent agent)
    {
        float currentDensity = 1; // me

        // get agent searchable area
        float density_area = Mathf.PI * Mathf.Pow(agent.agentRadius, 2);

        // get agent occupancy area
        float avgHullRadius;
        if (agent.Auxins == null || agent.Auxins.Count == 0)
            avgHullRadius = agent.agentRadius;
        else if (agent.Auxins.Count == 1)
            avgHullRadius = 0.3f; // agent size on navmesh config
        else
        {
            Vector2 hullMin = new(agent.Auxins.Min(x => x.transform.position.x - x.transform.localScale.x),
                                  agent.Auxins.Min(x => x.transform.position.z - x.transform.localScale.z));
            Vector2 hullMax = new(agent.Auxins.Max(x => x.transform.position.x + x.transform.localScale.x),
                                  agent.Auxins.Max(x => x.transform.position.z + x.transform.localScale.z));
            float avgHullDiameter = Mathf.Lerp(Mathf.Abs(hullMax.x - hullMin.x), Mathf.Abs(hullMax.y - hullMin.y), 0.5f);
            avgHullRadius = avgHullDiameter * 0.5f;            
        }
            
        float hullArea = Mathf.PI * Mathf.Pow(avgHullRadius, 2);

        // calculate density
        currentDensity = density_area / hullArea;

        return currentDensity;
    }    
    public float CalculateCellsDensity(out float max)
    {
        // calcula densidade baseado em n de agentes / area da célula, desconsiderando células sem agentes
        float densitySum = 0;
        float densityMax = 0;
        int cellCount = 0;
        foreach (Cell c in simulation.Cells)
        {
            float area = c.transform.localScale.x * c.transform.localScale.y;
            float agentCount = c.Auxins.Select(x => x.Agent).Where(x => x != null).Distinct().Count();
            
            if (agentCount == 0)
                continue;

            float d = agentCount / area;
            densitySum += d;
            densityMax = Mathf.Max(densityMax, d);
            cellCount++;
        }
        
        max = densityMax;
        if (cellCount != 0)
            return densitySum / (float)cellCount;
        return 0;        
    }
    public float CalculateCellsDensitySmart(out float max)
    {
        // calcula densidade baseado em n de agentes pela area da célula, desconsiderando células sem agentes
        // mas área da célula é de acordo com n de marcadores nela pelo n de marcadores que deveria ter
        float densitySum = 0;
        float densityMax = 0;
        int cellCount = 0;
        foreach (Cell c in simulation.Cells)
        {
            float maxArea = c.transform.localScale.x * c.transform.localScale.y;
            float area = maxArea * (c.Auxins.Count / (float)simulation.MarkerSpawner._maxMarkersPerCell);
            int agentCount = c.Auxins.Select(x => x.Agent).Where(x => x != null).Distinct().Count();

            if (agentCount == 0)
                continue;

            float d = agentCount / area;
            densitySum += d;
            densityMax = Mathf.Max(densityMax, d);
            cellCount++;
        }
        max = densityMax;
        if (cellCount != 0)
            return densitySum / (float)cellCount;
        return 0;
    }
    public float CalculateGlobalDensity()
    {
        // calcula densidade baseado em n de agentes / area da sala
        float area = roomWidth * roomLength;
        float agentCount = simulation._agents.Count;

        return agentCount / area;        
    }
    public float CalculateGlobalDensitySmart()
    {
        // calcula densidade baseado em n de agentes / (area da sala - area dos obstáculos)
        float area = roomWidth * roomLength;
        area -= obstacleGenerator?.GetObstacleArea() ?? 0;

        float agentCount = simulation._agents.Count;

        return agentCount / area;
    }


    public float CalculateAvgWorldAreaDensity()
    {
        float densitySum = 0;
        foreach (Agent a in simulation._agents)
        {
            densitySum += CalculateAgentAreaDensity(a);
        }
        if (densitySum == 0)
            return 0;
        return densitySum / simulation._agents.Count;
    }
    public float CalculateAvgWorldAreaDensitySmart()
    {
        float densitySum = 0;
        foreach (Agent a in simulation._agents)
        {
            densitySum += CalculateAgentAreaDensitySmart(a);
        }
        if (densitySum == 0)
            return 0;
        return densitySum / simulation._agents.Count;
    }

    // Utils & Helpers    
    Transform SearchChildOf(Transform parent, string childName)
    {
        foreach (Transform t in parent)
        {
            if (t.name == childName)
                return t;
        }

        foreach (Transform t in parent)
        {
            SearchChildOf(t, childName);
        }

        return null;
    }
    class AgentData
    {
        // data
        public float meanLocalDensityArea = 0;
        public float maxLocalDensityArea = 0;
        public float meanLocalDensitySmartArea = 0;
        public float maxLocalDensitySmartArea = 0;
        public float meanLocalDensityCell = 0; 
        public float meanSpeed = 0;
        public float distanceTraveled = 0;
        public int simulatedFrames = 0;

        // cache
        public Vector3 lastPos;
    }
    class WorldData
    {
        public float meanCellDensity = 0;
        public float maxCellDensity = 0;

        public float meanCellDensitySmart = 0;
        public float maxCellDensitySmart = 0;

        public float meanGlobalDensity = 0;
        public float maxGlobalDensity = 0;

        public float meanGlobalDensitySmart = 0;
        public float maxGlobalDensitySmart = 0;

        public int simulatedFrames = 0;
    }

    
}


