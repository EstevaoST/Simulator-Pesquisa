using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using OrcaSimulator.Core;

[DefaultExecutionOrder(-1)]
public class GeneratorSimulatorManager : SimulationManager 
{    
    [Header("Scene Generation")]
    public bool generateScene = true;
    public float roomLength, roomWidth, inputFlow, flowDuration, initialPopulation, exitSize;
    public int inputsAgents;

    [Header("Result Saving")]
    
    public string saveSummary;
    public bool appendSummary = true;

    protected override void Start()
    {        
        CreateSceneForParameters();
        base.Start();
    }

    // static automatization    
    protected override void SaveSummaryResult()
    {
        if (saveSummary == "")
            saveSummary = SceneManager.GetActiveScene().name + ".csv";

        bool addHeader = !File.Exists(saveSummary);
        StreamWriter sw = new StreamWriter(saveSummary, appendSummary);

        if (addHeader)
        {
            // 1: simulation has finished
            sw.Write("Finished");
            sw.Write(";INPUTS;");
            // 3 - 8 : Room definition
            sw.Write("Room width;Room lenght;Exit size;Input flow;Flow Duration;Initial population");
            sw.Write(";OUTPUTS;");
            // 10 - 14 : Simulation results
            sw.WriteLine("Simulation time (seconds);Average exit time (seconds);Average speed (m/s);Average density;First exit time at second");
        }
        
        // write if finished
        if (simulationInterrupted)
            sw.Write("No");
        else
            sw.Write("Yes");
        sw.Write(";;");

        var goals = GameObject.Find("Goals").GetComponentsInChildren<DecisionContext>();

        // write simulation input
        float[] fData = { roomWidth, roomLength, exitSize, inputFlow, flowDuration, initialPopulation };
        string data = string.Join(';', fData.Select(x => x.ToString(SimGlobalConfig.ExportFormat)));
        sw.Write(data);        
        sw.Write(";;");

        // remove all agents that didn't make it -> they probably didn't make it because they get stuck with ORCA's behavior
        agentList.RemoveAll(x => x.isActiveAndEnabled);

        simulatedTime = Mathf.Min( simulatedTime , decisionContexts.Max(x => x.lastExitTime) );
        
        // calculate simulation output
        float avgDensity1 = 0, avgDensity2 = 0;
        int avgFrames = 0;
        float avgSpeed1 = 0, avgSpeed2 = 0;

        if (agentList.Count > 0)
        {
            foreach (var agent in agentList)
            {
                avgDensity1 += agent.meanDensity;
                avgDensity2 += agent.meanDensity * agent.simulatedFrames;
                avgFrames += agent.simulatedFrames;
                avgSpeed1 += agent.meanSpeed;
                avgSpeed2 += agent.meanSpeed * agent.simulatedFrames;
            }
            avgDensity1 /= agentList.Count;
            avgDensity2 /= avgFrames;
            avgSpeed1 /= agentList.Count;
            avgDensity2 /= avgFrames;
            avgFrames /= agentList.Count;

        }

        // write simulatted time output
        fData = new float[]{ simulatedTime , (avgFrames * stepTime) , avgSpeed1 , avgDensity1, goals.Min(x => x.firstExitTime) };
        data = string.Join(';', fData.Select(x => x.ToString(SimGlobalConfig.ExportFormat)));
        sw.WriteLine( data );

        foreach (DecisionContext dc in decisionContexts)
        {
            if (dc.agentCountForLocalStatistics > -1)
            {
                sw.WriteLine();
                sw.WriteLine(dc.name + " - local data");
                sw.WriteLine("Starting time;Simulation Time;Avg Exit Time;Avg Speed; Avg Density;First Exit Time");
                fData = new float[] { dc.local_startTime, dc.local_SimTm, (dc.local_AvgFrames * stepTime), dc.local_AvgSp, dc.local_AvgDn, dc.local_FET };
                data = string.Join(';', fData.Select(x => x.ToString(SimGlobalConfig.ExportFormat)));
                sw.WriteLine(data);
            }
        }

        Debug.Log("Saved at " + saveSummary);
        sw.Close();

        Agent.collisionCount = 0;
    }
    
    public void CreateSceneForParameters()
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
        if (generateScene)
        {            
            foreach (var ag in agentList)
                Destroy(ag.gameObject);
            agentList.Clear();
            activeAgentList.Clear();
            foreach (var ct in decisionContexts)
                ct.Reset();
        }

        // criar uma nova sala
        if (generateScene)
        {
            #region alterar tamanho da sala
            Vector3 roomSize = new Vector3(roomWidth, room.transform.localScale.y, roomLength);
            if (room != null)
            {
                room.transform.localScale = roomSize;
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
        }
        #region alterar saída e tamanho da saída

        //Context goal = goals.GetComponentInChildren<Context>(true);
        //goal.transform.localScale = Vector3.one + Vector3.right * (exitSize-1);// + Vector3.forward * roomLength;

        var gs = goals.GetComponentsInChildren<Context>(true);
        foreach(Context g in gs)
            g.transform.localScale = Vector3.one + Vector3.right * (exitSize-1);// + Vector3.forward * roomLength;

        #endregion
        SpawnerContext spawn = spawns.GetComponentInChildren<SpawnerContext>();
        if (spawn != null)
        {
            #region alterar input flow e duration

            spawn.spawnTotal = inputsAgents;
            spawn.spawnSpeed = inputFlow;
            spawn.SetSizeForFlow(inputFlow);

            //foreach (var item in spawn.targets)
            //{
            //    item.chance = item.target.gameObject.activeInHierarchy ? 1 : 0;
            //}
            #endregion
            #region instanciar agentes já existentes
            Vector3 nextPos = Vector3.zero;
            int lBorder = 0, loop = 0, dir = 0;
            float offset = defaultAgentPrefab.radius * 2;
            for (int i = 0; i < initialPopulation; i++)
            {
                var agent = Instantiate(defaultAgentPrefab.gameObject).GetComponent<Agent>();
                agent.transform.SetParent(agents.transform);
                agent.transform.localPosition = nextPos * offset;
                
                agent.SetTarget(spawn);
                spawn.EnterContext(agent);

                if (i == lBorder)
                {
                    lBorder += lBorder + 4;
                    loop++;
                    nextPos = Vector3.forward * loop;
                    dir = 0;
                }
                else
                {

                    switch (dir)
                    {
                        case 0:
                            nextPos.x++;
                            nextPos.z--;
                            if (nextPos.z <= 0)
                                dir++;
                            break;
                        case 1:
                            nextPos.x--;
                            nextPos.z--;
                            if (nextPos.x <= 0)
                                dir++;
                            break;
                        case 2:
                            nextPos.x--;
                            nextPos.z++;
                            if (nextPos.z >= 0)
                                dir++;
                            break;
                        case 3:
                            nextPos.x++;
                            nextPos.z++;
                            if (nextPos.x >= 0)
                                dir++;
                            break;
                        default:
                            break;
                    }
                }
            }

            #endregion
        }

        if (generateScene)
        {
            //UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
            //UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            //var geo = UnityEngine.AI.NavMesh.CalculateTriangulation();

            navmeshBorders = ShowNavmeshEdges.FindNavMeshBorders(UnityEngine.AI.NavMesh.CalculateTriangulation());            
        }
    }

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
}
